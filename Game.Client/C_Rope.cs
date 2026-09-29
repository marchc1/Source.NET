using Game.Shared;

using Source;
using Source.Common;
using System.Net;

using System.Security.Cryptography.X509Certificates;
using Source.Common.MaterialSystem;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_RopeKeyframe>;

[NetworkName("CRopeKeyframe")]
public class C_RopeKeyframe : C_BaseEntity
{
	public static readonly RecvTable DT_RopeKeyframe = new([
		RecvPropEHandle(FIELD.OF(nameof(StartPoint))),
		RecvPropEHandle(FIELD.OF(nameof(EndPoint))),
		RecvPropInt(FIELD.OF(nameof(StartAttachment))),
		RecvPropInt(FIELD.OF(nameof(EndAttachment))),
		RecvPropInt(FIELD.OF(nameof(StartBone))),
		RecvPropInt(FIELD.OF(nameof(EndBone))),
		RecvPropVector(FIELD.OF(nameof(StartOffset))),
		RecvPropVector(FIELD.OF(nameof(EndOffset))),
		RecvPropInt(FIELD.OF(nameof(RenderColor))),
		// todo: RecvProxy_RecomputeSprings
		RecvPropInt(FIELD.OF(nameof(Slack))),
		// todo: RecvProxy_RecomputeSprings
		RecvPropInt(FIELD.OF(nameof(RopeLength))),
		RecvPropInt(FIELD.OF(nameof(LockedPoints))),
		RecvPropInt(FIELD.OF(nameof(RopeFlags))),
		RecvPropInt(FIELD.OF(nameof(Segments))),
		RecvPropBool(FIELD.OF(nameof(ConstrainBetweenEndpoints))),
		RecvPropInt(FIELD.OF(nameof(RopeMaterialModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Subdiv))),
		RecvPropFloat(FIELD.OF(nameof(TextureScale))),
		RecvPropFloat(FIELD.OF(nameof(Width))),
		RecvPropFloat(FIELD.OF(nameof(ScrollSpeed))),
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropEHandle(FIELD.OF(nameof(MoveParent))),
		RecvPropInt(FIELD.OF(nameof(ParentAttachment))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_RopeKeyframe);

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

