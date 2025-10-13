using Game.Shared;

using Source.Common;


namespace Game.Client;
public class C_FuncTrackTrain : C_BaseEntity
{
	public static readonly RecvTable DT_FuncTrackTrain = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncTrackTrain", DT_FuncTrackTrain).WithManualClassID(StaticClassIndices.CFuncTrackTrain);
}
