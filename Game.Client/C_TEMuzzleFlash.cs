using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Common.Mathematics;
namespace Game.Client;
using FIELD = FIELD<C_TEMuzzleFlash>;
[NetworkName("CTEMuzzleFlash")]
public class C_TEMuzzleFlash : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEMuzzleFlash = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Angles))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(Type))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEMuzzleFlash);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecAngles")]
	public Vector3 Angles;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_nType")]
	public int Type;
}

public static partial class TempEnts
{
	public static void TE_MuzzleFlash(IRecipientFilter filter, float delay, in Vector3 start, in QAngle angles, float scale, int type) {
		throw new NotImplementedException();
	}
}
