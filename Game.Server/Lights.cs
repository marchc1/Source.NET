using Game.Shared;

namespace Game.Server;

[LinkEntityToClass("light")]
public class Light : PointEntity // TODO, server only
{

}

[LinkEntityToClass("light_environment")]
public class EnvLight : Light
{

}
