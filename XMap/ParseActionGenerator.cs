namespace XmlMapper
{
    using System.Linq.Expressions;
    using System.Reflection;

    class ParseActionGenerator<TItem, TProperty> : ActionGeneratorBase<TItem, TProperty>
    {
        private readonly MethodInfo _parseMethod;

        public ParseActionGenerator(PropertyInfo propertyInfo, MethodInfo parseMethod) : base(propertyInfo)
        {
            _parseMethod = parseMethod;
        }

        protected override Expression MakeConversion(ParameterExpression stringParam)
        {
            return Expression.Call(_parseMethod, stringParam);
        }
    }
}