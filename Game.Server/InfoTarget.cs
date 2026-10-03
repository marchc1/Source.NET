using Game.Shared;

namespace Game.Server;

[LinkEntityToClass("info_target")]
public class InfoTarget : PointEntity
{
	public override void Spawn() {
		base.Spawn();

		if (HasSpawnFlags(0x01))
			SetEFlags(EFL.ForceCheckTransmit);
	}
}
