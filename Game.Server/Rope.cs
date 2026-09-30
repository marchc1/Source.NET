using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<RopeKeyframe>;

[NetworkName("CRopeKeyframe")]
public partial class RopeKeyframe : BaseEntity
{
	public static readonly SendTable DT_RopeKeyframe = new([
		SendPropEHandle(RopeKeyframe.NetworkVarFields.StartPoint),
		SendPropEHandle(RopeKeyframe.NetworkVarFields.EndPoint),
		SendPropInt(NetworkVarFields.StartAttachment, 5),
		SendPropInt(NetworkVarFields.EndAttachment, 5),
		SendPropInt(NetworkVarFields.StartBone, 5),
		SendPropInt(NetworkVarFields.EndBone, 5),
		SendPropVector(NetworkVarFields.StartOffset, 0, PropFlags.Coord),
		SendPropVector(NetworkVarFields.EndOffset, 0, PropFlags.Coord),
		SendPropInt(NetworkVarFields.RenderColor, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Slack, 12),
		SendPropInt(NetworkVarFields.RopeLength, 15),
		SendPropInt(NetworkVarFields.LockedPoints, 4, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.RopeFlags, 9, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Segments, 4, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.ConstrainBetweenEndpoints),
		SendPropInt(NetworkVarFields.RopeMaterialModelIndex, 16, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Subdiv, 4, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.TextureScale, 10, 0, 0.1f, 10.0f),
		SendPropFloat(NetworkVarFields.Width, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScrollSpeed, 0, PropFlags.NoScale),
		SendPropVector(BaseEntity.NetworkVarFields.Origin, 0, PropFlags.Coord),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(BaseEntity.NetworkVarFields.ParentAttachment, 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RopeKeyframe);
	[NetworkName("m_hStartPoint")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> StartPoint { get; }
	[NetworkName("m_hEndPoint")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> EndPoint { get; }
	[NetworkName("m_iStartAttachment")]
	[NetworkVar] public partial int StartAttachment { get; set; }
	[NetworkName("m_iEndAttachment")]
	[NetworkVar] public partial int EndAttachment { get; set; }
	[NetworkName("m_iStartBone")]
	[NetworkVar] public partial int StartBone { get; set; }
	[NetworkName("m_iEndBone")]
	[NetworkVar] public partial int EndBone { get; set; }
	[NetworkName("m_vecStartOffset")]
	[NetworkVar] public partial Vector3 StartOffset { get; set; }
	[NetworkName("m_vecEndOffset")]
	[NetworkVar] public partial Vector3 EndOffset { get; set; }
	[NetworkName("m_clrRender")]
	[NetworkVar] public partial Color RenderColor { get; set; }
	[NetworkName("m_Slack")]
	[NetworkVar] public partial int Slack { get; set; }
	[NetworkName("m_RopeLength")]
	[NetworkVar] public partial int RopeLength { get; set; }
	[NetworkName("m_fLockedPoints")]
	[NetworkVar] public partial int LockedPoints { get; set; }
	[NetworkName("m_RopeFlags")]
	[NetworkVar] public partial int RopeFlags { get; set; }
	[NetworkName("m_nSegments")]
	[NetworkVar] public partial int Segments { get; set; }
	[NetworkName("m_bConstrainBetweenEndpoints")]
	[NetworkVar] public partial bool ConstrainBetweenEndpoints { get; set; }
	[NetworkName("m_iRopeMaterialModelIndex")]
	[NetworkVar] public partial int RopeMaterialModelIndex { get; set; }
	[NetworkName("m_Subdiv")]
	[NetworkVar] public partial int Subdiv { get; set; }
	[NetworkName("m_TextureScale")]
	[NetworkVar] public partial float TextureScale { get; set; }
	[NetworkName("m_Width")]
	[NetworkVar] public partial float Width { get; set; }
	[NetworkName("m_flScrollSpeed")]
	[NetworkVar] public partial float ScrollSpeed { get; set; }
}
