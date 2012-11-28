namespace XMap
{
    using System;
    using System.Linq.Expressions;

    internal class SimpleActionGenerator<TItem> : IActionGenerator<TItem>
    {
        private readonly LambdaExpression _assignmentLambda;
        private readonly ParameterExpression _stringParam;
        private readonly ParameterExpression _itemParam;

        public SimpleActionGenerator(LambdaExpression assignmentLambda)
        {
            _assignmentLambda = assignmentLambda;
            _stringParam = Expression.Parameter(typeof (string));
            _itemParam = Expression.Parameter(typeof (TItem));
        }

        public Action<string, TItem> Generate()
        {
            return Compile(Expression.Invoke(_assignmentLambda, _itemParam, ConverterExpression.Create(_assignmentLambda.Parameters[1].Type, _stringParam)));
        }

        protected Action<string, TItem> Compile(Expression body)
        {
            return Expression.Lambda<Action<string, TItem>>(body, _stringParam, _itemParam).Compile();
        }
    }
}