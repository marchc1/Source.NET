using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.Common;

public struct BrushVertex
{
	public Vector3 Pos;
	public Vector3 Normal;
	public Vector3 TangentS;
	public Vector3 TangentT;
	public Vector2 TexCoord;
	public Vector2 LightmapCoord;
}

public interface IBrushSurface
{
	void ComputeTextureCoordinate(in Vector3 worldPos, out Vector2 texCoord);
	void ComputeLightmapCoordinate(in Vector3 worldPos, out Vector2 lightmapCoord);
	int GetVertexCount();
	void GetVertexData(Span<BrushVertex> verts);
	IMaterial? GetMaterial();
}

public interface IBrushRenderer
{
	bool RenderBrushModelSurface(IClientEntity? baseEntity, IBrushSurface brushSurface);
}
