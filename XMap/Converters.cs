namespace XMap
{
    using System;
    using System.Reflection;

    internal static class Converters
    {
        public static readonly MethodInfo ParseEnumMethod = GetMyMethod("ParseEnum");
        public static readonly MethodInfo ChangeTypeMethod = GetMyMethod("ChangeType");
 
        public static T ParseEnum<T>(string value)
            where T : struct
        {
            T result;
            if (!Enum.TryParse(value, out result))
            {
                int interim;
                if (int.TryParse(value, out interim))
                {
                    try
                    {
                        result = (T)(object)interim;
                    }
                    catch (InvalidCastException)
                    {
                        result = default(T);
                    }
                }
            }
            return result;
        }

        public static T ChangeType<T>(string value)
        {
            try
            {
                return (T) Convert.ChangeType(value, typeof (T));
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        private static MethodInfo GetMyMethod(string name)
        {
            return typeof(Converters).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
        }
    }
}