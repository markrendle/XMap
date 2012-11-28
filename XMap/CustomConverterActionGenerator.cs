namespace XMap
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    class CustomConverterActionGenerator<TItem, TProperty>
    {
        private readonly ParameterExpression _stringParam;
        private readonly ParameterExpression _itemParam;
        private readonly Expression _assignExpression;
        private readonly Expression<Func<string, TProperty>> _converter;

        public CustomConverterActionGenerator(Expression assignExpression, Expression<Func<string,TProperty>> converter)
        {
            _stringParam = Expression.Parameter(typeof(string));
            _itemParam = Expression.Parameter(typeof(TItem));
            _assignExpression = assignExpression;
            _converter = converter;
        }

        public Action<string, TItem> Generate()
        {
            return
                Compile(Expression.Invoke(_assignExpression, _itemParam, Expression.Invoke(_converter, _stringParam)));
        }

        protected Action<string, TItem> Compile(Expression body)
        {
            return Expression.Lambda<Action<string, TItem>>(body, _stringParam, _itemParam).Compile();
        }
    }

    class CustomConverterActionGenerator<TItem, TProperty, T1, T2>
    {
        private readonly ParameterExpression _param1;
        private readonly ParameterExpression _param2;
        private readonly ParameterExpression _itemParam;
        private readonly MemberExpression _itemProperty;
        private readonly Expression<Func<T1, T2, TProperty>> _converter;

        public CustomConverterActionGenerator(PropertyInfo propertyInfo, Expression<Func<T1, T2, TProperty>> converter)
        {
            _param1 = Expression.Parameter(typeof (T1));
            _param2 = Expression.Parameter(typeof (T2));
            _itemParam = Expression.Parameter(typeof(TItem));
            _itemProperty = Expression.Property(_itemParam, propertyInfo);
            _converter = converter;
        }

        public Action<T1, T2, TItem> Generate()
        {
            return Compile(Expression.Assign(_itemProperty, Expression.Invoke(_converter, _param1, _param2)));
        }

        protected Action<T1, T2, TItem> Compile(Expression body)
        {
            return Expression.Lambda<Action<T1, T2, TItem>>(body, _param1, _param2, _itemParam).Compile();
        }
    }
}