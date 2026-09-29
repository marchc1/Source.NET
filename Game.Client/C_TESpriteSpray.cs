using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESpriteSpray>;
[NetworkName("CTESpriteSpray")]
public class C_TESpriteSpray : C_BaseTempEntity
{
	public static readonly RecvTable DT_TESpriteSpray = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Noise))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(Count))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TESpriteSpray);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fNoise")]
	public float Noise;
	[NetworkName("m_nSpeed")]
	public int Speed;
	[NetworkName("m_nCount")]
	public int Count;
}

public static partial class TempEnts
{
	public static void TE_SpriteSpray(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, int modelIndex, int speed, float noise, int count) {
		throw new NotImplementedException();
	}
}
