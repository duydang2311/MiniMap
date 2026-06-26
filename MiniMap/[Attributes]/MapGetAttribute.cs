namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MapGetAttribute : Attribute
{
    public MapGetAttribute() { }

    public MapGetAttribute(string pattern) { }
}
