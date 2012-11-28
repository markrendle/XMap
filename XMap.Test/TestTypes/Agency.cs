namespace XMap.Test.TestTypes
{
    public class Agency
    {
        public string Name { get; set; }
        public Country Country { get; set; }
    }

    public enum Country
    {
        Belgium,
        China,
        USA,
    }
}