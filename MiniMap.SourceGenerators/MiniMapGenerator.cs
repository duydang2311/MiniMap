using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniMap.SourceGenerators;

[Generator]
public sealed class MiniMapGenerator : IIncrementalGenerator
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

        var transformed = combined.Select(
            (models, ct) =>
            {
                var transformed = new List<MapModel>(models.Length);
                foreach (var group in models.GroupBy(m => m.Id))
                {
                    var list = group.ToList();
                    var model = list[0];
                    if (list.Count == 1)
                    {
                        transformed.Add(model);
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

                    transformed.Add(
                        new MapModel(
                            model.Id,
                            [.. list.SelectMany(m => m.Methods).Distinct()],
                            model.FullNamespaceName,
                            model.ClassName,
                            pattern,
                            model.AllowAnonymous,
                            model.Authorize,
                            model.DisableAntiforgery
                        )
                    );
                }
                return transformed.ToImmutableArray();
            }
        );

        context.RegisterSourceOutput(
            transformed,
            static (context, models) =>
            {
                foreach (var model in models)
                {
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
                var allowAnonymous = false;
                var authorize = false;
                var disableAntiforgery = false;
                AttributeData? attribute = null;
                foreach (var attr in symbol.GetAttributes())
                {
                    if (attr.AttributeClass is null)
                    {
                        continue;
                    }
                    if (
                        !allowAnonymous
                        && attr.AttributeClass.Name.Equals(
                            "AllowAnonymousAttribute",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        allowAnonymous = true;
                    }
                    if (
                        !authorize
                        && attr.AttributeClass.Name.Equals(
                            "AuthorizeAttribute",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        authorize = true;
                    }
                    if (
                        !disableAntiforgery
                        && attr.AttributeClass.Name.Equals(
                            "DisableAntiforgeryAttribute",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        disableAntiforgery = true;
                    }
                    if (
                        attribute is null
                        && fullyQualifiedMetadataName.Contains(
                            attr.AttributeClass.Name,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        attribute = attr;
                    }
                }
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
                    authorize,
                    disableAntiforgery
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
