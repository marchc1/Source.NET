using System.Collections.Concurrent;

namespace Source.GUI.Controls;

public class PanelAnimationMap(ReadOnlySpan<char> className)
{
	public readonly List<PanelAnimationMapEntry> Entries = [];
	public PanelAnimationMap? BaseMap;
	public string? ClassName = new(className);
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
	private static readonly ConcurrentDictionary<ulong, PanelAnimationMap> AnimationMaps = new();

	public static PanelAnimationMap FindOrAddPanelAnimationMap(ReadOnlySpan<char> className) {
		Panel.InitPropertyConverters();
		ulong hashSymbol = className.Hash();
		string name = new string(className);
		return AnimationMaps.GetOrAdd(hashSymbol, static (_, n) => new PanelAnimationMap(n), name);	}

	public static PanelAnimationMap? FindPanelAnimationMap(ReadOnlySpan<char> className) {
		ulong hashSymbol = className.Hash();
		return AnimationMaps.GetValueOrDefault(hashSymbol);
	}

	public static void PanelAnimationDumpVars(ReadOnlySpan<char> className) {
		
	}
}
