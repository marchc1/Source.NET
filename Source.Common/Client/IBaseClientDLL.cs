using Source.Common.Bitbuffers;
using Source.Common.Input;
using Source.Common.MaterialSystem;

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
	int HudVidInit();
	bool HandleUiToggle();
	void IN_DeactivateMouse();
	void IN_ActivateMouse();
	void View_Render(ViewRects screenrect);
	void InstallStringTableCallback(ReadOnlySpan<char> tableName);
	int IN_KeyEvent(int eventcode, ButtonCode keynum, ReadOnlySpan<char> currentBinding);
	void IN_OnMouseWheeled(int delta);
	void ExtraMouseSample(double frametime, bool active);
	void IN_ClearStates();
	bool ShouldAllowConsole();
	void FrameStageNotify(ClientFrameStage stage);
	ClientClass? GetAllClasses();
	RenamedRecvTableInfo? GetRenamedRecvTableInfos();
	void ErrorCreatingEntity(int entityIdx, int classIdx, int serialNumber);
	void InitSprite(EngineSprite? sprite, ReadOnlySpan<char> loadName);
	void LevelShutdown();
	LookupProxyInterfaceFn GetMaterialProxyInterfaceFn();
	void LevelInitPreEntity(ReadOnlySpan<char> mapname);
}
