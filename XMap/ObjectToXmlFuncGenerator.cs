using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace XMap
{
    public class ObjectToXmlFuncGenerator<T> where T : class, new()
    {
        public static Func<T, string> GenerateAttributeFunc<TProperty>(Expression<Func<T, TProperty>> propFunc, Expression<Func<TProperty, string>> toStringFunc)
        {
            var itemParam = Expression.Parameter(typeof (T));
            var invoke = Expression.Invoke(propFunc, itemParam);
            Expression toString;
            if (toStringFunc == null)
            {
                toString = Helpers.MakeToStringCall<TProperty>(invoke);
            }
            else
            {
                toString = Expression.Invoke(toStringFunc, invoke);
            }
            Func<T, string> func = Expression.Lambda<Func<T, string>>(toString, itemParam).Compile();
            return func;
        }

        public static Func<T, Tuple<string, string>> GenerateAttributePairFunc<TProperty>(Expression<Func<T, TProperty>> propFunc, Expression<Func<TProperty, Tuple<string, string>>> toStringsFunc)
        {
            if (toStringsFunc == null) throw new ArgumentNullException("toStringsFunc");
            var itemParam = Expression.Parameter(typeof (T));
            var invoke = Expression.Invoke(propFunc, itemParam);
            var toString = Expression.Invoke(toStringsFunc, invoke);
            var func = Expression.Lambda<Func<T, Tuple<string, string>>>(toString, itemParam).Compile();
            return func;
        }

        public static Func<T, XElement> GenerateSingleElemenetFunc<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc, XmlMapper<TProperty> mapper) where TProperty : class, new()
        {
            var itemParam = Expression.Parameter(typeof (T));
            var invoke = Expression.Invoke(propFunc, itemParam);
            var mapperConstant = Expression.Constant(mapper);
            var nameConstant = Expression.Constant(name);
            var toXml = mapper.GetType().GetMethod("ToXml", new[] {typeof (TProperty), typeof (XName)});
            var callToXml = Expression.Call(mapperConstant, toXml, invoke, nameConstant);

            var func = Expression.Lambda<Func<T, XElement>>(callToXml, itemParam).Compile();
            return func;
        }

        public static Func<T, XElement> GenerateCollectionElementFunc<TProperty>(string name, Expression<Func<T, ICollection<TProperty>>> propFunc, XmlMapper<TProperty> mapper)
            where TProperty : class, new()
        {
            var itemParam = Expression.Parameter(typeof (T));
            var invoke = Expression.Invoke(propFunc, itemParam);
            var mapperConstant = Expression.Constant(mapper);

            XName containerElementName;
            XName childElementName;
            GetContainerElementName(name, out containerElementName, out childElementName);

            var containerNameConstant = Expression.Constant(containerElementName);
            var childNameConstant = Expression.Constant(childElementName);
            var propertyEnumerableType = typeof (IEnumerable<>).MakeGenericType(typeof (TProperty));
            var toXml = mapper.GetType()
                              .GetMethod("ToXml", new[] {propertyEnumerableType, typeof (XName), typeof (XName)});
            var callToXml = Expression.Call(mapperConstant, toXml, invoke, containerNameConstant,
                                            childNameConstant);

            var func = Expression.Lambda<Func<T, XElement>>(callToXml, itemParam).Compile();
            return func;
        }

        private static void GetContainerElementName(string name, out XName containerElementName, out XName childElementName)
        {
            int slashIndex = name.IndexOf('/');
            if (slashIndex < 0)
            {
                containerElementName = name;
                childElementName = name + "Item";
            }
            else
            {
                containerElementName = name.Substring(0, slashIndex);
                childElementName = name.Substring(slashIndex + 1);
            }
        }
    }
}