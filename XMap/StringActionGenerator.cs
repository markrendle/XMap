namespace XmlMapper
{
    using System.Linq.Expressions;
    using System.Reflection;

    class StringActionGenerator<TItem> : ActionGeneratorBase<TItem, string>
    {
        public StringActionGenerator(PropertyInfo propertyInfo) : base(propertyInfo)
        {
        }

        protected override Expression MakeConversion(ParameterExpression stringParam)
        {
            return stringParam;
        }
    }
}