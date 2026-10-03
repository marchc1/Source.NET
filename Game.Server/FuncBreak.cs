using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("func_breakable")]
public class Breakable : BaseEntity, IBreakableWithPropData;
