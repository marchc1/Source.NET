using Source.Common.MaterialSystem;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

[InlineArray(Render.MAX_DECALCLIPVERT)] public struct InlineArrayMaxDecalClipVert<T> { public T item; }

public struct DecalVert
{
	public Vector3 Pos;
	public int DecalIndex;

	public Vector2 CtCoords;
	public Vector2 CLMCoords;
}

public partial class Render
{
	public const int MAX_DECALCLIPVERT = 48;
	public static InlineArrayMaxDecalClipVert<DecalVert> g_DecalClipVerts;

	public static Span<DecalVert> R_DoDecalSHClip(Span<DecalVert> inVerts, Span<DecalVert> outVerts, Decal decal, int startVerts, in Vector3 normal) {
		throw new NotImplementedException();
	}

	public static Span<DecalVert> R_DecalVertsClip(Span<DecalVert> outVerts, Decal decal, SurfaceHandle_t surfID, IMaterial material) {
		throw new NotImplementedException();
	}

	public static void R_DecalComputeBasis(in Vector3 surfaceNormal, in Vector3 sAxis, Span<Vector3> textureSpaceBasis) {
		throw new NotImplementedException();
	}

	public static void R_SetupDecalTextureSpaceBasis(Decal decal, ref Vector3 surfNormal, IMaterial material, Span<Vector3> textureSpaceBasis, Span<float> decalWorldScale) {
		throw new NotImplementedException();
	}
}
