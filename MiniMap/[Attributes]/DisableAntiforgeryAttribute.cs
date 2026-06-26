namespace MiniMap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableAntiforgeryAttribute : Attribute
{
    public DisableAntiforgeryAttribute() { }
}
