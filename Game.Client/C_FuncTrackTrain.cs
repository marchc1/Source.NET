using Game.Shared;

using Source.Common;


namespace Game.Client;
[NetworkName("CFuncTrackTrain")]
public class C_FuncTrackTrain : C_BaseEntity
{
	public static readonly RecvTable DT_FuncTrackTrain = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FuncTrackTrain);
}
