using System.Globalization;
using System.Linq.Expressions;

namespace XMap
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    class Helpers
    {
        public static readonly MethodInfo ResizeAndAddNewMethod = typeof (Helpers).GetMethod("ResizeAndAddNew",
                                                                                    BindingFlags.Public |
                                                                                    BindingFlags.Static);
        /// <summary>
        /// Resizes an array if necessary and adds a new instance of the array type at the index point.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="array">The array to resize.</param>
        /// <param name="index">The index which must exist in the array.</param>
        /// <returns>The resized array with an instance in the index element.</returns>
        public static T[] ResizeAndAddNew<T>(T[] array, int index)
            where T : new()
        {
            if (array.Length <= index)
            {
                Array.Resize(ref array, index + 1);
            }

            if (Equals(array[index], default(T)))
            {
                array[index] = new T();
            }
            return array;
        }

        public static readonly MethodInfo FillAndAddNewMethod = typeof (Helpers).GetMethod("FillAndAddNew",
                                                                                  BindingFlags.Public |
                                                                                  BindingFlags.Static);

        public static void FillAndAddNew<T>(IList<T> collection, int index)
            where T : new()
        {
            while (collection.Count <= index)
            {
                collection.Add(default(T));
            }

            if (Equals(collection[index], default(T)))
            {
                collection[index] = new T();
            }
        }

        public static readonly MethodInfo NonGenericFillMethod = typeof (Helpers).GetMethod("NonGenericFill",
                                                                                            BindingFlags.Public |
                                                                                            BindingFlags.Static);

        public static void NonGenericFill(IList collection, int index)
        {
            while (collection.Count <= index)
            {
                collection.Add(null);
            }
        }

        public static MethodInfo NewArrayMethod = typeof (Helpers).GetMethod("NewArray",
                                                                             BindingFlags.Public | BindingFlags.Static);

        public static T[] NewArray<T>()
        {
            return new T[0];
        }

        public static MethodInfo SafeToStringMethod = typeof (Helpers).GetMethod("SafeToString",
                                                                             BindingFlags.Public | BindingFlags.Static);
        public static string SafeToString<T>(T value)
            where T : class
        {
            return value == null ? null : value.ToString();
        }

        /// <summary>
        /// Returns the ToString() call for something.
        /// </summary>
        /// <typeparam name="TProperty">The type to call ToString on.</typeparam>
        /// <param name="invoke">The expression which returns the instance of the type.</param>
        /// <returns>A <see cref="MethodCallExpression"/> pointing to the best ToString method.</returns>
        public static MethodCallExpression MakeToStringCall<TProperty>(Expression invoke)
        {
            MethodCallExpression toString;
            if (typeof (TProperty).IsValueType)
            {
                MethodInfo toStringMethod = typeof (TProperty).GetMethod("ToString", new[] {typeof (CultureInfo)});
                if (toStringMethod != null)
                {
// ReSharper disable PossiblyMistakenUseOfParamsMethod
                    toString = Expression.Call(invoke, toStringMethod, Expression.Constant(CultureInfo.CurrentCulture));
// ReSharper restore PossiblyMistakenUseOfParamsMethod
                }
                else
                {
                    toStringMethod = typeof (TProperty).GetMethod("ToString", Type.EmptyTypes);
                    toString = Expression.Call(invoke, toStringMethod);
                }
            }
            else
            {
                MethodInfo toStringMethod = SafeToStringMethod.MakeGenericMethod(typeof (TProperty));
                toString = Expression.Call(toStringMethod, invoke);
            }
            return toString;
        }
    }
}