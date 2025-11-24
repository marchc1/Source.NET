using Source;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;

public static class BeamDraw
{
	public static void DrawSprite(in Vector3 origin, float width, float height, Color color) {
		width *= 0.5f;
		height *= 0.5f;

		Vector3 fwd, right = new( 1, 0, 0 ), up = new( 0, 1, 0 );
		MathLib.VectorSubtract(CurrentViewOrigin(), origin, out fwd);
		float flDist = MathLib.VectorNormalize(ref fwd);
		if (flDist >= 1e-3) {
			MathLib.CrossProduct(in CurrentViewUp(), in fwd, out right);
			flDist = MathLib.VectorNormalize(ref right);
			if (flDist >= 1e-3) 
				MathLib.CrossProduct(in fwd, in right, out up);
			else {
				// In this case, fwd == g_vecVUp, it's right above or 
				// below us in screen space
				MathLib.CrossProduct(in fwd, in CurrentViewRight(), out up);
				MathLib.VectorNormalize(ref up);
				MathLib.CrossProduct(in up, in fwd, out right);
			}
		}

		MeshBuilder meshBuilder = new();
		Vector3 point = default;
		using MatRenderContextPtr renderContext = new(materials);
		IMesh pMesh = renderContext.GetDynamicMesh();

		meshBuilder.Begin(pMesh, MaterialPrimitiveType.Quads, 1);

		meshBuilder.Color4ubv(color);
		meshBuilder.TexCoord2f(0, 0, 1);
		MathLib.VectorMA(origin, -height, up, ref point);
		MathLib.VectorMA(point, -width, right, ref point);
		meshBuilder.Position3fv(point);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ubv(color);
		meshBuilder.TexCoord2f(0, 0, 0);
		MathLib.VectorMA(origin, height, up, ref point);
		MathLib.VectorMA(point, -width, right, ref point);
		meshBuilder.Position3fv(point);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ubv(color);
		meshBuilder.TexCoord2f(0, 1, 0);
		MathLib.VectorMA(origin, height, up, ref point);
		MathLib.VectorMA(point, width, right, ref point);
		meshBuilder.Position3fv(point);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ubv(color);
		meshBuilder.TexCoord2f(0, 1, 1);
		MathLib.VectorMA(origin, -height, up, ref point);
		MathLib.VectorMA(point, width, right, ref point);
		meshBuilder.Position3fv(point);
		meshBuilder.AdvanceVertex();

		meshBuilder.End();
		pMesh.Draw();
	}
}
