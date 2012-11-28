using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace XMap
{
    /// <summary>
    ///     Maps data from <see cref="XElement" /> elements to an object graph and back again.
    /// </summary>
    /// <typeparam name="T">The type of the object graph root.</typeparam>
    public class XmlMapper<T> : IEnumerable where T : class, new()
    {
        private readonly XmlToObjectActionGenerator<T> _xmlToObjectActionGenerator = new XmlToObjectActionGenerator<T>();

        private readonly Dictionary<XName, Action<string, T>> _attributeActions =
            new Dictionary<XName, Action<string, T>>();

        private readonly Dictionary<XName, Func<T, string>> _attributeFuncs = new Dictionary<XName, Func<T, string>>();

        private readonly Dictionary<Tuple<XName, XName>, Func<T, Tuple<string, string>>> _attributePairFuncs =
            new Dictionary<Tuple<XName, XName>, Func<T, Tuple<string, string>>>();

        private readonly Dictionary<XName, Action<XElement, T>> _elementActions =
            new Dictionary<XName, Action<XElement, T>>();

        private readonly Dictionary<string, Action<XElement, T>> _elementCollectionActions =
            new Dictionary<string, Action<XElement, T>>();

        private readonly Dictionary<string, Func<T, XElement>> _elementCollectionFuncs =
            new Dictionary<string, Func<T, XElement>>();

        private readonly Dictionary<XName, Func<T, XElement>> _elementFuncs = new Dictionary<XName, Func<T, XElement>>();

        private readonly Dictionary<Tuple<XName, XName>, Action<string, string, T>> _multiAttributeActions =
            new Dictionary<Tuple<XName, XName>, Action<string, string, T>>();

        private readonly ObjectToXmlFuncGenerator<T> _objectToXmlFuncGenerator = new ObjectToXmlFuncGenerator<T>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Enumerable.Empty<object>().GetEnumerator();
        }

        /// <summary>
        ///     Adds a mapping for an attribute to a property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="name">The name of the XML attribute.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        public void Add<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc)
        {
            _attributeActions.Add(name, _xmlToObjectActionGenerator.Generate(propFunc));

            AddAttributeFunc(name, propFunc);
        }

        /// <summary>
        ///     Adds a mapping for an attribute to a property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="name1">The name of the first XML attribute.</param>
        /// <param name="name2">The name of the second XML attribute.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        /// <param name="converter">A custom function for converting the XML attribute strings to the property type.</param>
        /// <param name="formatter">A custom function for converting the property to the XML attribute strings.</param>
        public void Add<TProperty>(XName name1, XName name2, Expression<Func<T, TProperty>> propFunc,
                                   Expression<Func<string, string, TProperty>> converter,
                                   Expression<Func<TProperty, Tuple<string, string>>> formatter)
        {
            Tuple<XName, XName> tuple = Tuple.Create(name1, name2);
            _multiAttributeActions.Add(tuple, _xmlToObjectActionGenerator.Generate(propFunc, converter));
            AddAttributePairFunc(tuple, propFunc, formatter);
        }

        /// <summary>
        ///     Adds a mapping for an attribute to a property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="name">The name of the XML attribute.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        /// <param name="converter">A custom function for converting the XML attribute string to the property type.</param>
        public void Add<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc,
                                   Expression<Func<string, TProperty>> converter)
        {
            _attributeActions.Add(name, _xmlToObjectActionGenerator.Generate(propFunc, converter));
            AddAttributeFunc(name, propFunc);
        }

        /// <summary>
        ///     Adds a mapping for an attribute to a property.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="name">The name of the XML attribute.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        /// <param name="converter">A custom function for converting the XML attribute string to the property type.</param>
        /// <param name="formatter">A custom function for converting the property value to the XML attribute string.</param>
        public void Add<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc,
                                   Expression<Func<string, TProperty>> converter,
                                   Expression<Func<TProperty, string>> formatter)
        {
            _attributeActions.Add(name, _xmlToObjectActionGenerator.Generate(propFunc, converter));
            AddAttributeFunc(name, propFunc, formatter);
        }

        /// <summary>
        ///     Adds a mapping for an element to a property of a complex type.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="name">The name of the XML element.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        /// <param name="mapper">
        ///     An <see cref="XmlMapper{T}" /> to map the child element to the complex property type.
        /// </param>
        public void Add<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc, XmlMapper<TProperty> mapper)
            where TProperty : class, new()
        {
            _elementActions.Add(name, _xmlToObjectActionGenerator.Generate(propFunc, mapper));
            AddSingleElementFunc(name, propFunc, mapper);
        }

        /// <summary>
        ///     Adds a mapping for a collection of elements to a collection of properties of a complex type.
        /// </summary>
        /// <typeparam name="TProperty">The type of the objects in the property collection.</typeparam>
        /// <param name="name">The name of the XML element.</param>
        /// <param name="propFunc">An expression representing the object property.</param>
        /// <param name="mapper">
        ///     An <see cref="XmlMapper{T}" /> to map the child element to the complex property type.
        /// </param>
        public void Add<TProperty>(string name, Expression<Func<T, ICollection<TProperty>>> propFunc,
                                   XmlMapper<TProperty> mapper)
            where TProperty : class, new()
        {
            string childName = null;
            int slashIndex = name.IndexOf('/');
            if (slashIndex > -1)
            {
                childName = name.Substring(slashIndex + 1);
            }
            _elementCollectionActions.Add(name, _xmlToObjectActionGenerator.Generate(propFunc, mapper, childName));

            AddCollectionElementFunc(name, propFunc, mapper);
        }

        private void AddAttributeFunc<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc,
                                                 Expression<Func<TProperty, string>> toStringFunc = null)
        {
            var func = ObjectToXmlFuncGenerator<T>.GenerateAttributeFunc(propFunc, toStringFunc);
            _attributeFuncs.Add(name, func);
        }

        private void AddAttributePairFunc<TProperty>(Tuple<XName, XName> name, Expression<Func<T, TProperty>> propFunc,
                                                     Expression<Func<TProperty, Tuple<string, string>>> toStringsFunc)
        {
            var func = ObjectToXmlFuncGenerator<T>.GenerateAttributePairFunc(propFunc, toStringsFunc);
            _attributePairFuncs.Add(name, func);
        }

        private void AddSingleElementFunc<TProperty>(XName name, Expression<Func<T, TProperty>> propFunc,
                                                     XmlMapper<TProperty> mapper) where TProperty : class, new()
        {
            var func = ObjectToXmlFuncGenerator<T>.GenerateSingleElemenetFunc(name, propFunc, mapper);
            _elementFuncs.Add(name, func);
        }

        private void AddCollectionElementFunc<TProperty>(string name,
                                                         Expression<Func<T, ICollection<TProperty>>> propFunc,
                                                         XmlMapper<TProperty> mapper) where TProperty : class, new()
        {
            var func = ObjectToXmlFuncGenerator<T>.GenerateCollectionElementFunc(name, propFunc, mapper);
            _elementCollectionFuncs.Add(name, func);
        }

        /// <summary>
        ///     Creates an object graph from an <see cref="XElement" />.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns>A new object graph.</returns>
        public T ToObject(XElement xml)
        {
            return ToObject(xml, new T());
        }

        /// <summary>
        ///     Copies data from an <see cref="XElement" /> to an existing object graph.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="obj">The object to copy the data to.</param>
        /// <returns>
        ///     The object passed in the <c>obj</c> parameter (to facilitate fluent composition).
        /// </returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public T ToObject(XElement xml, T obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            _xmlToObjectActionGenerator.SetupAction(obj);
            MapAttributes(xml, obj);
            MapMultiAttributes(xml, obj);
            MapElements(xml, obj);
            MapCollectionElements(xml, obj);

            return obj;
        }

        private void MapAttributes(XElement xml, T obj)
        {
            foreach (XAttribute attribute in xml.Attributes())
            {
                Action<string, T> action;
                if (_attributeActions.TryGetValue(attribute.Name, out action))
                {
                    action(attribute.Value, obj);
                }
            }
        }

        private void MapMultiAttributes(XElement xml, T obj)
        {
            foreach (var multiAttributeAction in _multiAttributeActions)
            {
                XName key1 = multiAttributeAction.Key.Item1;
                XName key2 = multiAttributeAction.Key.Item2;
                string value1 = xml.Attribute(key1).ValueOrEmpty();
                string value2 = xml.Attribute(key2).ValueOrEmpty();
                multiAttributeAction.Value(value1, value2, obj);
            }
        }

        private void MapElements(XElement xml, T obj)
        {
            foreach (XElement element in xml.Elements())
            {
                Action<XElement, T> action;
                if (_elementActions.TryGetValue(element.Name, out action))
                {
                    action(element, obj);
                }
            }
        }

        private void MapCollectionElements(XElement xml, T obj)
        {
            foreach (var collectionAction in _elementCollectionActions)
            {
                string key = collectionAction.Key;
                Action<XElement, T> action = collectionAction.Value;

                XElement element;
                if (key.Contains("/"))
                {
                    string first = key.Substring(0, key.IndexOf("/", StringComparison.InvariantCulture));
                    element = xml.Element(first);
                }
                else
                {
                    element = xml.Element(key);
                }
                if (element != null)
                {
                    action(element, obj);
                }
            }
        }

        /// <summary>
        ///     Generates XML from an object graph.
        /// </summary>
        /// <param name="obj">The object to get data from.</param>
        /// <param name="elementName">The name to use for the root element.</param>
        /// <returns>
        ///     An <see cref="XElement" /> representation of the object graph.
        /// </returns>
        public XElement ToXml(T obj, XName elementName)
        {
            return ToXml(obj, new XElement(elementName));
        }

        /// <summary>
        ///     Generates XML from an object graph.
        /// </summary>
        /// <param name="obj">The object to get data from.</param>
        /// <param name="xml">
        ///     An existing <see cref="XElement" /> to populate.
        /// </param>
        /// <returns>
        ///     An <see cref="XElement" /> representation of the object graph.
        /// </returns>
        public XElement ToXml(T obj, XElement xml)
        {
            foreach (var attributeFunc in _attributeFuncs)
            {
                xml.SetAttributeValue(attributeFunc.Key, attributeFunc.Value(obj));
            }
            foreach (var attributePairFunc in _attributePairFuncs)
            {
                Tuple<string, string> values = attributePairFunc.Value(obj);
                xml.SetAttributeValue(attributePairFunc.Key.Item1, values.Item1);
                xml.SetAttributeValue(attributePairFunc.Key.Item2, values.Item2);
            }
            foreach (var elementFunc in _elementFuncs)
            {
                xml.Add(elementFunc.Value(obj));
            }
            foreach (var elementFunc in _elementCollectionFuncs)
            {
                xml.Add(elementFunc.Value(obj));
            }
            return xml;
        }

        /// <summary>
        ///     Generates XML from an object graph collection.
        /// </summary>
        /// <param name="objs">The object graphs.</param>
        /// <param name="containerName">Name to use for the container element.</param>
        /// <param name="childName">Name to use for the child elements.</param>
        /// <returns>
        ///     An <see cref="XElement" /> representation of the object graphs.
        /// </returns>
        public XElement ToXml(IEnumerable<T> objs, XName containerName, XName childName)
        {
            var container = new XElement(containerName);
            foreach (XElement child in ToXml(objs, childName))
            {
                container.Add(child);
            }
            return container;
        }

        /// <summary>
        ///     Generates XML from an object graph collection.
        /// </summary>
        /// <param name="objs">The object graphs.</param>
        /// <param name="childName">Name to use for the child elements.</param>
        /// <returns>
        ///     A list of <see cref="XElement" /> representations of the object graphs.
        /// </returns>
        public IEnumerable<XElement> ToXml(IEnumerable<T> objs, XName childName)
        {
            return objs.Select(obj => ToXml(obj, childName));
        }
    }
}