using Game.Shared;

namespace Game.Server;

[LinkEntityToClass("light")]
public class Light : PointEntity
{

}

[LinkEntityToClass("light_environment")]
public class EnvLight : Light
{

}
