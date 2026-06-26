namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MapDeleteAttribute : Attribute
{
    public MapDeleteAttribute() { }

    public MapDeleteAttribute(string pattern) { }
}
