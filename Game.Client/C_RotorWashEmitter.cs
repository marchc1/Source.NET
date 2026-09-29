using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_RotorWashEmitter>;
[NetworkName("CRotorWashEmitter")]
public class C_RotorWashEmitter : C_BaseEntity
{
	public static readonly RecvTable DT_RotorWashEmitter = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Altitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_RotorWashEmitter);

	[NetworkName("m_flAltitude")]
	public float Altitude;
}
