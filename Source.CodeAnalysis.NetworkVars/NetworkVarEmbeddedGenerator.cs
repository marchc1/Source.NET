using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Source.CodeAnalysis.NetworkVars
{
	[Generator(LanguageNames.CSharp)]
	public sealed class NetworkVarEmbeddedGenerator : IIncrementalGenerator
	{
		private const string AttributeMetadataName = "Source.Common.NetworkVarEmbeddedAttribute";
		private const string NetworkVarMetadataName = "Source.Common.NetworkVarAttribute";

		private static readonly DiagnosticDescriptor BadDeclaration = new DiagnosticDescriptor(
			id: "NETVAR010",
			title: "[NetworkVarEmbedded] requires a partial property typed as T.NetworkVar",
			messageFormat: "'{0}' must be declared as 'partial T.NetworkVar {0} {{ get; }}' inside a top-level partial class, where T is a top-level partial struct",
			category: "NetworkVars",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			context.RegisterPostInitializationOutput(ctx =>
				ctx.AddSource("NetworkVarEmbeddedAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8)));

			IncrementalValuesProvider<OwnerModel?> owners = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					AttributeMetadataName,
					predicate: static (node, _) => node is PropertyDeclarationSyntax,
					transform: static (ctx, _) => GetModel(ctx))
				.Where(static m => m != null);

			IncrementalValueProvider<bool> clientDll = context.ParseOptionsProvider.Select(static (o, _) => o.PreprocessorSymbolNames.Contains("CLIENT_DLL"));

			context.RegisterSourceOutput(owners.Collect().Combine(clientDll), static (spc, input) => Emit(spc, input.Left, input.Right));
		}

		private static OwnerModel? GetModel(GeneratorAttributeSyntaxContext ctx) {
			var propSyntax = (PropertyDeclarationSyntax)ctx.TargetNode;
			var prop = (IPropertySymbol)ctx.TargetSymbol;
			INamedTypeSymbol owner = prop.ContainingType;

			INamedTypeSymbol? embedded = null;
			bool isClass = prop.Type is INamedTypeSymbol pt && pt.TypeKind == TypeKind.Class;
			if (isClass)
				embedded = (INamedTypeSymbol)prop.Type;
			else if (propSyntax.Type is QualifiedNameSyntax q && q.Right.Identifier.Text == "NetworkVar")
				embedded = ctx.SemanticModel.GetSymbolInfo(q.Left).Symbol as INamedTypeSymbol;

			bool ok = embedded != null && (isClass || embedded.TypeKind == TypeKind.Struct) && embedded.ContainingType == null && !embedded.IsGenericType
				&& owner.ContainingType == null && !owner.IsGenericType
				&& propSyntax.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
				&& IsPartial(owner) && IsPartial(embedded);

			return new OwnerModel(
				propertyName: prop.Name,
				modifiers: string.Join(" ", propSyntax.Modifiers.Where(m => !m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)).Select(m => m.Text)),
				@namespace: owner.ContainingNamespace.IsGlobalNamespace ? null : owner.ContainingNamespace.ToDisplayString(),
				typeName: owner.Name,
				typeFullyQualified: owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				embedded: ok ? (isClass ? new StructModel(embedded!.ContainingNamespace.IsGlobalNamespace ? null : embedded.ContainingNamespace.ToDisplayString(), embedded.Name, embedded.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), new List<MemberModel>(), true) : BuildStruct(embedded!)) : null,
				location: propSyntax.Identifier.GetLocation());
		}

		private static bool IsPartial(INamedTypeSymbol type) => type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.Any(t => t.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));

		private static StructModel BuildStruct(INamedTypeSymbol type) {
			var members = new List<MemberModel>();
			foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>()) {
				if (field.IsStatic || field.IsImplicitlyDeclared)
					continue;
				bool isVar = field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NetworkVarMetadataName);
				bool isEmbedded = field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeMetadataName);
				if (field.DeclaredAccessibility != Accessibility.Public && !isVar && !isEmbedded)
					continue;
				StructModel? nested = isEmbedded && field.Type is INamedTypeSymbol nt && nt.TypeKind == TypeKind.Struct ? BuildStruct(nt) : null;
				string? arrayElement = null;
				if (isVar && field.Type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InlineArrayAttribute"))
					arrayElement = field.Type.GetMembers().OfType<IFieldSymbol>().First(f => !f.IsStatic).Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				string? handleEntity = isVar && field.Type is INamedTypeSymbol ht && ht.Name == "Handle" && ht.TypeArguments.Length == 1 && ht.ContainingNamespace.ToDisplayString() == "Source.Common"
					? ht.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null;
				members.Add(new MemberModel(field.Name, field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), nested, !isVar && !isEmbedded, arrayElement) { HandleEntity = handleEntity });
			}
			return new StructModel(
				type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
				type.Name,
				type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				members);
		}

		private static void Emit(SourceProductionContext spc, ImmutableArray<OwnerModel?> models, bool clientDll) {
			var structs = new Dictionary<string, StructModel>();

			foreach (var group in models.Where(m => m != null).Select(m => m!).GroupBy(m => m.TypeFullyQualified)) {
				var valid = new List<OwnerModel>();
				foreach (OwnerModel model in group) {
					if (model.Embedded == null) {
						spc.ReportDiagnostic(Diagnostic.Create(BadDeclaration, model.Location, model.PropertyName));
						continue;
					}
					valid.Add(model);
					Collect(model.Embedded, structs);
				}
				if (valid.Count == 0)
					continue;

				OwnerModel first = valid[0];
				spc.AddSource((first.Namespace == null ? "" : first.Namespace + ".") + first.TypeName + ".NetworkVarsEmbedded.g.cs", SourceText.From(BuildOwner(first, valid, clientDll), Encoding.UTF8));
			}

			foreach (StructModel s in structs.Values)
				spc.AddSource((s.Namespace == null ? "" : s.Namespace + ".") + s.TypeName + ".NetworkVar.g.cs", SourceText.From(s.IsClass ? BuildClass(s, clientDll) : BuildStruct(s, clientDll), Encoding.UTF8));
		}

		private static void Collect(StructModel s, Dictionary<string, StructModel> structs) {
			if (structs.ContainsKey(s.TypeFullyQualified))
				return;
			structs[s.TypeFullyQualified] = s;
			foreach (MemberModel m in s.Members)
				if (m.Nested != null)
					Collect(m.Nested, structs);
		}

		private static void Leaves(StructModel s, string path, List<(string Path, bool Array)> output) {
			foreach (MemberModel m in s.Members) {
				if (m.Plain)
					continue;
				if (m.Nested != null)
					Leaves(m.Nested, path + "." + m.Name, output);
				else
					output.Add((path + "." + m.Name, m.ArrayElement != null));
			}
		}

		private static string BuildOwner(OwnerModel type, List<OwnerModel> props, bool clientDll) {
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated/>");
			sb.AppendLine("#nullable enable");
			sb.AppendLine();

			bool hasNamespace = type.Namespace != null;
			string classIndent = hasNamespace ? "\t" : "";
			string indent = classIndent + "\t";

			if (hasNamespace) {
				sb.Append("namespace ").AppendLine(type.Namespace);
				sb.AppendLine("{");
			}

			sb.Append(classIndent).Append("partial class ").Append(type.TypeName).AppendLine(" : global::Source.Common.INetworkStateChanged");
			sb.Append(classIndent).AppendLine("{");

			foreach (OwnerModel p in props) {
				string t = p.Embedded!.TypeFullyQualified;
				string backing = "__nv_" + p.PropertyName;
				if (p.Embedded.IsClass) {
					sb.Append(indent).Append("private readonly ").Append(t).Append(' ').Append(backing).AppendLine(" = new();");
					if (clientDll)
						sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(' ').Append(p.PropertyName).Append(" => ").Append(backing).AppendLine(";");
					else {
						sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(' ').Append(p.PropertyName).AppendLine(" {");
						sb.Append(indent).AppendLine("	get {");
						sb.Append(indent).Append("		").Append(backing).AppendLine(".__nv_Outer ??= this;");
						sb.Append(indent).Append("		return ").Append(backing).AppendLine(";");
						sb.Append(indent).AppendLine("	}");
						sb.Append(indent).AppendLine("}");
					}
					sb.AppendLine();
					continue;
				}
				sb.Append(indent).Append("private ").Append(t).Append(' ').Append(backing).Append(" = new();").AppendLine();
				sb.Append(indent).Append(p.Modifiers).Append(" partial ").Append(t).Append(".NetworkVar ").Append(p.PropertyName)
					.Append(" => new(ref ").Append(backing).Append(", this, NetworkVarFields.").Append(p.PropertyName).AppendLine(", 0);");
				sb.AppendLine();
			}

			if (clientDll)
				sb.Append(indent).AppendLine("void global::Source.Common.INetworkStateChanged.NetworkStateChanged(global::Source.Common.IFieldAccessor accessor) { }");

			sb.Append(indent).AppendLine("internal static partial class NetworkVarFields");
			sb.Append(indent).AppendLine("{");
			foreach (OwnerModel p in props) {
				if (p.Embedded!.IsClass)
					continue;
				var leaves = new List<(string Path, bool Array)>();
				Leaves(p.Embedded!, p.PropertyName, leaves);
				foreach (var (leaf, array) in leaves)
					if (array)
						sb.Append(indent).Append("\tpublic static readonly global::Source.Common.DynamicArrayAccessor ").Append(leaf.Replace('.', '_'))
							.Append(" = new(typeof(").Append(type.TypeFullyQualified).Append("), \"__nv_").Append(leaf).AppendLine("\");");
					else sb.Append(indent).Append("\tpublic static readonly global::Source.Common.IFieldAccessor ").Append(leaf.Replace('.', '_'))
						.Append(" = new global::Source.Common.DynamicAccessor(typeof(").Append(type.TypeFullyQualified).Append("), \"__nv_").Append(leaf).Append("\", \"").Append(leaf).AppendLine("\");");
				sb.Append(indent).Append("\tpublic static readonly global::Source.Common.IFieldAccessor[] ").Append(p.PropertyName).Append(" = [")
					.Append(string.Join(", ", leaves.Select(l => l.Path.Replace('.', '_')))).AppendLine("];");
			}
			sb.Append(indent).AppendLine("}");

			sb.Append(classIndent).AppendLine("}");
			if (hasNamespace)
				sb.AppendLine("}");
			return sb.ToString();
		}

		private static string BuildClass(StructModel s, bool clientDll) {
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated/>");
			sb.AppendLine("#nullable enable");
			sb.AppendLine();

			bool hasNamespace = s.Namespace != null;
			string typeIndent = hasNamespace ? "	" : "";
			string indent = typeIndent + "	";

			if (hasNamespace) {
				sb.Append("namespace ").AppendLine(s.Namespace);
				sb.AppendLine("{");
			}

			sb.Append(typeIndent).Append("partial class ").Append(s.TypeName).AppendLine(" : global::Source.Common.INetworkStateChanged");
			sb.Append(typeIndent).AppendLine("{");
			sb.Append(indent).AppendLine("internal global::Source.Common.INetworkStateChanged? __nv_Outer;");
			if (clientDll)
				sb.Append(indent).AppendLine("public void NetworkStateChanged(global::Source.Common.IFieldAccessor accessor) { }");
			else
				sb.Append(indent).AppendLine("public void NetworkStateChanged(global::Source.Common.IFieldAccessor accessor) => __nv_Outer?.NetworkStateChanged(accessor);");
			sb.Append(typeIndent).AppendLine("}");
			if (hasNamespace)
				sb.AppendLine("}");
			return sb.ToString();
		}

		private static int LeafCount(StructModel s) => s.Members.Sum(m => m.Plain ? 0 : m.Nested != null ? LeafCount(m.Nested) : 1);

		private static string BuildStruct(StructModel s, bool clientDll) {
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated/>");
			sb.AppendLine("#nullable enable");
			sb.AppendLine();

			bool hasNamespace = s.Namespace != null;
			string typeIndent = hasNamespace ? "\t" : "";
			string indent = typeIndent + "\t";
			string body = indent + "\t";

			if (hasNamespace) {
				sb.Append("namespace ").AppendLine(s.Namespace);
				sb.AppendLine("{");
			}

			sb.Append(typeIndent).Append("partial struct ").AppendLine(s.TypeName);
			sb.Append(typeIndent).AppendLine("{");
			sb.Append(indent).AppendLine("public ref struct NetworkVar");
			sb.Append(indent).AppendLine("{");
			sb.Append(body).Append("readonly ref ").Append(s.TypeFullyQualified).AppendLine(" Value;");
			sb.Append(body).AppendLine("readonly global::Source.Common.INetworkStateChanged Outer;");
			sb.Append(body).AppendLine("readonly global::Source.Common.IFieldAccessor[] Fields;");
			sb.Append(body).AppendLine("readonly int Offset;");
			sb.AppendLine();
			sb.Append(body).Append("public NetworkVar(ref ").Append(s.TypeFullyQualified).AppendLine(" value, global::Source.Common.INetworkStateChanged outer, global::Source.Common.IFieldAccessor[] fields, int offset) {");
			sb.Append(body).AppendLine("\tValue = ref value;");
			sb.Append(body).AppendLine("\tOuter = outer;");
			sb.Append(body).AppendLine("\tFields = fields;");
			sb.Append(body).AppendLine("\tOffset = offset;");
			sb.Append(body).AppendLine("}");
			sb.AppendLine();
			sb.Append(body).Append("public readonly ref readonly ").Append(s.TypeFullyQualified).AppendLine(" Get() => ref Value;");
			sb.Append(body).Append("public static implicit operator ").Append(s.TypeFullyQualified).AppendLine("(NetworkVar v) => v.Value;");

			int index = 0;
			foreach (MemberModel m in s.Members) {
				sb.AppendLine();
				if (m.Plain) {
					sb.Append(body).Append("public readonly ref ").Append(m.Type).Append(' ').Append(m.Name).Append(" => ref Value.").Append(m.Name).AppendLine(";");
					continue;
				}
				if (m.HandleEntity != null) {
					sb.Append(body).Append("public readonly global::Game.Shared.NetworkHandle<").Append(m.HandleEntity).Append("> ").Append(m.Name)
						.Append(" => new(ref Value.").Append(m.Name).Append(", Outer, Fields[Offset + ").Append(index).AppendLine("]);");
					index++;
					continue;
				}
				if (m.ArrayElement != null) {
					sb.Append(body).Append("public readonly global::Source.Common.NetworkArray<").Append(m.Type).Append(", ").Append(m.ArrayElement).Append("> ").Append(m.Name)
						.Append(" => new(Value.").Append(m.Name).Append(", Outer, (global::Source.Common.DynamicArrayAccessor)Fields[Offset + ").Append(index).AppendLine("]);");
					index++;
					continue;
				}
				if (m.Nested != null) {
					sb.Append(body).Append("public readonly ").Append(m.Type).Append(".NetworkVar ").Append(m.Name)
						.Append(" => new(ref Value.").Append(m.Name).Append(", Outer, Fields, Offset + ").Append(index).AppendLine(");");
					index += LeafCount(m.Nested);
					continue;
				}
				sb.Append(body).Append("public readonly ").Append(m.Type).Append(' ').Append(m.Name).AppendLine(" {");
				sb.Append(body).Append("\tget => Value.").Append(m.Name).AppendLine(";");
				if (clientDll)
					sb.Append(body).Append("\tset => Value.").Append(m.Name).AppendLine(" = value;");
				else {
					sb.Append(body).AppendLine("\tset {");
					sb.Append(body).Append("\t\tif (!global::System.Collections.Generic.EqualityComparer<").Append(m.Type).Append(">.Default.Equals(Value.").Append(m.Name).AppendLine(", value)) {");
					sb.Append(body).Append("\t\t\tValue.").Append(m.Name).AppendLine(" = value;");
					sb.Append(body).Append("\t\t\tOuter.NetworkStateChanged(Fields[Offset + ").Append(index).AppendLine("]);");
					sb.Append(body).AppendLine("\t\t}");
					sb.Append(body).AppendLine("\t}");
				}
				sb.Append(body).AppendLine("}");
				sb.Append(body).Append("public readonly ref ").Append(m.Type).Append(' ').Append(m.Name).AppendLine("ForModify() {");
				if (!clientDll)
					sb.Append(body).Append("\tOuter.NetworkStateChanged(Fields[Offset + ").Append(index).AppendLine("]);");
				sb.Append(body).Append("\treturn ref Value.").Append(m.Name).AppendLine(";");
				sb.Append(body).AppendLine("}");
				index++;
			}

			sb.Append(indent).AppendLine("}");
			sb.Append(typeIndent).AppendLine("}");
			if (hasNamespace)
				sb.AppendLine("}");
			return sb.ToString();
		}

		private const string AttributeSource =
@"// <auto-generated/>
#nullable enable
namespace Source.Common
{
	[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	internal sealed class NetworkVarEmbeddedAttribute : global::System.Attribute
	{
	}
}";

		private sealed class MemberModel
		{
			public MemberModel(string name, string type, StructModel? nested, bool plain, string? arrayElement) {
				ArrayElement = arrayElement;
				Plain = plain;
				Name = name;
				Type = type;
				Nested = nested;
			}
			public string Name { get; }
			public string Type { get; }
			public StructModel? Nested { get; }
			public bool Plain { get; }
			public string? ArrayElement { get; }
			public string? HandleEntity { get; set; }
		}

		private sealed class StructModel
		{
			public StructModel(string? @namespace, string typeName, string typeFullyQualified, List<MemberModel> members, bool isClass = false) {
				IsClass = isClass;
				Namespace = @namespace;
				TypeName = typeName;
				TypeFullyQualified = typeFullyQualified;
				Members = members;
			}
			public string? Namespace { get; }
			public string TypeName { get; }
			public string TypeFullyQualified { get; }
			public List<MemberModel> Members { get; }
			public bool IsClass { get; }
		}

		private sealed class OwnerModel
		{
			public OwnerModel(string propertyName, string modifiers, string? @namespace, string typeName, string typeFullyQualified, StructModel? embedded, Location location) {
				PropertyName = propertyName;
				Modifiers = modifiers;
				Namespace = @namespace;
				TypeName = typeName;
				TypeFullyQualified = typeFullyQualified;
				Embedded = embedded;
				Location = location;
			}
			public string PropertyName { get; }
			public string Modifiers { get; }
			public string? Namespace { get; }
			public string TypeName { get; }
			public string TypeFullyQualified { get; }
			public StructModel? Embedded { get; }
			public Location Location { get; }
		}
	}
}
