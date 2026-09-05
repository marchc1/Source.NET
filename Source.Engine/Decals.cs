using Source.Common.MaterialSystem;

namespace Source.Engine;

public struct DecalEntry
{
	public IMaterial? Material;
	public int Index;
}

public partial class Render
{
	public static readonly Dictionary<FileNameHandle_t, DecalEntry> g_DecalDictionary = [];

	public static readonly ReaderWriterLockSlim g_DecalMutex = new();

	public static readonly List<int> g_DecalLookup = [];

	public static int Draw_DecalMax() {
		throw new NotImplementedException();
	}

	public static IMaterial? Draw_DecalMaterial(int index) {
		throw new NotImplementedException();
	}

	public static void Draw_DecalSetName(int decal, ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public static int Draw_DecalIndexFromName(ReadOnlySpan<char> name, out bool found) {
		throw new NotImplementedException();
	}

	public static ReadOnlySpan<char> Draw_DecalNameFromIndex(int index) {
		throw new NotImplementedException();
	}

	public static void Decal_Init() {
		throw new NotImplementedException();
	}

	public static void Decal_Shutdown() {
		throw new NotImplementedException();
	}
}
