using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Source.CodeAnalysis.NetworkVars
{
	[Generator(Microsoft.CodeAnalysis.LanguageNames.CSharp)]
	public sealed class NetworkVarGenerator : IIncrementalGenerator
	{
		private const string AttributeMetadataName = "Source.Common.NetworkVarAttribute";

		private static readonly DiagnosticDescriptor NotPartialProperty = new DiagnosticDescriptor(
			id: "NETVAR001",
			title: "[NetworkVar] requires a partial property",
			messageFormat: "'{0}' is marked [NetworkVar] but is not a partial property; the generator cannot emit its backing storage",
			category: "NetworkVars",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor NotPartialType = new DiagnosticDescriptor(
			id: "NETVAR002",
			title: "[NetworkVar] requires a partial containing class",
			messageFormat: "'{0}' contains [NetworkVar] properties but the containing type '{1}' is not partial",
			category: "NetworkVars",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor UnsupportedType = new DiagnosticDescriptor(
			id: "NETVAR003",
			title: "[NetworkVar] only supports top-level non-generic classes",
			messageFormat: "'{0}' contains [NetworkVar] properties but the containing type '{1}' is nested or generic, which is not supported yet",
			category: "NetworkVars",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			context.RegisterPostInitializationOutput(ctx =>
				ctx.AddSource("NetworkVarAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8)));

			IncrementalValuesProvider<PropertyModel?> properties = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					AttributeMetadataName,
					predicate: static (node, _) => node is PropertyDeclarationSyntax,
					transform: static (ctx, _) => GetModel(ctx))
				.Where(static m => m != null);

			IncrementalValueProvider<ImmutableArray<PropertyModel?>> collected = properties.Collect();
			IncrementalValueProvider<bool> clientDll = context.ParseOptionsProvider.Select(static (o, _) => o.PreprocessorSymbolNames.Contains("CLIENT_DLL"));

			context.RegisterSourceOutput(collected.Combine(clientDll), static (spc, input) => Emit(spc, input.Left, input.Right));
		}

		private static PropertyModel? GetModel(GeneratorAttributeSyntaxContext ctx) {
			if (!(ctx.TargetSymbol is IPropertySymbol prop))
				return null;

			INamedTypeSymbol containingType = prop.ContainingType;
			var propSyntax = (PropertyDeclarationSyntax)ctx.TargetNode;

			bool propertyIsPartial = propSyntax.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));

			bool typeIsPartial = containingType.DeclaringSyntaxReferences
				.Select(r => r.GetSyntax())
				.OfType<TypeDeclarationSyntax>()
				.Any(t => t.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));

			bool unsupported = containingType.ContainingType != null || containingType.IsGenericType;

			string? arrayStorage = null;
			if (prop.Type is INamedTypeSymbol arr && arr.Name == "NetworkArray" && arr.TypeArguments.Length == 2 && arr.ContainingNamespace.ToDisplayString() == "Source.Common")
				arrayStorage = arr.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			string? handleStorage = null;
			if (prop.Type is INamedTypeSymbol hnd && hnd.Name == "NetworkHandle" && hnd.TypeArguments.Length == 1)
				handleStorage = "global::Source.Common.Handle<" + hnd.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">";

			string modifiers = string.Join(" ", propSyntax.Modifiers.Where(m => !m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)).Select(m => m.Text));

			return new PropertyModel(
				propertyName: prop.Name,
				modifiers: modifiers,
				arrayStorage: arrayStorage,
				handleStorage: handleStorage,
				newBacking: prop.Type.IsReferenceType && prop.NullableAnnotation != NullableAnnotation.Annotated && prop.Type is INamedTypeSymbol rt && rt.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public),
				typeDisplay: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				@namespace: containingType.ContainingNamespace.IsGlobalNamespace
					? null
					: containingType.ContainingNamespace.ToDisplayString(),
				typeName: containingType.Name,
				typeFullyQualified: containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				propertyIsPartial: propertyIsPartial,
				typeIsPartial: typeIsPartial,
				unsupported: unsupported,
				location: propSyntax.Identifier.GetLocation());
		}

		private static void Emit(SourceProductionContext spc, ImmutableArray<PropertyModel?> models, bool clientDll) {
			foreach (var group in models
				.Where(m => m != null)
				.Select(m => m!)
				.GroupBy(m => m.TypeFullyQualified)) {

				List<PropertyModel> valid = new List<PropertyModel>();
				PropertyModel first = group.First();

				foreach (PropertyModel model in group) {
					if (!model.PropertyIsPartial) {
						spc.ReportDiagnostic(Diagnostic.Create(NotPartialProperty, model.Location, model.PropertyName));
						continue;
					}
					if (model.Unsupported) {
						spc.ReportDiagnostic(Diagnostic.Create(UnsupportedType, model.Location, model.PropertyName, model.TypeName));
						continue;
					}
					if (!model.TypeIsPartial) {
						spc.ReportDiagnostic(Diagnostic.Create(NotPartialType, model.Location, model.PropertyName, model.TypeName));
						continue;
					}
					valid.Add(model);
				}

				if (valid.Count == 0)
					continue;

				string source = BuildSource(first, valid, clientDll);
				string hintName = (first.Namespace == null ? "" : first.Namespace + ".") + first.TypeName + ".NetworkVars.g.cs";
				spc.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
			}
		}

		private static string BuildSource(PropertyModel type, List<PropertyModel> props, bool clientDll) {
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated/>");
			sb.AppendLine("#nullable enable");
			sb.AppendLine();

			bool hasNamespace = type.Namespace != null;
			string indent = hasNamespace ? "\t\t" : "\t";

			if (hasNamespace) {
				sb.Append("namespace ").AppendLine(type.Namespace);
				sb.AppendLine("{");
			}

			string classIndent = hasNamespace ? "\t" : "";
			sb.Append(classIndent).Append("partial class ").Append(type.TypeName);
			if (!clientDll && props.Any(p => p.ArrayStorage != null || p.HandleStorage != null))
				sb.Append(" : global::Source.Common.INetworkStateChanged");
			sb.AppendLine();
			sb.Append(classIndent).AppendLine("{");

			foreach (PropertyModel p in props) {
				string t = p.TypeDisplay;
				string name = p.PropertyName;
				string backing = "__nv_" + name;

				if (p.HandleStorage != null) {
					sb.Append(indent).Append("private ").Append(p.HandleStorage).Append(' ').Append(backing).AppendLine(" = new();");
					sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(' ').Append(name).Append(" => new(ref ").Append(backing)
						.Append(clientDll ? ", null" : ", this").Append(", NetworkVarFields.").Append(name).AppendLine(");");
					sb.AppendLine();
					continue;
				}
				if (p.ArrayStorage != null) {
					sb.Append(indent).Append("private ").Append(p.ArrayStorage).Append(' ').Append(backing).AppendLine(";");
					sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(' ').Append(name).Append(" => new(").Append(backing)
						.Append(clientDll ? ", null" : ", this").Append(", NetworkVarFields.").Append(name).AppendLine(");");
					sb.AppendLine();
					continue;
				}

				sb.Append(indent).Append("private ").Append(t).Append(' ').Append(backing).AppendLine(p.NewBacking ? " = new();" : ";");
				sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(' ').Append(name).AppendLine(" {");
				sb.Append(indent).Append("\tget => ").Append(backing).AppendLine(";");
				if (clientDll)
					sb.Append(indent).Append("\tset => ").Append(backing).AppendLine(" = value;");
				else {
					sb.Append(indent).AppendLine("\tset {");
					sb.Append(indent).Append("\t\tif (!global::System.Collections.Generic.EqualityComparer<").Append(t).Append(">.Default.Equals(").Append(backing).AppendLine(", value)) {");
					sb.Append(indent).Append("\t\t\t").Append(backing).AppendLine(" = value;");
					sb.Append(indent).Append("\t\t\tNetworkStateChanged(NetworkVarFields.").Append(name).AppendLine(");");
					sb.Append(indent).AppendLine("\t\t}");
					sb.Append(indent).AppendLine("\t}");
				}
				sb.Append(indent).AppendLine("}");
				sb.Append(indent).Append(p.Modifiers.Split(' ').Contains("new") ? "public new ref " : "public ref ").Append(t).Append(' ').Append(name).AppendLine("ForModify() {");
				if (!clientDll)
					sb.Append(indent).Append("\tNetworkStateChanged(NetworkVarFields.").Append(name).AppendLine(");");
				sb.Append(indent).Append("\treturn ref ").Append(backing).AppendLine(";");
				sb.Append(indent).AppendLine("}");
				sb.AppendLine();
			}

			sb.Append(indent).AppendLine("internal static partial class NetworkVarFields");
			sb.Append(indent).AppendLine("{");
			foreach (PropertyModel p in props) {
				if (p.ArrayStorage != null) {
					sb.Append(indent).Append("\tpublic static readonly global::Source.Common.DynamicArrayAccessor ").Append(p.PropertyName)
						.Append(" = new(typeof(").Append(type.TypeFullyQualified).Append("), \"__nv_").Append(p.PropertyName).AppendLine("\");");
					continue;
				}
				sb.Append(indent).Append("\tpublic static readonly global::Source.Common.IFieldAccessor ").Append(p.PropertyName)
					.Append(" = new global::Source.Common.DynamicAccessor(typeof(").Append(type.TypeFullyQualified).Append("), \"__nv_").Append(p.PropertyName).Append("\", \"").Append(p.PropertyName).AppendLine("\");");
			}
			sb.Append(indent).AppendLine("}");

			sb.Append(classIndent).AppendLine("}");

			if (hasNamespace)
				sb.AppendLine("}");

			return sb.ToString();
		}

		private const string AttributeSource =
@"// <auto-generated/>
#nullable enable
namespace Source.Common
{
	/// <summary>
	/// Marks a partial property as a networked var. The NetworkVar source generator emits the
	/// backing field, a change-checked setter that calls NetworkStateChanged, and a shared
	/// IFieldAccessor (exposed via the nested NetworkVarFields class) for the send table to use.
	/// </summary>
	[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	internal sealed class NetworkVarAttribute : global::System.Attribute
	{
	}
}";

		private sealed class PropertyModel
		{
			public PropertyModel(string propertyName, string modifiers, string? arrayStorage, string? handleStorage, bool newBacking, string typeDisplay, string? @namespace, string typeName,
				string typeFullyQualified, bool propertyIsPartial, bool typeIsPartial, bool unsupported, Location location) {
				PropertyName = propertyName;
				Modifiers = modifiers;
				ArrayStorage = arrayStorage;
				HandleStorage = handleStorage;
				NewBacking = newBacking;
				TypeDisplay = typeDisplay;
				Namespace = @namespace;
				TypeName = typeName;
				TypeFullyQualified = typeFullyQualified;
				PropertyIsPartial = propertyIsPartial;
				TypeIsPartial = typeIsPartial;
				Unsupported = unsupported;
				Location = location;
			}

			public string PropertyName { get; }
			public string Modifiers { get; }
			public string? ArrayStorage { get; }
			public string? HandleStorage { get; }
			public bool NewBacking { get; }
			public string TypeDisplay { get; }
			public string? Namespace { get; }
			public string TypeName { get; }
			public string TypeFullyQualified { get; }
			public bool PropertyIsPartial { get; }
			public bool TypeIsPartial { get; }
			public bool Unsupported { get; }
			public Location Location { get; }
		}
	}
}
