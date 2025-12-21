global using static Game.Client.ClassMap;

using Source;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

class ClassEntry
{
	public ClassEntry() {

	}

	public ReadOnlySpan<char> GetMapName() => ((Span<char>)MapName).SliceNullTerminatedString();
	public void SetMapName(ReadOnlySpan<char> newname) => strcpy(MapName, newname);

	public DispatchFunction Factory = null!;
	private InlineArray40<char> MapName;
}

public class ClassMap : IClassMap
{
	public static readonly ClassMap g_ClassMap = new();
	public static ClassMap GetClassMap() => g_ClassMap;

	readonly Dictionary<ulong, ClassEntry> ClassDict = [];

	public void Add(ReadOnlySpan<char> mapname, ReadOnlySpan<char> classname, DispatchFunction? factory = null) {
		ReadOnlySpan<char> map = Lookup(classname);
		if (!map.IsEmpty && map.Equals(mapname, StringComparison.Ordinal))
			return;

		if (!map.IsEmpty)
			ClassDict.Remove(classname.Hash(false));

		ClassEntry element = new();
		element.SetMapName(mapname);
		element.Factory = factory!;
		ClassDict[classname.Hash(false)] = element;
	}

	public C_BaseEntity? CreateEntity(ReadOnlySpan<char> mapname) {
		foreach (var kvp in ClassDict) {
			ClassEntry lookup = kvp.Value;
			if (stricmp(lookup.GetMapName(), mapname) != 0)
				continue;

			if (lookup.Factory == null) {
#if DEBUG
				Msg($"No factory for {lookup.GetMapName()}\n");
#endif
				continue;
			}

			return lookup.Factory();
		}

		return null;
	}

	public ReadOnlySpan<char> Lookup(ReadOnlySpan<char> classname) {
		if (!ClassDict.TryGetValue(classname.Hash(false), out var entry))
			return null;

		return entry.GetMapName();
	}
}
