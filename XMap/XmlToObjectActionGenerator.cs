namespace XMap
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Xml.Linq;

    internal class XmlToObjectActionGenerator<TItem>
    {
        private Action<TItem> _setupAction;
        private readonly IDictionary<string, Expression> _setup = new Dictionary<string, Expression>();
        private readonly ParameterExpression _itemParam = Expression.Parameter(typeof (TItem));

        public Action<string, TItem> Generate<TProperty>(Expression<Func<TItem, TProperty>> propFunc)
        {
            var assignExpression = CreatePropertySetterExpression(propFunc);
            return new SimpleActionGenerator<TItem>(assignExpression).Generate();
        }

        public Action<string, TItem> Generate<TProperty>(Expression<Func<TItem, TProperty>> propFunc, Expression<Func<string, TProperty>> converter)
        {
            var assignExpression = CreatePropertySetterExpression(propFunc);

            var generator = new CustomConverterActionGenerator<TItem, TProperty>(assignExpression, converter);
            return generator.Generate();
        }
        
        public Action<string,string, TItem> Generate<TProperty>(Expression<Func<TItem, TProperty>> propFunc, Expression<Func<string,string, TProperty>> converter)
        {
            var property = propFunc.Body as MemberExpression;
            if (property == null) throw new ArgumentException("Expression does not represent a Property.");

            var generator = new CustomConverterActionGenerator<TItem, TProperty, string, string>((PropertyInfo) property.Member,
                                                                                 converter);
            return generator.Generate();
        }
        
        public Action<XElement, TItem> Generate<TProperty>(Expression<Func<TItem, TProperty>> propFunc, XmlMapper<TProperty> mapper)
            where TProperty : class, new()
        {
            var property = propFunc.Body as MemberExpression;
            if (property == null) throw new ArgumentException("Expression does not represent a Property.");

            var generator = new CustomElementConverterActionGenerator<TItem, TProperty>((PropertyInfo) property.Member,
                                                                                        x => mapper.ToObject(x));
            return generator.Generate();
        }
        
        public Action<XElement, TItem> Generate<TProperty>(Expression<Func<TItem, ICollection<TProperty>>> propFunc, XmlMapper<TProperty> mapper, string childName)
            where TProperty : class, new()
        {
            var property = propFunc.Body as MemberExpression;
            if (property == null) throw new ArgumentException("Expression does not represent a Property.");

            var generator = new CustomCollectionElementConverterActionGenerator<TItem, TProperty>((PropertyInfo) property.Member,
                                                                                        x => mapper.ToObject(x), childName);
            return generator.Generate();
        }

        private LambdaExpression CreatePropertySetterExpression<TProperty>(
            Expression<Func<TItem, TProperty>> propFunc)
        {
            return new AssignmentBuilder<TItem, TProperty>(propFunc, _setup, _itemParam).Build();
        }

        public Action<TItem> SetupAction
        {
            get
            {
                return _setupAction ??
                       (_setupAction =
                        CompileSetupAction());
            }
        }

        private Action<TItem> CompileSetupAction()
        {
            if (_setup.Count == 0) return _ => { };
            return Expression.Lambda<Action<TItem>>(Expression.Block(_setup.Values), _itemParam).Compile();
        }
    }
}