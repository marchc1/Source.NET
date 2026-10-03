using Game.Shared;

namespace Game.Server;

[LinkEntityToClass("light")]
[LinkEntityToClass("light_directional")]
[LinkEntityToClass("light_glspot")]
[LinkEntityToClass("light_spot")]
public class Light : PointEntity // TODO, server only
{

}

[LinkEntityToClass("light_environment")]
public class EnvLight : Light
{

}
