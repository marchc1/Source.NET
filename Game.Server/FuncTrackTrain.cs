using Game.Shared;

using Source.Common;

namespace Game.Server;

public class FuncTrackTrain : Breakable
{
	public static readonly SendTable DT_FuncTrackTrain = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncTrackTrain", DT_FuncTrackTrain).WithManualClassID(StaticClassIndices.CFuncTrackTrain);
}
