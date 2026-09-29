using Source.Common.MaterialSystem;

namespace Source.Engine;

public struct DecalEntry
{
	public IMaterial? Material;
	public int Index;
}

public partial class Render
{
	static readonly int DECAL_DICTIONARY_INVALID_INDEX = -1;

	public static readonly List<DecalEntry> g_DecalDictionary = [];
	static readonly Dictionary<FileNameHandle_t, int> g_DecalDictionaryIndices = [];

	public static readonly List<int> g_DecalLookup = [];

	public static int Draw_DecalMax() => g_MaxDecals;

	public static IMaterial? Draw_DecalMaterial(int index) {
		if (index < 0 || index >= g_DecalLookup.Count)
			return null;

		int slot = g_DecalLookup[index];
		if (slot < 0 || slot >= g_DecalDictionary.Count)
			return null;

		DecalEntry entry = g_DecalDictionary[slot];
		return entry.Material;
	}

#if !SWDS
	public static void Draw_DecalSetName(int decal, ReadOnlySpan<char> name) {
		while (decal >= g_DecalLookup.Count)
			g_DecalLookup.Add(DECAL_DICTIONARY_INVALID_INDEX);

		FileNameHandle_t fnHandle = g_pFileSystem.FindOrAddFileName(name);
		if (!g_DecalDictionaryIndices.TryGetValue(fnHandle, out int lookup)) {
			DecalEntry entry = new() {
				Material = MatSys.GL_LoadMaterial(name, MaterialDefines.TEXTURE_GROUP_DECAL),
				Index = decal
			};

			lookup = g_DecalDictionary.Count;
			g_DecalDictionary.Add(entry);
			g_DecalDictionaryIndices[fnHandle] = lookup;
		}
		else {
			DecalEntry entry = g_DecalDictionary[lookup];
			entry.Index = decal;
			g_DecalDictionary[lookup] = entry;
		}

		g_DecalLookup[decal] = lookup;
	}

	public static int Draw_DecalIndexFromName(ReadOnlySpan<char> name, out bool found) {
		FileNameHandle_t fnHandle = g_pFileSystem.FindOrAddFileName(name);
		if (!g_DecalDictionaryIndices.TryGetValue(fnHandle, out int lookup)) {
			found = false;
			return 0;
		}

		found = true;
		return g_DecalDictionary[lookup].Index;
	}
#endif

	public static ReadOnlySpan<char> Draw_DecalNameFromIndex(int index) =>
#if !SWDS
	g_DecalDictionary[index].Material != null ? g_DecalDictionary[index].Material!.GetName() : ""
#else
""
#endif
;

	public static void Decal_Init() => Decal_Shutdown();

	public static void Decal_Shutdown() {
		for (int index = 0; index < g_DecalDictionary.Count; index++) {
			IMaterial? mat = g_DecalDictionary[index].Material;
			if (mat != null)
				MatSys.GL_UnloadMaterial(mat);
		}
		g_DecalLookup.Clear();
		g_DecalDictionary.Clear();
		g_DecalDictionaryIndices.Clear();
	}
}
