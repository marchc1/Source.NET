using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<RopeKeyframe>;

[LinkEntityToClass("keyframe_rope")]
[LinkEntityToClass("move_rope")]
[NetworkName("CRopeKeyframe")]
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
		SendPropFloat(FIELD.OF(nameof(TextureScale)), 10, 0, 0.1f, 10.0f),
		SendPropFloat(FIELD.OF(nameof(Width)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScrollSpeed)), 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.Coord),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RopeKeyframe);
	[NetworkName("m_hStartPoint")]
	public EHANDLE StartPoint = new();
	[NetworkName("m_hEndPoint")]
	public EHANDLE EndPoint = new();
	[NetworkName("m_iStartAttachment")]
	public int StartAttachment;
	[NetworkName("m_iEndAttachment")]
	public int EndAttachment;
	[NetworkName("m_iStartBone")]
	public int StartBone;
	[NetworkName("m_iEndBone")]
	public int EndBone;
	[NetworkName("m_vecStartOffset")]
	public Vector3 StartOffset;
	[NetworkName("m_vecEndOffset")]
	public Vector3 EndOffset;
	[NetworkName("m_clrRender")]
	public Color RenderColor;
	[NetworkName("m_Slack")]
	public int Slack;
	[NetworkName("m_RopeLength")]
	public int RopeLength;
	[NetworkName("m_fLockedPoints")]
	public int LockedPoints;
	[NetworkName("m_RopeFlags")]
	public int RopeFlags;
	[NetworkName("m_nSegments")]
	public int Segments;
	[NetworkName("m_bConstrainBetweenEndpoints")]
	public bool ConstrainBetweenEndpoints;
	[NetworkName("m_iRopeMaterialModelIndex")]
	public int RopeMaterialModelIndex;
	[NetworkName("m_Subdiv")]
	public int Subdiv;
	[NetworkName("m_TextureScale")]
	public float TextureScale;
	[NetworkName("m_Width")]
	public float Width;
	[NetworkName("m_flScrollSpeed")]
	public float ScrollSpeed;
}
