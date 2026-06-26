namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MapPostAttribute : Attribute
{
    public MapPostAttribute() { }

    public MapPostAttribute(string pattern) { }
}
