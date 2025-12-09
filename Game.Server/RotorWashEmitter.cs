using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RotorWashEmitter>;
public class RotorWashEmitter : BaseEntity
{
	public static readonly SendTable DT_RotorWashEmitter = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Altitude)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("RotorWashEmitter", DT_RotorWashEmitter).WithManualClassID(StaticClassIndices.CRotorWashEmitter);

	public float Altitude;
}
