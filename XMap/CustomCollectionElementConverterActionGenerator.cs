namespace XMap
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Xml.Linq;

    class CustomCollectionElementConverterActionGenerator<TItem, TProperty>
    {
        private readonly Expression<Func<XElement, TProperty>> _converter;
        private readonly ConstantExpression _childName;
        private readonly ParameterExpression _elementParam;
        private readonly ParameterExpression _itemParam;
        private readonly MemberExpression _itemProperty;
        private readonly Type _collectionType;

        public CustomCollectionElementConverterActionGenerator(PropertyInfo propertyInfo, Expression<Func<XElement, TProperty>> converter, string childName = null)
        {
            _converter = converter;
            _childName = childName == null ? null : Expression.Constant(childName);
            _elementParam = Expression.Parameter(typeof(XElement));
            _itemParam = Expression.Parameter(typeof(TItem));
            _itemProperty = Expression.Property(_itemParam, propertyInfo);
            _collectionType = propertyInfo.PropertyType;
        }

        public Action<XElement, TItem> Generate()
        {
            var createIfNull =
                Expression.IfThen(Expression.Equal(_itemProperty, Expression.Constant(null, _collectionType)),
                                  Expression.Assign(_itemProperty, Expression.New(_collectionType)));
            var run = _childName == null ?
                Expression.Call(CollectionRunner.RunAllMethod.MakeGenericMethod(typeof(TProperty)), _itemProperty, _elementParam, _converter)
                :
                Expression.Call(CollectionRunner.RunNamedMethod.MakeGenericMethod(typeof(TProperty)), _itemProperty, _elementParam, _converter, _childName)
                ;
            var block = Expression.Block(createIfNull, run);

            return Expression.Lambda<Action<XElement, TItem>>(block, _elementParam, _itemParam).Compile();
        }

        protected Action<XElement, TItem> Compile(Expression body)
        {
            return Expression.Lambda<Action<XElement, TItem>>(body, _elementParam, _itemParam).Compile();
        }
    }

    class CollectionRunner
    {
        public static readonly MethodInfo RunAllMethod =
            typeof (CollectionRunner).GetMethod("RunAll", BindingFlags.Static | BindingFlags.Public);

        public static readonly MethodInfo RunNamedMethod =
            typeof (CollectionRunner).GetMethod("RunNamed", BindingFlags.Static | BindingFlags.Public);

        public static void RunAll<TProperty>(ICollection<TProperty> collection, XElement element, Func<XElement, TProperty> convert)
        {
            foreach (var child in element.Elements())
            {
                collection.Add(convert(child));
            }
        }

        public static void RunNamed<TProperty>(ICollection<TProperty> collection, XElement element, Func<XElement, TProperty> convert, string childName)
        {
            foreach (var child in element.Elements(childName))
            {
                collection.Add(convert(child));
            }
        }
    }
}