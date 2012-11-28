namespace XMap
{
    using System.Xml.Linq;

    internal static class XEx
    {
        public static string ValueOrNull(this XAttribute attribute)
        {
            return attribute == null ? null : attribute.Value;
        }

        public static string ValueOrEmpty(this XAttribute attribute)
        {
            return attribute == null ? string.Empty : attribute.Value;
        }
    }
}