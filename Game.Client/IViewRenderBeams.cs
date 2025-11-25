using Game.Shared;

using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Client;



public class BeamTrail
{
	public BeamTrail? Next;
	public TimeUnit_t Die;
	public Vector3 Origin;
	public Vector3 Velocity;
}

public ref struct BeamInfo
{
	public TempEntType Type;

	// Entities
	public C_BaseEntity? StartEnt;
	public int StartAttachment;
	public C_BaseEntity? EndEnt;
	public int EndAttachment;

	// Points
	public Vector3 Start;
	public Vector3 End;

	public int ModelIndex;
	public ReadOnlySpan<char> ModelName;

	public int HaloIndex;
	public ReadOnlySpan<char> HaloName;
	public float HaloScale;

	public TimeUnit_t Life;
	public float Width;
	public float EndWidth;
	public float FadeLength;
	public float Amplitude;

	public float Brightness;
	public TimeUnit_t Speed;

	public int StartFrame;
	public TimeUnit_t FrameRate;

	public float Red;
	public float Green;
	public float Blue;

	public bool Renderable;

	public int Segments;

	public BeamFlags Flags;

	// Rings
	public Vector3 Center;
	public float StartRadius;
	public float EndRadius;

	public BeamInfo() {
		Type = TempEntType.BeamPoints;
		Segments = -1;
		ModelName = null;
		HaloName = null;
		ModelIndex = -1;
		HaloIndex = -1;
		Renderable = true;
		Flags = 0;
	}
}

public interface IViewRenderBeams
{
	void InitBeams();
	void ShutdownBeams();
	void ClearBeams();

	// Updates the state of the temp ent beams
	void UpdateTempEntBeams();

	void DrawBeam(C_Beam beam, ITraceFilter? entityBeamTraceFilter = null);
	void DrawBeam(Beam beam);

	void KillDeadBeams(SharedBaseEntity? ent);

	// New interfaces!
	Beam? CreateBeamEnts(ref BeamInfo beamInfo );
	Beam? CreateBeamEntPoint(ref BeamInfo beamInfo );
	Beam? CreateBeamPoints(ref BeamInfo beamInfo );
	Beam? CreateBeamRing(ref BeamInfo beamInfo );
	Beam? CreateBeamRingPoint(ref BeamInfo beamInfo );
	Beam? CreateBeamCirclePoints(ref BeamInfo beamInfo );
	Beam? CreateBeamFollow(ref BeamInfo beamInfo );

	void FreeBeam(Beam pBeam);
	void UpdateBeamInfo(Beam pBeam, ref BeamInfo beamInfo);

	// These will go away!
	void CreateBeamEnts(int startEnt, int endEnt, int modelIndex, int haloIndex, float haloScale,
							float life, float width, float m_nEndWidth, float m_nFadeLength, float amplitude,
							float brightness, float speed, int startFrame,
							float framerate, float r, float g, float b, int type = -1);
	void CreateBeamEntPoint(int startEntity, in Vector3 start, int endEntity, in Vector3 end,
							int modelIndex, int haloIndex, float haloScale,
							float life, float width, float endWidth, float fadeLength, float amplitude,
							float brightness, float speed, int startFrame,
							float framerate, float r, float g, float b );
	void CreateBeamPoints(ref Vector3 start, ref Vector3 end, int modelIndex, int haloIndex, float haloScale,
							float life, float width, float endWidth, float fadeLength, float amplitude,
							float brightness, float speed, int startFrame,
							float framerate, float r, float g, float b);
	void CreateBeamRing(int startEnt, int endEnt, int modelIndex, int haloIndex, float haloScale,
							float life, float width, float endWidth, float fadeLength, float amplitude,
							float brightness, float speed, int startFrame,
							float framerate, float r, float g, float b, int flags = 0);
	void CreateBeamRingPoint( in Vector3 center, float startRadius, float endRadius, int modelIndex, int haloIndex, float haloScale,
							float life, float width, float m_nEndWidth, float fadeLength, float amplitude,
							float brightness, float speed, int startFrame,
							float framerate, float r, float g, float b, int flags = 0 );
	void CreateBeamCirclePoints(int type, ref Vector3 start, ref Vector3 end,
							int modelIndex, int haloIndex, float haloScale, float life, float width,
							float endWidth, float fadeLength, float amplitude, float brightness, float speed,
							int startFrame, float framerate, float r, float g, float b);
	void CreateBeamFollow(int startEnt, int modelIndex, int haloIndex, float haloScale,
							float life, float width, float endWidth, float fadeLength, float r, float g, float b,
							float brightness);
}
