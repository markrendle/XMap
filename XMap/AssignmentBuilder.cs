namespace XMap
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    class AssignmentBuilder<TItem,TProperty>
    {
        private readonly Expression<Func<TItem, TProperty>> _propFunc;
        private readonly ParameterExpression _itemParam;
        private readonly ParameterExpression _propertyParam = Expression.Parameter(typeof (TProperty));
        private readonly IDictionary<string, Expression> _objectSetupLines;

        public AssignmentBuilder(Expression<Func<TItem, TProperty>> propFunc, IDictionary<string, Expression> objectSetupLines, ParameterExpression itemParam)
        {
            _propFunc = propFunc;
            _objectSetupLines = objectSetupLines;
            _itemParam = itemParam;
        }

        public LambdaExpression Build()
        {
            var lines = new List<Expression>();
            var methodCall = _propFunc.Body as MethodCallExpression;
            if (methodCall != null)
            {
                if (methodCall.Method.Name.Equals("get_Item"))
                {
                    var argumentTypes = methodCall.Arguments.Select(e => e.Type).ToList();
                    argumentTypes.Add(_propertyParam.Type);
                    var arguments = methodCall.Arguments.ToList();
                    arguments.Add(_propertyParam);
                    var assign = Expression.Call(BuildProperty(methodCall.Object), "set_Item", argumentTypes.ToArray(), arguments.ToArray());
                    lines.Add(assign);
                }
                else
                {
                    throw new ArgumentException("Only indexers are supported on property expressions.");
                }
            }
            else
            {
                var assign = Expression.Assign(BuildProperty(_propFunc.Body, true), _propertyParam);
                lines.Add(assign);
            }

            return Expression.Lambda(Expression.Block(lines), _itemParam, _propertyParam);
        }

        private Expression BuildProperty(Expression expression, bool isAssignedProperty = false)
        {
            if (expression is ParameterExpression)
            {
                return _itemParam;
            }

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                var property = Expression.PropertyOrField(BuildProperty(memberExpression.Expression), memberExpression.Member.Name);
                if (!isAssignedProperty)
                {
                    EnsureMember(property);
                }
                return property;
            }

            if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                var binaryExpression = (BinaryExpression) expression;
                var array = BuildProperty(binaryExpression.Left);

                //EnsureMember(array as MemberExpression);
                EnsureArraySize(array, binaryExpression);

                return Expression.ArrayIndex(BuildProperty(binaryExpression.Left), binaryExpression.Right);
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                if (methodCallExpression.Method.Name.Equals("get_Item"))
                {
                    var property = BuildProperty(methodCallExpression.Object);
                    EnsureCollectionSize(property, methodCallExpression.Arguments[0]);
                    return Expression.Call(property, methodCallExpression.Method, methodCallExpression.Arguments);
                }
            }

            throw new InvalidOperationException("Cannot parse property expression.");
        }

        private void EnsureArraySize(Expression array, BinaryExpression binaryExpression)
        {
            var expressionText = array + "#" + binaryExpression.Right;
            if (_objectSetupLines.ContainsKey(expressionText)) return;
            var length = Expression.Property(array, "Length");
            var tooSmall = Expression.LessThanOrEqual(length, binaryExpression.Right);
            var resizeMethod = Helpers.ResizeMethod.MakeGenericMethod(array.Type.GetElementType());
            var resize = Expression.Call(resizeMethod, array, binaryExpression.Right);
            var assign = Expression.Assign(array, resize);
            _objectSetupLines.Add(expressionText, Expression.IfThen(tooSmall, assign));
        }

        private void EnsureCollectionSize(Expression collection, Expression index)
        {
            var expressionText = collection + "#" + index;
            if (_objectSetupLines.ContainsKey(expressionText)) return;

            var collectionType = collection.Type;
            if (collectionType.IsGenericType)
            {
                var itemType = collectionType.GetGenericArguments()[0];
                if (typeof (ICollection<>).MakeGenericType(itemType).IsAssignableFrom(collectionType))
                {
                    var ensureSizeMethod = Helpers.FillMethod.MakeGenericMethod(itemType);
                    _objectSetupLines.Add(expressionText, Expression.Call(ensureSizeMethod, collection, index));
                    return;
                }
            }
            if (typeof (IList).IsAssignableFrom(collectionType))
            {
                _objectSetupLines.Add(expressionText, Expression.Call(Helpers.NonGenericFillMethod, collection, index));
            }
        }

        private void EnsureMember(MemberExpression property)
        {
            if (property == null) return;
            var expressionText = property.ToString();
            if (_objectSetupLines.ContainsKey(expressionText)) return;

            Expression newExpression;
            if (property.Type.IsArray)
            {
                newExpression = Expression.Call(Helpers.NewArrayMethod.MakeGenericMethod(property.Type.GetElementType()));
            }
            else
            {
                newExpression = Expression.New(property.Type);
            }

            var checkNull = Expression.ReferenceEqual(property, Expression.Constant(null, property.Type));
            var create = Expression.Assign(property, newExpression);

            _objectSetupLines.Add(expressionText, Expression.IfThen(checkNull, create));
        }
    }
}