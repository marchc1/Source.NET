
using Source;
using Source.Common;

using System.Collections.Frozen;

while (true) {
	Console.Write("Type a datatable in the path: ");
	string? cppname = Console.ReadLine()?.Trim() ?? throw new Exception();
	string file = Directory.GetFiles(args[0]).First(x => x.Contains(cppname));

	using FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read);
	using StreamReader reader = new(stream);
	Console.WriteLine("Starting...");
	Prop workProp = new();
	List<Prop> props = [];
	bool wroteOneKey = false;

	ReadOnlySpan<char> CSharpifyName(ReadOnlySpan<char> name) {
		if (name.StartsWith("m_")) {
			name = name[2..];

			if (name.StartsWith('i')) name = name[1..];
			else if (name.StartsWith('b')) name = name[1..];
			else if (name.StartsWith('n')) name = name[1..];
			else if (name.StartsWith("f")) name = name[1..];
			else if (name.StartsWith("fl")) name = name[2..];
			else if (name.StartsWith("vec")) name = name[3..];
		}
		string newName = $"{char.ToUpper(name[0])}{name[1..]}";
		return newName;
	}

	while (!reader.EndOfStream) {
		string? line = reader.ReadLine()?.Trim();
		if (line == null)
			continue;

		if (line.Length == 0) {
			if (wroteOneKey) {
				props.Add(workProp);
				workProp = new();
				wroteOneKey = false;
			}
			continue;
		}

		ReadOnlySpan<char> key = line.AsSpan()[..line.IndexOf(':')].Trim();
		ReadOnlySpan<char> val = line.AsSpan()[(line.IndexOf(':') + 1)..].Trim();

		switch (key) {
			case "Index": workProp.Index = int.Parse(val); goto oneKey;
			case "PropName": workProp.PropName = new(CSharpifyName(val)); goto oneKey;
			case "ExcludeName": workProp.ExcludeName = new(val); goto oneKey;
			case "Flags":
				foreach (var piece in val.Split(' '))
					if (piece.Length > 0)
						workProp.Flags.Add(PropFlagsConv.ConvTable[piece]);
				goto oneKey;
			case "Inherited": workProp.Inherited = new(val); goto oneKey;
			case "Type":
				workProp.Type = val switch {
					"DPT_Int" => SendPropType.Int,
					"DPT_Float" => SendPropType.Float,
					"DPT_Vector" => SendPropType.Vector,
					"DPT_VectorXY" => SendPropType.VectorXY,
					"DPT_String" => SendPropType.String,
					"DPT_Array" => SendPropType.Array,
					"DPT_DataTable" => SendPropType.DataTable,
#if GMOD_DLL
					"DPT_GMODTable" => SendPropType.GModTable,
#endif
					_ => throw new InvalidDataException()
				}; goto oneKey;
			case "NumElement": workProp.NumElement = int.Parse(val); goto oneKey;
			case "Bits": workProp.Bits = int.Parse(val); goto oneKey;
			case "HighValue": workProp.HighValue = float.Parse(val); goto oneKey;
			case "LowValue": workProp.LowValue = float.Parse(val); goto oneKey;
		}
		continue;

	oneKey:
		wroteOneKey = true;
	}

	if (wroteOneKey)
		props.Add(workProp);

	// Determine the name of the class without the C prefix. If there is no C prefix, we use the same name on both sides (?)
	string noprefix = cppname.StartsWith('C') ? cppname[1..] : cppname;
	// cl, sv, dt names. The switch statements are independent for extended control
	string clname = $"C_" + noprefix switch {
		"TEBaseBeam" => "BaseBeam",
		_ => noprefix
	};
	string svname = noprefix switch {
		"TEBaseBeam" => "BaseBeam",
		_ => noprefix
	};
	string dtname = "DT_" + noprefix switch {
		"TEBaseBeam" => "BaseBeam",
		_ => noprefix
	};

	string cl_path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Game.Client/" + clname + ".cs"));
	string sv_path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Game.Server/" + svname + ".cs"));

	if (File.Exists(cl_path) || File.Exists(sv_path)) {
		Console.WriteLine("Skipping, already exists.");
		continue;
	}


	Console.WriteLine("Writing to:");
	Console.WriteLine($"    CL: {cl_path}");
	Console.WriteLine($"    SV: {sv_path}");

	using FileStream clWriteStream = File.Open(cl_path, FileMode.Create, FileAccess.Write);
	using FileStream svWriteStream = File.Open(sv_path, FileMode.Create, FileAccess.Write);

	using StreamWriter clWriter = new(clWriteStream);
	using StreamWriter svWriter = new(svWriteStream);

	void writeBoth(ReadOnlySpan<char> line) {
		clWriter.Write(line);
		svWriter.Write(line);
	}

	void writeBothLine(ReadOnlySpan<char> line) {
		clWriter.WriteLine(line);
		svWriter.WriteLine(line);
	}

	// sv?
	void writeDependant(Func<bool, ReadOnlySpan<char>> lineProducer) {
		clWriter.Write(lineProducer(false));
		svWriter.Write(lineProducer(true));
	}
	void writeDependantLine(Func<bool, ReadOnlySpan<char>> lineProducer) {
		clWriter.WriteLine(lineProducer(false));
		svWriter.WriteLine(lineProducer(true));
	}


	writeBothLine("using Source.Common;");
	writeBothLine("using Source;");
	writeBothLine("using Game.Shared;");
	writeBothLine("using System.Numerics;");
	writeDependantLine(isSv => $"namespace Game.{(isSv ? "Server" : "Client")};");
	writeDependantLine(isSv => $"using FIELD = FIELD<{(isSv ? svname : clname)}>;");

	// Determine if the first prop is an inherit datatable, if it is, we're going to guess the inheriting type and remove it.
	Prop? prop = props.FirstOrDefault();
	Prop? inheritProp = null;
	if (prop != null && prop.Type == SendPropType.DataTable && prop.Inherited != null) {
		inheritProp = prop;
		props.RemoveAt(0);
	}

	string? inheritDatatableName = inheritProp?.Inherited;
	string? inheritCsClassName = inheritDatatableName != null ? inheritDatatableName[3..] /* to remove the DT_ */ : null;

	writeDependant(isSv => $"public class {(isSv ? svname : clname)}");
	if (inheritDatatableName != null) {
		string clInheritClassName = "C_" + inheritCsClassName!;
		string svInheritClassName = "" + inheritCsClassName!;
		writeDependantLine(isSv => $" : {(isSv ? svInheritClassName : clInheritClassName)}");

	}
	else
		writeBothLine("");

	writeBothLine("{");
	writeDependant(sv => $"\tpublic static readonly {(sv ? "SendTable" : "RecvTable")} {dtname} = new(");
	if (inheritDatatableName != null)
		writeBoth($"{inheritDatatableName}, ");
	writeBothLine($"[");

	List<Field> fields = [];
	Console.WriteLine($"Props count: {props.Count}");
	foreach (var p in props) {
		string? propFunc = p.Type switch {
			SendPropType.Int => "PropInt",
			SendPropType.Float => "PropFloat",
			SendPropType.Vector => "PropVector",
			_ => null
		};

		if (propFunc == null) {
			// We can identify specific functions sometimes based on values

			bool isLikelyEhandle = p.Bits == Constants.NUM_NETWORKED_EHANDLE_BITS && p.Flags.Count == 1 && p.Flags[0] == PropFlags.Unsigned && p.Type == SendPropType.Int;
			bool isLikelyBoolean = p.Bits == 1 && p.Flags.Count == 1 && p.Flags[0] == PropFlags.Unsigned && p.Type == SendPropType.Int;

			if (isLikelyEhandle)
				propFunc = "PropEHandle";
			else if (isLikelyBoolean)
				propFunc = "PropBool";
			else {
				Console.WriteLine($"Skipping prop '{p.PropName}' - unable to discern more info.");
				continue;
			}
		}

		switch (propFunc) {
			case "PropInt": fields.Add(new() { Name = p.PropName, Type = "int" }); break;
			case "PropFloat": fields.Add(new() { Name = p.PropName, Type = "float" }); break;
			case "PropVector": fields.Add(new() { Name = p.PropName, Type = "Vector3" }); break;
			case "PropEHandle": fields.Add(new() { Name = p.PropName, Type = "readonly EHANDLE" }); break;
			case "PropBool": fields.Add(new() { Name = p.PropName, Type = "bool" }); break;
		}

		writeBoth("\t\t");
		writeDependant(sv => sv ? "Send" : "Recv");
		writeBoth(propFunc);
		writeBoth($"(FIELD.OF(nameof({p.PropName}))");

		// For recv props, we can be done with this here.
		clWriter.WriteLine("),");

		// For send props, write what is necessary if we know anything
		switch (propFunc) {
			case "PropInt": svWriter.Write($", {p.Bits}, {p.JoinFlags()}"); break;
			case "PropFloat": svWriter.Write($", {p.Bits}, {p.JoinFlags()}"); break;
			case "PropVector": svWriter.Write($", {p.Bits}, {p.JoinFlags()}"); break;
		}
		svWriter.WriteLine("),");
	}

	writeBothLine($"\t]);");

	// Write the class

	writeBoth("\tpublic static readonly new ");
	writeDependant(sv => sv ? "ServerClass" : "ClientClass");
	writeBoth(" ");
	writeDependant(sv => sv ? "ServerClass" : "ClientClass");
	writeDependantLine(sv => $" = new {(sv ? "ServerClass" : "ClientClass")}(\"{noprefix}\", {dtname}).WithManualClassID(StaticClassIndices.{cppname});");

	// Try writing fields
	if (fields.Count > 0)
		writeBothLine("");
	foreach (var field in fields) {
		writeBothLine($"\tpublic {field.Type} {field.Name};");
	}

	writeBothLine("}");

	Console.WriteLine("Done. Resetting...");
}
public static class PropFlagsConv
{
	public static readonly FrozenDictionary<string, PropFlags> ConvTable = new Dictionary<string, PropFlags>() {
		{ "None", 0 },
		{ "UNSIGNED", PropFlags.Unsigned },
		{ "COORD", PropFlags.Coord },
		{ "NOSCALE", PropFlags.NoScale },
		{ "ROUNDDOWN", PropFlags.RoundDown },
		{ "ROUNDUP", PropFlags.RoundUp },
		{ "NORMAL", PropFlags.Normal },
		{ "EXCLUDE", PropFlags.Exclude },
		{ "XYZE", PropFlags.XYZExponent },
		{ "INSIDEARRAY", PropFlags.InsideArray},
		{ "PROXY_ALWAYS_YES", PropFlags.ProxyAlwaysYes},
		{ "CHANGES_OFTEN", PropFlags.ChangesOften},
		{ "IS_A_VECTOR_ELEM", PropFlags.IsAVectorElem},
		{ "COORD_MP", PropFlags.CoordMP },
		{ "COORD_MP_LOWPRECISION", PropFlags.CoordMPLowPrecision},
		{ "COORD_MP_INTEGRAL", PropFlags.CoordMPIntegral },
		{ "VARINT", PropFlags.VarInt },
		{ "ENCODED_AGAINST_TICKCOUNT", PropFlags.EncodedAgainstTickCount },
	}.ToFrozenDictionary();
}

public class Field
{
	public required string Type;
	public required string Name;
}

class Prop
{
	public int Index;
	public string PropName = "";
	public string? ExcludeName;
	public string? Inherited;
	public readonly List<PropFlags> Flags = [];
	public SendPropType Type;
	public int NumElement;
	public int Bits;
	public float HighValue;
	public float LowValue;

	public string JoinFlags() {
		if (Flags.Count == 0)
			return "0";

		string[] pieces = new string[Flags.Count];
		for (int i = 0; i < Flags.Count; i++) {
			pieces[i] = Flags[i] == 0 ? "0" : $"PropFlags.{Enum.GetName(Flags[i])}";
		}

		return string.Join(" | ", pieces);
	}
}

