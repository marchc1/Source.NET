using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("info_player_start")]
[LinkEntityToClass("info_landmark")]
public class PointEntity : BaseEntity
{
	public static readonly new ServerClass ServerClass = new ServerClass("PointEntity", DT_BaseEntity);

	public override void Spawn() {
		SetSolid(Source.SolidType.None);
	}

	// todo
	// public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;
}
