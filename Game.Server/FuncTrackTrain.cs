using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("func_tracktrain")]
[NetworkName("CFuncTrackTrain")]
public class FuncTrackTrain : Breakable
{
	public static readonly SendTable DT_FuncTrackTrain = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncTrackTrain);
}
