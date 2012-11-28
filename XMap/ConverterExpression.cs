namespace XMap
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class ConverterExpression
    {
        public static Expression Create(Type propertyType, ParameterExpression source)
        {
            if (propertyType == typeof(string))
            {
                return source;
            }

            if (propertyType.IsEnum)
            {
                return Expression.Call(Converters.ParseEnumMethod.MakeGenericMethod(propertyType), source);
            }

            var parseMethod = propertyType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static,
                                                                    null,
                                                                    new[] { typeof(string) }, null);
            if (parseMethod != null)
            {
                return Expression.Call(parseMethod, source);
            }

            return Expression.Call(Converters.ChangeTypeMethod.MakeGenericMethod(propertyType), source);
        }
    }
}