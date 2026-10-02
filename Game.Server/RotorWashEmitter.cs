using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RotorWashEmitter>;
[LinkEntityToClass("env_rotorwash_emitter")]
[NetworkName("CRotorWashEmitter")]
public class RotorWashEmitter : BaseEntity
{
	public static readonly SendTable DT_RotorWashEmitter = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Altitude)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RotorWashEmitter);

	[NetworkName("m_flAltitude")]
	public float Altitude;
}
