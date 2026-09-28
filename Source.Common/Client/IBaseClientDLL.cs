using Source.Common.Bitbuffers;
using Source.Common.Hashing;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.Common.Networking;

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
	bool DispatchUserMessage(int msgType, bf_read msgData);
	bool Init();
	int HudVidInit();
	void HudProcessInput(bool active);
	void HudUpdate(bool active);
	void HudReset();
	void HudText(ReadOnlySpan<char> text);
	bool HandleUiToggle();
	void IN_DeactivateMouse();
	void IN_ActivateMouse();
	void IN_Accumulate();
	bool IN_IsKeyDown( ReadOnlySpan<char> name, out bool isDown);
	void View_Render(ViewRects screenrect);
	void InstallStringTableCallback(ReadOnlySpan<char> tableName);
	int IN_KeyEvent(int eventcode, ButtonCode keynum, ReadOnlySpan<char> currentBinding);
	void IN_OnMouseWheeled(int delta);
	void ExtraMouseSample(double frametime, bool active);
	void IN_ClearStates();
	bool ShouldAllowConsole();
	bool ShouldDrawDropdownConsole();
	void FrameStageNotify(ClientFrameStage stage);
	ClientClass? GetAllClasses();
	RenamedRecvTableInfo? GetRenamedRecvTableInfos();
	void ErrorCreatingEntity(int entityIdx, int classIdx, int serialNumber);
	void InitSprite(EngineSprite? sprite, ReadOnlySpan<char> loadName);
	void LevelShutdown();
	void Shutdown() { }
	LookupProxyInterfaceFn GetMaterialProxyInterfaceFn();
	void LevelInitPreEntity(ReadOnlySpan<char> mapname);
	void LevelInitPostEntity();
	void GMod_RequestLuaFiles(INetChannel netchan);
	void GMod_ReceiveLuaFile(ReadOnlySpan<char> fileName, in SHA256Value sha256, ReadOnlySpan<byte> compressed);
	void FileReceived(ReadOnlySpan<char> fileName, uint transferID);
	StandardRecvProxies GetStandardRecvProxies();

	// Called when a player starts or stops talking.
	// entindex is -1 to represent the local client talking (before the data comes back from the server). 
	// entindex is -2 to represent the local client's voice being acked by the server.
	// entindex is GetPlayer() when the server acknowledges that the local client is talking.
	void VoiceStatus(int entityIndex, bool talking) { }
	// Given a list of "S(wavname) S(wavname2)" tokens, look up the localized text and emit
	//  the appropriate close caption if running with closecaption = 1
	void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream) { }
	// Emits a regular close caption by token name
	void EmitCloseCaption(ReadOnlySpan<char> captionname, float duration) { }
	// Get the mouthinfo for the sound being played inside UI panels
	Audio.MouthInfo? GetClientUIMouthInfo() => null;
}
