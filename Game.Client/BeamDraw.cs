using Game.Shared;

using Source;
using Source.Common;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;


namespace Game.Client;

public class Beam : DefaultClientRenderable {
	public const int NOISE_DIVISIONS = 128;
	public Beam() {

	}

	// Bounding box...
	public Vector3 Mins;
	public Vector3 Maxs;
	public float HaloProxySize;

	public Beam? Next;

	// Type of beam
	public TempEntType Type;
	public BeamFlags Flags;

	// Control points for the beam
	public int NumAttachments;
	public InlineArrayNewMaxBeamEnts<Vector3> Attachment;
	public Vector3 Delta;

	// 0 .. 1 over lifetime of beam
	public TimeUnit_t T;
	public TimeUnit_t Freq;

	// Time when beam should die
	public TimeUnit_t Die;
	public float Width;
	public float EndWidth;
	public float FadeLength;
	public float Amplitude;
	public TimeUnit_t Life;

	// Color
	public float R, G, B;
	public float Brightness;

	// Speed
	public TimeUnit_t Speed;

	// Animation
	public TimeUnit_t FrameRate;
	public TimeUnit_t Frame;
	public int Segments;

	// Attachment entities for the beam
	public InlineArrayNewMaxBeamEnts<EHANDLE> Entity = new();
	public InlineArrayNewMaxBeamEnts<int> AttachmentIndex;

	// Model info
	public int ModelIndex;
	public int HaloIndex;

	public float HaloScale;
	public int FrameCount;

	public float StartRadius;
	public float EndRadius;

	public bool CalculatedNoise;
	public float HDRColorScale;

	public BeamTrail? Trail;
	public readonly float[] Noise = new float[NOISE_DIVISIONS + 1];
	public override ref readonly QAngle GetRenderAngles() {
		throw new NotImplementedException();
	}

	public override void GetRenderBounds(out Vector3 mins, out Vector3 maxs) {
		throw new NotImplementedException();
	}

	public override ref readonly Vector3 GetRenderOrigin() {
		throw new NotImplementedException();
	}

	public override bool IsTransparent() {
		throw new NotImplementedException();
	}

	public override bool ShouldDraw() {
		throw new NotImplementedException();
	}

	public override int DrawModel(StudioFlags flags) {
		return base.DrawModel(flags);
	}

	public void Dispose() {
	
	}

	public void Reset() {
		Mins.Init(0, 0, 0);
		Maxs.Init(0, 0, 0);
		Type = 0;
		Flags = 0;
		Trail = null;
		m_RenderHandle = INVALID_CLIENT_RENDER_HANDLE;
		CalculatedNoise = false;
		HDRColorScale = 1.0f;
	}
}

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
