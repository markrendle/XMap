namespace XMap
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    class Helpers
    {
        public static readonly MethodInfo ResizeMethod = typeof (Helpers).GetMethod("Resize",
                                                                                    BindingFlags.Public |
                                                                                    BindingFlags.Static);
        public static T[] Resize<T>(T[] array, int index)
            where T : new()
        {
            if (array.Length > index) return array;
            Array.Resize(ref array, index + 1);

            if (Equals(array[index], default(T)))
            {
                array[index] = new T();
            }
            return array;
        }

        public static readonly MethodInfo FillMethod = typeof (Helpers).GetMethod("Fill",
                                                                                  BindingFlags.Public |
                                                                                  BindingFlags.Static);

        public static void Fill<T>(IList<T> collection, int index)
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
    }
}