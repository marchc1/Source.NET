using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Source.Engine;

public class DispInfo : IDispInfo {
	public DispArray? DispArray;
	nint ParentSurfPointer;
	readonly WeakReference<WorldBrushData> brushData = new(null!);

	public ref BSPMSurface2 ParentSurfID => ref GetParent();

	public static DispInfo? GetModelDisp(Model world, int i){
		return (DispInfo?)DispInfo_IndexArray(world.Brush.Shared!.DispInfos, i);
	}

	private static IDispInfo? DispInfo_IndexArray(object? oArray, int i) {
		DispArray? array = (DispArray?)oArray;
		if (array == null)
			return null;
		return array.DispInfos[i];
	}

	internal void SetParent(ref BSPMSurface2 surfID, WorldBrushData brushData) {
		this.brushData.SetTarget(brushData);
		ParentSurfPointer = surfID.SurfNum;
	}

	public ref BSPMSurface2 GetParent(){
		if (!brushData.TryGetTarget(out WorldBrushData? brush))
			throw new NullReferenceException("The world brush data has gone out of GC scope, and the pointer to a BSPMSurface2 cannot be retrieved.");
		return ref brush.Surfaces2![ParentSurfPointer];
	}


	public Vector3 BoxMin;
	public Vector3 BoxMax;
	public int NumIndices;
	public int IndexOffset;
	public GroupMesh? Mesh;
	public int VertOffset;
	public float BumpSTexCoordOffset;
	public readonly List<ushort> Indices = [];
	public readonly List<DispRenderVert> Verts = [];
}
