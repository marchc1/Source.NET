using Game.Shared;

using Source.Common;

namespace Game.Server;

public class PointEntity : BaseEntity
{
	public static readonly new ServerClass ServerClass = new ServerClass("PointEntity", DT_BaseEntity);
}

[LinkEntityToClass("info_player_start")]
class PlayerInfoStart : PointEntity { }