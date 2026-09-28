using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Globalization;

namespace Source.Physics;

internal class VPhysicsKeyParser : IVPhysicsKeyParser
{
	readonly byte[] Data;
	int Pos;
	string CurrentBlock = "";

	public VPhysicsKeyParser(ReadOnlySpan<byte> keyData) {
		Data = keyData.ToArray();
		Pos = 0;
		NextBlock();
	}

	string ReadToken() {
		Span<char> tok = stackalloc char[256];
		ReadOnlySpan<byte> rem = FilesystemHelpers.ParseFile(((ReadOnlySpan<byte>)Data)[Pos..], tok, out _);
		Pos = Data.Length - rem.Length;
		return new string(((ReadOnlySpan<char>)tok).SliceNullTerminatedString());
	}

	void NextBlock() => CurrentBlock = ReadToken();

	public ReadOnlySpan<char> GetCurrentBlockName() => CurrentBlock;

	public bool Finished() => CurrentBlock.Length == 0;

	public void SkipBlock() {
		int depth = 0;
		while (true) {
			string t = ReadToken();
			if (t.Length == 0)
				break;
			if (t == "{")
				depth++;
			else if (t == "}") {
				depth--;
				if (depth <= 0)
					break;
			}
		}
		NextBlock();
	}

	static void CopyString(Span<char> dest, ReadOnlySpan<char> src) {
		int n = Math.Min(src.Length, dest.Length - 1);
		src[..n].CopyTo(dest);
		dest[n] = '\0';
	}

	public void ParseSolid(ref Solid solid, IVPhysicsKeyHandler? unknownKeyHandler) {
		// consume the opening brace
		string open = ReadToken();
		if (open != "{") {
			NextBlock();
			return;
		}

		while (true) {
			string key = ReadToken();
			if (key.Length == 0 || key == "}")
				break;

			string value = ReadToken();
			if (value == "}")
				break;

			switch (key.ToLowerInvariant()) {
				case "index":
					if (int.TryParse(value, out int idx))
						solid.Index = idx;
					break;
				case "name":
					CopyString(solid.Name, value);
					break;
				case "parent":
					CopyString(solid.Parent, value);
					break;
				case "surfaceprop":
					CopyString(solid.SurfaceProp, value);
					break;
				case "mass":
					solid.Params.Mass = ParseFloat(value);
					break;
				case "damping":
					solid.Params.Damping = ParseFloat(value);
					break;
				case "rotdamping":
					solid.Params.RotDamping = ParseFloat(value);
					break;
				case "inertia":
					solid.Params.Inertia = ParseFloat(value);
					break;
				case "volume":
					solid.Params.Volume = ParseFloat(value);
					break;
				case "drag":
					solid.Params.DragCoefficient = ParseFloat(value);
					break;
				default:
					unknownKeyHandler?.ParseKeyValue(solid, key, value);
					break;
			}
		}

		NextBlock();
	}

	static float ParseFloat(ReadOnlySpan<char> s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0.0f;

	public void ParseFluid(ref Fluid fluid, IVPhysicsKeyHandler? unknownKeyHandler) => SkipBlock();
	public void ParseRagdollConstraint(ref ConstraintRagdollParams constraint, IVPhysicsKeyHandler? unknownKeyHandler) => SkipBlock();
	public void ParseSurfaceTable(Span<nint> table, IVPhysicsKeyHandler? unknownKeyHandler) => SkipBlock();
	public void ParseCustom(ref object? custom, IVPhysicsKeyHandler? unknownKeyHandler) => SkipBlock();
	public void ParseVehicle(ref VehicleParams vehicle, IVPhysicsKeyHandler? unknownKeyHandler) => SkipBlock();
}
