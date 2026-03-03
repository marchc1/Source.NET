using Game.Shared;

namespace Game.Server;

public class PointEntity : BaseEntity
{

}

[LinkEntityToClass("info_player_start")]
class PlayerInfoStart : PointEntity { }