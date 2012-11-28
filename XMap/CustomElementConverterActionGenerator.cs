namespace XMap
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Xml.Linq;

    class CustomElementConverterActionGenerator<TItem, TProperty>
    {
        private readonly Expression<Func<XElement, TProperty>> _converter;
        private readonly ParameterExpression _elementParam;
        private readonly ParameterExpression _itemParam;
        private readonly MemberExpression _itemProperty;

        public CustomElementConverterActionGenerator(PropertyInfo propertyInfo, Expression<Func<XElement, TProperty>> converter)
        {
            _converter = converter;
            _elementParam = Expression.Parameter(typeof(XElement));
            _itemParam = Expression.Parameter(typeof(TItem));
            _itemProperty = Expression.Property(_itemParam, propertyInfo);
        }

        public Action<XElement, TItem> Generate()
        {
            return Compile(Expression.Assign(_itemProperty, Expression.Invoke(_converter, _elementParam)));
        }

        protected Action<XElement, TItem> Compile(Expression body)
        {
            return Expression.Lambda<Action<XElement, TItem>>(body, _elementParam, _itemParam).Compile();
        }
    }
}