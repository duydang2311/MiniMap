namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MapPutAttribute : Attribute
{
    public MapPutAttribute() { }

    public MapPutAttribute(string pattern) { }
}
