using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

[InlineArray(Render.MAX_DECALCLIPVERT)] public struct InlineArrayMaxDecalClipVert<T> { public T item; }

public interface IDecalClipper
{
	static abstract bool Inside(ref DecalVert vert);
	static abstract float Clip(ref DecalVert one, ref DecalVert two);
}

public struct CPlane_Top : IDecalClipper
{
	public static bool Inside(ref DecalVert vert) => vert.CtCoords.Y < 1;
	public static float Clip(ref DecalVert one, ref DecalVert two) => (1 - one.CtCoords.Y) / (two.CtCoords.Y - one.CtCoords.Y);
}

public struct CPlane_Left : IDecalClipper
{
	public static bool Inside(ref DecalVert vert) => vert.CtCoords.X > 0;
	public static float Clip(ref DecalVert one, ref DecalVert two) => one.CtCoords.X / (one.CtCoords.X - two.CtCoords.X);
}

public struct CPlane_Right : IDecalClipper
{
	public static bool Inside(ref DecalVert vert) => vert.CtCoords.X < 1;
	public static float Clip(ref DecalVert one, ref DecalVert two) => (1 - one.CtCoords.X) / (two.CtCoords.X - one.CtCoords.X);
}

public struct CPlane_Bottom : IDecalClipper
{
	public static bool Inside(ref DecalVert vert) => vert.CtCoords.Y > 0;
	public static float Clip(ref DecalVert one, ref DecalVert two) => one.CtCoords.Y / (one.CtCoords.Y - two.CtCoords.Y);
}

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
	public const int MAX_PLAYERSPRAY_SIZE = 64;
	public const float SIN_45_DEGREES = 0.70710678118654752440084436210485f;

	public static InlineArrayMaxDecalClipVert<DecalVert> g_DecalClipVerts;
	static InlineArrayMaxDecalClipVert<DecalVert> g_DecalClipVerts2;

	static void Intersect<Clipper>(ref DecalVert one, ref DecalVert two, ref DecalVert outVert) where Clipper : IDecalClipper {
		float t = Clipper.Clip(ref one, ref two);

		MathLib.VectorLerp(one.Pos, two.Pos, t, out outVert.Pos);
		MathLib.Vector2DLerp(one.CLMCoords, two.CLMCoords, t, out outVert.CLMCoords);
		MathLib.Vector2DLerp(one.CtCoords, two.CtCoords, t, out outVert.CtCoords);
	}

	static int SHClip<Clipper>(Span<DecalVert> decalClipVerts, int vertCount, Span<DecalVert> outVerts) where Clipper : IDecalClipper {
		int j, outCount;

		Assert(vertCount <= MAX_DECALCLIPVERT);

		outCount = 0;

		if (vertCount == 0)
			return outCount;

		ref DecalVert s = ref decalClipVerts[vertCount - 1];
		for (j = 0; j < vertCount; j++) {
			ref DecalVert p = ref decalClipVerts[j];
			if (Clipper.Inside(ref p)) {
				if (Clipper.Inside(ref s)) {
					outVerts[outCount] = p;
					outCount++;
				}
				else {
					Intersect<Clipper>(ref s, ref p, ref outVerts[outCount]);
					outCount++;

					outVerts[outCount] = p;
					outCount++;
				}
			}
			else {
				if (Clipper.Inside(ref s)) {
					Intersect<Clipper>(ref p, ref s, ref outVerts[outCount]);
					outCount++;
				}
			}
			s = ref p;
		}

		return outCount;
	}

	public const float DECAL_CLIP_EPSILON = 0.01f;

	public static Span<DecalVert> DoDecalSHClip(Span<DecalVert> inVerts, Span<DecalVert> outVerts, Decal decal, int startVerts, in Vector3 normal) {
		if (outVerts.IsEmpty)
			outVerts = g_DecalClipVerts;

		int outCount = SHClip<CPlane_Top>(inVerts, startVerts, g_DecalClipVerts2);
		outCount = SHClip<CPlane_Left>(g_DecalClipVerts2, outCount, g_DecalClipVerts);
		outCount = SHClip<CPlane_Right>(g_DecalClipVerts, outCount, g_DecalClipVerts2);
		outCount = SHClip<CPlane_Bottom>(g_DecalClipVerts2, outCount, outVerts);

		decal.ClippedVertCount = (ushort)outCount;

		if (outCount == 0)
			return default;

		for (int i = 0; i < outCount; ++i) {
			MathLib.VectorMA(outVerts[i].Pos, OVERLAY_AVOID_FLICKER_NORMAL_OFFSET, normal, out outVerts[i].Pos);
		}
		if (decal.Material!.InMaterialPage()) {
			Span<float> offset = stackalloc float[2];
			Span<float> scale = stackalloc float[2];
			decal.Material.GetMaterialOffset(offset);
			decal.Material.GetMaterialScale(scale);
			for (int i = 0; i < outCount; ++i) {
				outVerts[i].CtCoords.X = offset[0] + outVerts[i].CtCoords.X * scale[0];
				outVerts[i].CtCoords.Y = offset[1] + outVerts[i].CtCoords.Y * scale[1];
			}
		}

		return outVerts;
	}

	public static void SetupDecalClip(Span<DecalVert> outVerts, Decal decal, ref Vector3 surfNormal, IMaterial material, Span<Vector3> textureSpaceBasis, Span<float> decalWorldScale) {
		SetupDecalTextureSpaceBasis(decal, ref surfNormal, material, textureSpaceBasis, decalWorldScale);

		decal.Dx = Vector3.Dot(decal.Position, textureSpaceBasis[0]);
		decal.Dy = Vector3.Dot(decal.Position, textureSpaceBasis[1]);
	}

	public static Span<DecalVert> DecalVertsClip(Span<DecalVert> outVerts, Decal decal, SurfaceHandle_t surfID, IMaterial material) {
		Span<float> decalWorldScale = stackalloc float[2];
		Span<Vector3> textureSpaceBasis = stackalloc Vector3[3];

		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);

		SetupDecalClip(outVerts, decal, ref ModelLoader.MSurf_Plane(ref surf).Normal, material, textureSpaceBasis, decalWorldScale);
		SetupDecalVertsForMSurface(decal, surfID, textureSpaceBasis, g_DecalClipVerts);

		return DoDecalSHClip(g_DecalClipVerts, outVerts, decal, ModelLoader.MSurf_VertCount(ref surf), ModelLoader.MSurf_Plane(ref surf).Normal);
	}

	public static void SetupDecalVertsForMSurface(Decal decal, SurfaceHandle_t surfID, Span<Vector3> textureSpaceBasis, Span<DecalVert> verts) {
		ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(surfID);
		Span<ushort> indices = SourceDllMain.host_state.WorldBrush!.VertIndices.AsSpan(ModelLoader.MSurf_FirstVertIndex(ref surf));
		int count = ModelLoader.MSurf_VertCount(ref surf);
		float uOffset = 0.5f - decal.Dx;
		float vOffset = 0.5f - decal.Dy;

		for (int j = 0; j < count; j++) {
			int vertIndex = indices[j];
			verts[j].Pos = SourceDllMain.host_state.WorldBrush.Vertexes![vertIndex].Position;
			verts[j].CtCoords.X = Vector3.Dot(verts[j].Pos, textureSpaceBasis[0]) + uOffset;
			verts[j].CtCoords.Y = Vector3.Dot(verts[j].Pos, textureSpaceBasis[1]) + vOffset;
			verts[j].CLMCoords = default;
		}
	}

	public static void DecalComputeBasis(in Vector3 surfaceNormal, Vector3? sAxis, Span<Vector3> textureSpaceBasis) {
		textureSpaceBasis[2] = surfaceNormal;

		if (sAxis != null) {
			MathLib.CrossProduct(sAxis.Value, textureSpaceBasis[2], out textureSpaceBasis[1]);

			if (Vector3.Dot(textureSpaceBasis[1], textureSpaceBasis[1]) > 1e-6) {
				MathLib.CrossProduct(textureSpaceBasis[2], textureSpaceBasis[1], out textureSpaceBasis[0]);

				MathLib.VectorNormalizeFast(ref textureSpaceBasis[0]);
				MathLib.VectorNormalizeFast(ref textureSpaceBasis[1]);
				return;
			}
		}

		if (MathF.Abs(surfaceNormal[2]) > SIN_45_DEGREES) {
			textureSpaceBasis[0][0] = 1.0f;
			textureSpaceBasis[0][1] = 0.0f;
			textureSpaceBasis[0][2] = 0.0f;

			MathLib.CrossProduct(textureSpaceBasis[0], textureSpaceBasis[2], out textureSpaceBasis[1]);
			MathLib.CrossProduct(textureSpaceBasis[2], textureSpaceBasis[1], out textureSpaceBasis[0]);
		}
		else {
			textureSpaceBasis[1][0] = 0.0f;
			textureSpaceBasis[1][1] = 0.0f;
			textureSpaceBasis[1][2] = -1.0f;

			MathLib.CrossProduct(textureSpaceBasis[2], textureSpaceBasis[1], out textureSpaceBasis[0]);
			MathLib.CrossProduct(textureSpaceBasis[0], textureSpaceBasis[2], out textureSpaceBasis[1]);
		}

		MathLib.VectorNormalizeFast(ref textureSpaceBasis[0]);
		MathLib.VectorNormalizeFast(ref textureSpaceBasis[1]);
	}

	public static void SetupDecalTextureSpaceBasis(Decal decal, ref Vector3 surfNormal, IMaterial material, Span<Vector3> textureSpaceBasis, Span<float> decalWorldScale) {
		DecalComputeBasis(surfNormal, (decal.Flags & FDecal.UseSAxis) != 0 ? decal.SAxis : null, textureSpaceBasis);

		if ((decal.Flags & FDecal.PlayerSpray) != 0) {
			int widthScale = (int)material.GetMappingWidth() / MAX_PLAYERSPRAY_SIZE;
			int heightScale = (int)material.GetMappingHeight() / MAX_PLAYERSPRAY_SIZE;
			float scale = Math.Max(widthScale, heightScale);

			decalWorldScale[0] = decal.Scale / material.GetMappingWidth();
			decalWorldScale[1] = decal.Scale / material.GetMappingHeight();

			if (scale > 1.0f) {
				decalWorldScale[0] *= scale;
				decalWorldScale[1] *= scale;
			}
		}
		else {
			decalWorldScale[0] = decal.Scale / material.GetMappingWidth();
			decalWorldScale[1] = decal.Scale / material.GetMappingHeight();
		}

		MathLib.VectorScale(textureSpaceBasis[0], decalWorldScale[0], out textureSpaceBasis[0]);
		MathLib.VectorScale(textureSpaceBasis[1], decalWorldScale[1], out textureSpaceBasis[1]);
	}
}
