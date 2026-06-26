namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MapPatchAttribute : Attribute
{
    public MapPatchAttribute() { }

    public MapPatchAttribute(string pattern) { }
}
