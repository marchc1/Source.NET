using Game.Shared;

using Source;
using Source.Common;
using System.Net;

using System.Security.Cryptography.X509Certificates;
using Source.Common.MaterialSystem;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_RopeKeyframe>;

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
		RecvPropInt(FIELD.OF(nameof(Slack))),
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
	public static readonly new ClientClass ClientClass = new ClientClass("RopeKeyframe", DT_RopeKeyframe).WithManualClassID(StaticClassIndices.CRopeKeyframe);

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

