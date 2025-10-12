using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<RopeKeyframe>;

public class RopeKeyframe : BaseEntity
{
	public static readonly SendTable DT_RopeKeyframe = new([
		SendPropEHandle(FIELD.OF(nameof(StartPoint))),
		SendPropEHandle(FIELD.OF(nameof(EndPoint))),
		SendPropInt(FIELD.OF(nameof(StartAttachment)), 5),
		SendPropInt(FIELD.OF(nameof(EndAttachment)), 5),
		SendPropInt(FIELD.OF(nameof(StartBone)), 5),
		SendPropInt(FIELD.OF(nameof(EndBone)), 5),
		SendPropVector(FIELD.OF(nameof(StartOffset)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(EndOffset)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(RenderColor)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Slack)), 12),
		SendPropInt(FIELD.OF(nameof(RopeLength)), 15),
		SendPropInt(FIELD.OF(nameof(LockedPoints)), 4, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(RopeFlags)), 9, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Segments)), 4, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(ConstrainBetweenEndpoints))),
		SendPropInt(FIELD.OF(nameof(RopeMaterialModelIndex)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Subdiv)), 4, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(TextureScale)), 10),
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScrollSpeed)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("RopeKeyframe", DT_RopeKeyframe).WithManualClassID(StaticClassIndices.CRopeKeyframe);
	public readonly EHANDLE StartPoint = new();
	public readonly EHANDLE EndPoint = new();
	public int StartAttachment;
	public int EndAttachment;
	public int StartBone;
	public int EndBone;
	public Vector3 StartOffset;
	public Vector3 EndOffset;
	public Color RenderColor;
	public int Slack;
	public int RopeLength;
	public int LockedPoints;
	public int RopeFlags;
	public int Segments;
	public bool ConstrainBetweenEndpoints;
	public int RopeMaterialModelIndex;
	public int Subdiv;
	public float TextureScale;
	public float Width;
	public float ScrollSpeed;
}
