using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source.Common.Bitbuffers;
using Source.Common.Client;
using Game.Client.Entity;
using Source.Common.Entity;
using Source.Common.Networking.DataTable;
using Source.Common;

namespace Game.Client;

public class HLClient(IInput input, UserMessages usermessages, ClientGlobalVariables globalVars) : IBaseClientDLL
{
	// We really need this.
	public static ClientGlobalVariables GlobalVars;
	public static ClientEntityList EntityList = new();
	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton<IInput, HLInput>();
	}

	public void IN_SetSampleTime(double frameTime) {

	}

	public void PostInit() {
		GlobalVars = globalVars;
	}

	public void CreateMove(int sequenceNumber, double inputSampleFrametime, bool active) {
		input.CreateMove(sequenceNumber, inputSampleFrametime, active);
	}

	public bool WriteUsercmdDeltaToBuffer(bf_write buf, int from, int to, bool isNewCommand) {
		return input.WriteUsercmdDeltaToBuffer(buf, from, to, isNewCommand);
	}
	public bool DisconnectAttempt() => false;

	public void HudText(ReadOnlySpan<char> text) {

	}

	public bool DispatchUserMessage(int msgType, bf_read msgData) {
		return usermessages.DispatchUserMessage(msgType, msgData);
	}

	public bool Init() {
		return true;
	}

	public void EncodeUserCmdToBuffer(bf_write buf, int slot) {
		input.EncodeUserCmdToBuffer(buf, slot);
	}

	public void DecodeUserCmdFromBuffer(bf_read buf, int slot) {
		input.DecodeUserCmdFromBuffer(buf, slot);
	}

	public IClientEntityList GetClientEntityList()
	{
		return EntityList;
	}

	public void FrameStageNotify(ClientFrameStage stage)
	{
		
	}

	public ClientClass? GetAllClientClasses()
	{
		return ClientClass.g_pClientClassHead;
	}
}
