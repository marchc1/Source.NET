global using static Source.Engine.Disp;

namespace Source.Engine;

public static class Disp
{
	public static readonly List<byte> g_DispLMAlpha = [];
	public static readonly List<byte> g_DispLightmapSamplePositions = [];
	public static readonly List<DispGroup> g_DispGroups = [];
}
