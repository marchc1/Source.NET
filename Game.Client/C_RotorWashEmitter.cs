using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_RotorWashEmitter>;
public class C_RotorWashEmitter : C_BaseEntity
{
	public static readonly RecvTable DT_RotorWashEmitter = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Altitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("RotorWashEmitter", DT_RotorWashEmitter).WithManualClassID(StaticClassIndices.CRotorWashEmitter);

	public float Altitude;
}
