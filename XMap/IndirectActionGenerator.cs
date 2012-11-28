namespace XMap
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    class IndirectActionGenerator<TItem,TProperty> : IActionGenerator<TItem>
    {
        private readonly Stack<PropertyInfo> _propertyInfos;
        private readonly ParameterExpression _stringParam;
        private readonly ParameterExpression _itemParam;

        public IndirectActionGenerator(Stack<PropertyInfo> propertyInfos)
        {
            _propertyInfos = propertyInfos;
            _stringParam = Expression.Parameter(typeof (string));
            _itemParam = Expression.Parameter(typeof (TItem));
        }

        public Action<string, TItem> Generate()
        {
            var expressions = new List<Expression>();
            Expression ownerExpression = _itemParam;
            while (_propertyInfos.Count > 1)
            {
                var property = _propertyInfos.Pop();
                var propertyExpression = Expression.Property(ownerExpression, property);
                var ifNullCreate = Expression.IfThen(Expression.Equal(propertyExpression, Expression.Constant(null, property.PropertyType)),
                                                     Expression.Assign(propertyExpression,
                                                                       Expression.New(property.PropertyType)));
                expressions.Add(ifNullCreate);
                ownerExpression = propertyExpression;
            }
            var actualProperty = _propertyInfos.Pop();
            var actualPropertyExpression = Expression.Property(ownerExpression, actualProperty);

            var assignExpression = Expression.Assign(actualPropertyExpression,
                                                     ConverterExpression.Create(actualProperty.PropertyType, _stringParam));
            expressions.Add(assignExpression);

            var block = Expression.Block(expressions);
            return Expression.Lambda<Action<string, TItem>>(block, _stringParam, _itemParam).Compile();
        }
    }
}