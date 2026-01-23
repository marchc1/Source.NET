#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;
using Source.Common.Networking;

using System.Numerics;
namespace Game.Shared;
#if CLIENT_DLL
using FIELD = Source.FIELD<C_Beam>;
#else
using FIELD = Source.FIELD<Beam>;
#endif

public enum BeamTypes
{
	Points,
	EntPoint,
	Ents,
	Hose,
	Spline,
	Laser,
	NumTypes
}


public class
#if CLIENT_DLL
	C_Beam
#else
	Beam
#endif
	: SharedBaseEntity
{
	public const int ATTACHMENT_INDEX_BITS = 5;
	public const float MAX_BEAM_WIDTH = 102.3f;
	public const float MAX_BEAM_SCROLLSPEED = 100f;
	public const float MAX_BEAM_NOISEAMPLITUDE = 64;

	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BeamPredictableId = new(nameof(DT_BeamPredictableId), [
#if CLIENT_DLL
		RecvPropPredictableId(FIELD.OF(nameof(PredictableID))),
		RecvPropBool(FIELD.OF(nameof(b_IsPlayerSimulated)))
#else
		SendPropPredictableId(FIELD.OF(nameof(PredictableId))),
		SendPropBool(FIELD.OF(nameof(b_IsPlayerSimulated)))
#endif
		]);
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_Beam = new([
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(BeamType))),
			RecvPropInt(FIELD.OF(nameof(BeamFlags))),
			RecvPropInt(FIELD.OF(nameof(NumBeamEnts))),
			RecvPropArray3(
				FIELD.OF_ARRAY(nameof(AttachEntity)),
				RecvPropEHandle(FIELD.OF_ARRAYINDEX(nameof(AttachEntity), 0))
			),
			RecvPropArray3
			(
				FIELD.OF_ARRAY(nameof(AttachIndex)),
				RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(AttachIndex), 0))
			),
			RecvPropInt(FIELD.OF(nameof(HaloIndex))),
			RecvPropFloat(FIELD.OF(nameof(HaloScale))),
			RecvPropFloat(FIELD.OF(nameof(Width))),
			RecvPropFloat(FIELD.OF(nameof(EndWidth))),
			RecvPropFloat(FIELD.OF(nameof(FadeLength))),
			RecvPropFloat(FIELD.OF(nameof(Amplitude))),
			RecvPropFloat(FIELD.OF(nameof(StartFrame))),
			RecvPropFloat(FIELD.OF(nameof(Speed)), 0, RecvProxy_Beam_ScrollSpeed),
			RecvPropInt(FIELD.OF(nameof(RenderFX))),
			RecvPropInt(FIELD.OF(nameof(RenderMode))),
			RecvPropFloat(FIELD.OF(nameof(FrameRate))),
			RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
			RecvPropFloat(FIELD.OF(nameof(Frame))),
			RecvPropInt(FIELD.OF(nameof(RenderColor))),
			RecvPropVector(FIELD.OF(nameof(EndPos))),

			RecvPropInt(FIELD.OF(nameof(ModelIndex))),
			RecvPropVector(FIELD.OF(nameof(Origin))),
			RecvPropInt(FIELD.OF(nameof(MoveParent)), 0, RecvProxy_IntToMoveParent),
			RecvPropInt(FIELD.OF(nameof(MinDXLevel))),

			RecvPropDataTable( "beampredictable_id", DT_BeamPredictableId)
#else
			SendPropInt(FIELD.OF(nameof(BeamType)), (int)Math.Log2((int)BeamTypes.NumTypes)+1, PropFlags.Unsigned),
			SendPropInt(FIELD.OF(nameof(BeamFlags)), (int)Shared.BeamFlags.NumFlags, PropFlags.Unsigned),
			SendPropInt(FIELD.OF(nameof(NumBeamEnts)), 5, PropFlags.Unsigned),
			SendPropArray3(
				FIELD.OF_ARRAY(nameof(AttachEntity)),
				SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(AttachEntity), 0))
			),
			SendPropArray3
			(
				FIELD.OF_ARRAY(nameof(AttachIndex)),
				SendPropInt(FIELD.OF_ARRAYINDEX(nameof(AttachIndex), 0), ATTACHMENT_INDEX_BITS, PropFlags.Unsigned)
			),
			SendPropInt(FIELD.OF(nameof(HaloIndex)), 16, PropFlags.Unsigned),
			SendPropFloat(FIELD.OF(nameof(HaloScale)),0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(Width)), 10, PropFlags.RoundUp, 0.0f, MAX_BEAM_WIDTH),
			SendPropFloat(FIELD.OF(nameof(EndWidth)), 10, PropFlags.RoundUp, 0.0f, MAX_BEAM_WIDTH),
			SendPropFloat(FIELD.OF(nameof(FadeLength)),0, PropFlags.NoScale),
			SendPropFloat(FIELD.OF(nameof(Amplitude)), 8, PropFlags.RoundDown, 0.0f, MAX_BEAM_NOISEAMPLITUDE),
			SendPropFloat(FIELD.OF(nameof(StartFrame)), 8, PropFlags.RoundDown, 0.0f,   256.0f),
			SendPropFloat(FIELD.OF(nameof(Speed)), 8, PropFlags.NoScale, 0.0f, MAX_BEAM_SCROLLSPEED),
			SendPropInt(FIELD.OF(nameof(RenderFX)), 8, PropFlags.Unsigned),
			SendPropInt(FIELD.OF(nameof(RenderMode)), 8, PropFlags.Unsigned),
			SendPropFloat(FIELD.OF(nameof(FrameRate)), 10, PropFlags.RoundUp, -25.0f, 25.0f),
			SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale, 0.0f, 0.0f),
			SendPropFloat(FIELD.OF(nameof(Frame)), 20, PropFlags.RoundDown | PropFlags.ChangesOften, 0.0f, 256.0f),
			SendPropInt(FIELD.OF(nameof(RenderColor)), 32, PropFlags.Unsigned | PropFlags.ChangesOften),
			SendPropVector(FIELD.OF(nameof(EndPos)), -1, PropFlags.Coord),

			SendPropModelIndex(FIELD.OF(nameof(ModelIndex))),
			SendPropVector(FIELD.OF(nameof(Origin)), 19, PropFlags.ChangesOften, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
			SendPropEHandle(FIELD.OF(nameof(MoveParent))),
			SendPropInt(FIELD.OF(nameof(MinDXLevel)), 8, PropFlags.Unsigned),

			SendPropDataTable( "beampredictable_id", DT_BeamPredictableId, SendProxy_SendPredictableId)
#endif
		]);

#if CLIENT_DLL
	private static void RecvProxy_Beam_ScrollSpeed(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		C_Beam beam = (C_Beam)instance;
		float val = data.Value.Float * 0.1f;
		beam.Speed = val;
	}

	public static readonly new ClientClass ClientClass = new ClientClass("Beam", null, null, DT_Beam).WithManualClassID(StaticClassIndices.CBeam);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("Beam", DT_Beam).WithManualClassID(StaticClassIndices.CBeam);
#endif
	public InlineArrayNewMaxBeamEnts<EHANDLE> AttachEntity = new();
	public InlineArrayNewMaxBeamEnts<int> AttachIndex = new();
	public int BeamType;
	public int BeamFlags;
	public int NumBeamEnts;
	public int MinDXLevel;
	public int HaloIndex;
	public float HaloScale;
	public float Width;
	public float EndWidth;
	public float FadeLength;
	public float Amplitude;
	public float StartFrame;
	public new float Speed;
	public float FrameRate;
	public float HDRColorScale;
	public float Frame;
	public Color RenderColor;
	public Vector3 EndPos;
}
#endif
