namespace Source.GUI.Controls;

public class PanelAnimationMap
{
	public List<PanelAnimationMapEntry> Entries = [];
	public PanelAnimationMap? BaseMap;
	public string? ClassName;
	public PanelAnimationMap(ReadOnlySpan<char> className) {
		ClassName = new string(className);
	}
}

public delegate object PanelGetFunc(Panel panel);
public delegate void PanelSetFunc(Panel panel, object value);

public struct PanelAnimationMapEntry
{
	public string ScriptName;
	public string Variable;
	public string Type;
	public string DefaultValue;
	public bool Array;
	public PanelGetFunc Get;
	public PanelSetFunc Set;
}

public static class PanelAnimationDictionary
{
	static Dictionary<ulong, PanelAnimationMap> AnimationMaps = [];

	public static PanelAnimationMap FindOrAddPanelAnimationMap(ReadOnlySpan<char> className) {
		Panel.InitPropertyConverters();

		ulong hashsymbol = className.Hash();
		if (!AnimationMaps.TryGetValue(hashsymbol, out PanelAnimationMap? map))
			map = AnimationMaps[hashsymbol] = new(className);

		return map;
	}

	public static PanelAnimationMap? FindPanelAnimationMap(ReadOnlySpan<char> className) {
		ulong hashsymbol = className.Hash();
		if (AnimationMaps.TryGetValue(hashsymbol, out PanelAnimationMap? map))
			return map;

		return null;
	}

	public static void PanelAnimationDumpVars(ReadOnlySpan<char> className) {

	}
}