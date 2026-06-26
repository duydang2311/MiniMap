using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniMap.SourceGenerators;

[Generator]
public sealed partial class MiniMapGenerator : IIncrementalGenerator
{
    // private static readonly List<string> ATTRIBUTE_FULLY_QUALIFIED_NAMES =
    // [
    //     "MiniMap.MapGetAttribute",
    //     "MiniMap.MapPostAttribute",
    //     "MiniMap.MapPutAttribute",
    //     "MiniMap.MapDeleteAttribute",
    //     "MiniMap.MapPatchAttribute",
    // ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var get = MapModelsProviderFrom(context, "MiniMap.MapGetAttribute");
        var post = MapModelsProviderFrom(context, "MiniMap.MapPostAttribute");
        var put = MapModelsProviderFrom(context, "MiniMap.MapPutAttribute");
        var delete = MapModelsProviderFrom(context, "MiniMap.MapDeleteAttribute");
        var patch = MapModelsProviderFrom(context, "MiniMap.MapPatchAttribute");

        var combined = get.Collect()
            .Combine(post.Collect())
            .Combine(put.Collect())
            .Combine(delete.Collect())
            .Combine(patch.Collect())
            .Select(
                static (tuple, _) =>
                {
                    var ((((get, post), put), delete), patch) = tuple;
                    return get.AddRange(post).AddRange(put).AddRange(delete).AddRange(patch);
                }
            );

        context.RegisterSourceOutput(
            combined,
            static (context, models) =>
            {
                foreach (var group in models.GroupBy(m => m.Id))
                {
                    var list = group.ToList();
                    var model = list[0];
                    if (list.Count == 1)
                    {
                        model.Write(context);
                        continue;
                    }

                    string? pattern = null;
                    foreach (var m in list)
                    {
                        if (!string.IsNullOrEmpty(m.Pattern))
                        {
                            if (pattern is null)
                            {
                                pattern = m.Pattern;
                            }
                            else if (!pattern.Equals(m.Pattern))
                            {
                                // TODO: Add diagnostics error
                                continue;
                            }
                        }
                    }

                    if (pattern is null)
                    {
                        // TODO: Add diagnostics error
                        continue;
                    }

                    model.Methods = [.. list.SelectMany(m => m.Methods).Distinct()];
                    model.Pattern = pattern;
                    model.Write(context);
                }
            }
        );
    }

    private IncrementalValuesProvider<MapModel> MapModelsProviderFrom(
        IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName
    )
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            static (node, ct) =>
                node is ClassDeclarationSyntax cds
                && cds.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
                && cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
            (node, ct) =>
            {
                var symbol = node.TargetSymbol;
                var attribute = symbol
                    .GetAttributes()
                    .FirstOrDefault(a =>
                        a.AttributeClass is not null
                        && fullyQualifiedMetadataName.Contains(
                            a.AttributeClass.Name,
                            StringComparison.Ordinal
                        )
                    );
                var allowAnonymous = symbol
                    .GetAttributes()
                    .Any(a =>
                        a.AttributeClass is not null
                        && a.AttributeClass.Name.Equals("AllowAnonymousAttribute")
                    );
                var authorize = symbol
                    .GetAttributes()
                    .Any(a =>
                        a.AttributeClass is not null
                        && a.AttributeClass.Name.Equals("AuthorizeAttribute")
                    );
                string? pattern = null;
                if (attribute is not null && attribute.ConstructorArguments.Length > 0)
                {
                    pattern = (string)attribute.ConstructorArguments[0].Value!;
                }
                return new MapModel(
                    symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    [MapMethodFromMetadataName(fullyQualifiedMetadataName)],
                    symbol.ContainingNamespace.ToDisplayString(),
                    symbol.Name,
                    pattern,
                    allowAnonymous,
                    authorize
                );
            }
        );
    }

    private static MapMethod MapMethodFromMetadataName(string fullyQualifiedMetadataName)
    {
        return fullyQualifiedMetadataName switch
        {
            "MiniMap.MapGetAttribute" => MapMethod.Get,
            "MiniMap.MapPostAttribute" => MapMethod.Post,
            "MiniMap.MapPutAttribute" => MapMethod.Put,
            "MiniMap.MapDeleteAttribute" => MapMethod.Delete,
            "MiniMap.MapPatchAttribute" => MapMethod.Patch,
            _ => throw new InvalidOperationException(),
        };
    }
}
