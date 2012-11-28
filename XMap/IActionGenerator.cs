namespace XMap
{
    using System;

    internal interface IActionGenerator<in T>
    {
        Action<string, T> Generate();
    }
}