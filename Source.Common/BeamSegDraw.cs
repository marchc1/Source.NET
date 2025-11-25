using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common;

public struct BeamSeg
{
	public Vector3 Pos;
	public Vector3 Color;
	public float TexCoord;
	public float Width;
	public float Alpha;
}

public ref struct BeamSegDraw
{
	ref MeshBuilder pMeshBuilder;
	int MeshVertCount;
	MeshBuilder Mesh;
	BeamSeg Seg;
	int TotalSegs;
	int SegsDrawn;
	Vector3 NormalLast;
	IMatRenderContext RenderContext;

	public void Start(IMatRenderContext renderContext, int segs, IMaterial? material) {
		RenderContext = renderContext;
		SegsDrawn = 0;
		TotalSegs = segs;
		MeshVertCount = 0;

		IMesh mesh = renderContext.GetDynamicMesh(true, null, null, material);
		Mesh.Begin(mesh, MaterialPrimitiveType.TriangleStrip, (segs - 1) * 2);
	}

	public void NextSeg(ref BeamSeg seg) {
		RenderContext.GetWorldSpaceCameraPosition(out Vector3 vecCameraPos);

		if (SegsDrawn > 0) {
			// Get a vector that is perpendicular to us and perpendicular to the beam.
			// This is used to fatten the beam.
			Vector3 normal, aveNormal;
			ComputeNormal(in vecCameraPos, in Seg.Pos, in seg.Pos, out normal);

			if (SegsDrawn > 1) {
				// Average this with the previous normal
				MathLib.VectorAdd(normal, NormalLast, out aveNormal);
				aveNormal *= 0.5f;
				MathLib.VectorNormalize(ref aveNormal);
			}
			else
				aveNormal = normal;

			NormalLast = normal;
			SpecifySeg(vecCameraPos, aveNormal);
		}

		Seg = seg;
		++SegsDrawn;
		if (SegsDrawn == TotalSegs)
			SpecifySeg(vecCameraPos, NormalLast);
	}

	private void ComputeNormal(in Vector3 vecCameraPos, in Vector3 startPos, in Vector3 nextPos, out Vector3 normal) {
		Vector3 vTangentY;
		MathLib.VectorSubtract(startPos, nextPos, out vTangentY);

		Vector3 vDirToBeam;
		MathLib.VectorSubtract(startPos, vecCameraPos, out vDirToBeam);

		MathLib.CrossProduct(vTangentY, vDirToBeam, out normal);
		MathLib.VectorNormalize(ref normal);
	}

	public void SpecifySeg(in Vector3 vecCameraPos, in Vector3 vNormal) {
		Vector3 vDirToBeam, vTangentY;
		MathLib.VectorSubtract(Seg.Pos, vecCameraPos, out vDirToBeam);
		MathLib.CrossProduct(vDirToBeam, vNormal, out vTangentY);
		MathLib.VectorNormalize(ref vTangentY);

		// Build the endpoints.
		Vector3 vPoint1 = default, vPoint2 = default;
		MathLib.VectorMA(Seg.Pos, Seg.Width * 0.5f, vNormal, ref vPoint1);
		MathLib.VectorMA(Seg.Pos, -Seg.Width * 0.5f, vNormal, ref vPoint2);


		// Specify the points.
		Mesh.Position3fv(vPoint1);
		Mesh.Color4f(Seg.Color, Seg.Alpha);
		Mesh.TexCoord2f(0, 0, Seg.TexCoord);
		Mesh.TexCoord2f(1, 0, Seg.TexCoord);
		Mesh.TangentS3fv(vNormal);
		Mesh.TangentT3fv(vTangentY);
		Mesh.AdvanceVertex();

		Mesh.Position3fv(vPoint2);
		Mesh.Color4f(Seg.Color, Seg.Alpha);
		Mesh.TexCoord2f(0, 1, Seg.TexCoord);
		Mesh.TexCoord2f(1, 1, Seg.TexCoord);
		Mesh.TangentS3fv(vNormal);
		Mesh.TangentT3fv(vTangentY);
		Mesh.AdvanceVertex();
	}

	public void End() {
		Mesh.End(false, true);
	}
}
