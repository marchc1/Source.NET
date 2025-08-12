using Source.Common.Bitbuffers;
using Source.Common.Entity;
using Source.Common.Networking.DataTable;

namespace Source.Common.Client;

/// <summary>
/// Interface exposed from the client DLL back to the engine
/// </summary>
public interface IBaseClientDLL
{
	void PostInit();
	void IN_SetSampleTime(double frameTime);
	public void CreateMove(int sequenceNumber, double inputSampleFrametime, bool active);
	public bool WriteUsercmdDeltaToBuffer(bf_write buf, int from, int to, bool isNewCommand);
	public void EncodeUserCmdToBuffer(bf_write buf, int slot);
	public void DecodeUserCmdFromBuffer(bf_read buf, int slot);
	bool DisconnectAttempt();
	void HudText(ReadOnlySpan<char> text);
	bool DispatchUserMessage(int msgType, bf_read msgData);
	bool Init();
	public IClientEntityList GetClientEntityList();
	public void FrameStageNotify(ClientFrameStage curStage);
	public ClientClass? GetAllClientClasses();
}
