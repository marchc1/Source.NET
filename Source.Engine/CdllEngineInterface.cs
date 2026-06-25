using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Formats.Keyvalues;
using Source.Common.Input;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Common.Physics;
using Source.Engine.Client;
using Source.Engine.Server;

using System.Numerics;

namespace Source.Engine;

public class EngineClient(Cbuf Cbuf, Scr Scr, Con Con, Key Key, IGame game, Host Host,
							IMaterialSystem materials, MaterialSystem_Config MaterialSystemConfig,
							MatSysInterface MatSys, ModelLoader modelloader) : IEngineClient
{
	public ReadOnlySpan<char> Key_LookupBinding(ReadOnlySpan<char> binding) => Key.NameForBinding(binding);
	public void GetMainMenuBackgroundName(Span<char> dest) {
		"background05".CopyTo(dest);
	}

	public void Con_NXPrintf(in Con_NPrint_s np, ReadOnlySpan<char> text) => Con.NXPrintF(in np, text);

	public bool IsDrawingLoadingImage() => Scr.DrawLoading;

	public int GetMaxClients() => cl.MaxClients;

	public ReadOnlySpan<char> GetLevelName() {
		if (sv.IsDedicated())
			return "Dedicated Server";
		else if (!cl.IsConnected())
			return "";
		return cl.LevelFileName;
	}

	public int GetLocalPlayer() => cl.PlayerSlot + 1;
	public void ClientCmd_Unrestricted(ReadOnlySpan<char> cmdString) => Cbuf.AddText(cmdString);
	public void ExecuteClientCmd(ReadOnlySpan<char> cmdString) {
		Cbuf.AddText(cmdString);
		Cbuf.Execute();
	}

	public void ClientCmd(ReadOnlySpan<char> cmdString) {
		if (cl.RestrictClientCommands && !Cbuf.HasRoomForExecutionMarkers(2)) {
			AssertMsg(false, "EngineClient.ClientCmd called but there is no room for the execution markers. Ignoring command.");
			return;
		}

		if (cl.RestrictClientCommands)
			Cbuf.AddTextWithMarkers(CmdExecutionMarker.EnableClientCmdCanExecute, cmdString, CmdExecutionMarker.DisableClientCmdCanExecute);
		else
			Cbuf.AddText(cmdString);
	}

	public bool IsLevelMainMenuBackground() => sv.IsLevelMainMenuBackground();

	public bool IsPaused() => cl.IsPaused();

	public bool GetPlayerInfo(int playerIndex, out PlayerInfo playerInfo) {
		playerIndex--;
		if (playerIndex >= cl.MaxClients || playerIndex < 0) {
			playerInfo = new();
			return false;
		}

		Assert(cl.UserInfoTable != null);
		if (cl.UserInfoTable == null) {
			playerInfo = new();
			return false;
		}

		Assert(playerIndex < cl.UserInfoTable.GetNumStrings());
		if (playerIndex >= cl.UserInfoTable.GetNumStrings()) {
			playerInfo = new();
			return false;
		}

		Span<byte> pi = cl.UserInfoTable.GetStringUserData(playerIndex);
		PlayerInfo.FromBytes(pi, out playerInfo);
		return true;
	}

	public bool Con_IsVisible() => Con.IsVisible();

	ReadOnlySpan<byte> IEngineClient.ParseFile(ReadOnlySpan<byte> data, Span<char> token) => Common.ParseFile(data, token);

	public void GetViewAngles(out QAngle viewangles) {
		viewangles = cl.ViewAngles;
	}

	public void SetViewAngles(in QAngle viewangles) {
		cl.ViewAngles = QAngle.Normalize(in viewangles);
	}

	public void GetScreenSize(out int w, out int h) {
		// Is this even right???
		using MatRenderContextPtr renderContext = new(materials);
		renderContext.GetWindowSize(out w, out h);
	}

	readonly ILauncherManager launcherMgr = Singleton<ILauncherManager>();
	public void GetMouseDelta(out int dx, out int dy, bool ignoreNextMouseDelta = false) {
		launcherMgr.GetMouseDelta(out dx, out dy);
	}

	public bool IsConnected() {
		return cl.IsConnected();
	}

	public bool IsInGame() {
		return cl.IsActive();
	}

	public double GetLastTimeStamp() {
		return cl.LastServerTickTime;
	}

	public uint GetProtocolVersion() => Protocol.VERSION;

	public SkyboxVisibility IsSkyboxVisibleFromPoint(in Vector3 point) {
		if (MaterialSystemConfig.Fullbright == 1)
			return SkyboxVisibility.Skybox3D;

		int leaf = CM.PointLeafnum(point);
		int flags = GetCollisionBSPData()!.MapLeafs[leaf].Flags;
		if ((flags & BSPFileCommon.LEAF_FLAGS_SKY) != 0)
			return SkyboxVisibility.Skybox3D;
		return ((flags & BSPFileCommon.LEAF_FLAGS_SKY2D) != 0) ? SkyboxVisibility.Skybox2D : SkyboxVisibility.NotVisible;
	}

	public float GetScreenAspectRatio() {
		return MatSys.GetScreenAspect();
	}

	public bool IsPlayingDemo() => false; // Demos arent implemented yet
	public bool IsPlayingTimeDemo() => false; // Demos arent implemented yet
	public INetChannelInfo? GetNetChannelInfo() => cl.NetChannel;
	public void FireEvents() { } // todo

	public Model? LoadModel(ReadOnlySpan<char> name, bool prop) {
		return modelloader.GetModelForName(name, prop ? ModelLoaderFlags.DetailProp : ModelLoaderFlags.ClientDLL);
	}

	// These need more BSP work.
	public bool IsBoxVisible(in Vector3 mins, in Vector3 maxs) {
		return true;
	}

	public int GetPlayerForUserID(int userID) {
		if (cl.UserInfoTable == null)
			return 0;

		int maxClients = Math.Min(cl.MaxClients, cl.UserInfoTable.GetNumStrings());
		for (int i = 0; i < maxClients; i++) {
			Span<byte> pi = cl.UserInfoTable.GetStringUserData(i);
			if (!PlayerInfo.FromBytes(pi, out PlayerInfo playerInfo))
				continue;

			if (playerInfo.UserID == userID)
				return (i + 1);
		}

		return 0;
	}

	public TimeUnit_t Time() => Sys.Time;

	public int GetIntersectingSurfaces(Model? model, in Vector3 center, float radius, bool onlyVisibleSurfaces, ref SurfInfo infos, int maxInfos) {
		throw new NotImplementedException();
	}

	public Vector3 GetLightForPoint(in Vector3 pos, bool clamp) {
		throw new NotImplementedException();
	}

	public IMaterial? TraceLineMaterialAndLighting(in Vector3 start, in Vector3 end, out Vector3 diffuseLightColor, out Vector3 baseColor) {
		throw new NotImplementedException();
	}

	public bool CopyLocalFile(ReadOnlySpan<char> source, ReadOnlySpan<char> destination) {
		throw new NotImplementedException();
	}

	public void ServerCmd(ReadOnlySpan<char> szCmdString, bool bReliable = true) {
		throw new NotImplementedException();
	}

	public ref ClientTextMessage TextMessageGet(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public float GetSentenceLength(AudioSource? audioSource) {
		throw new NotImplementedException();
	}

	public bool IsStreaming(AudioSource? audioSource) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> Key_BindingForKey(ButtonCode code) {
		throw new NotImplementedException();
	}

	public void StartKeyTrapMode() {
		throw new NotImplementedException();
	}

	public bool CheckDoneKeyTrapping(out ButtonCode code) {
		throw new NotImplementedException();
	}

	public void Con_NPrintf(int pos, ReadOnlySpan<char> text) {
		// todo
	}

	public bool IsBoxInViewCluster(in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public bool CullBox(in Vector3 mins, in Vector3 maxs) {
		return true;
	}

	public void Sound_ExtraUpdate() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetGameDirectory() {
		throw new NotImplementedException();
	}

	public ref readonly Matrix4x4 WorldToScreenMatrix() => ref g_EngineRenderer.WorldToScreenMatrix();
	public ref readonly Matrix4x4 WorldToViewMatrix() {
		throw new NotImplementedException();
	}

	public int GameLumpVersion(int lumpId) {
		throw new NotImplementedException();
	}

	public int GameLumpSize(int lumpId) {
		throw new NotImplementedException();
	}

	public bool LoadGameLump(int lumpId, Span<byte> buffer) {
		throw new NotImplementedException();
	}

	public int LevelLeafCount() {
		throw new NotImplementedException();
	}

	public ISpatialQuery? GetBSPTreeQuery() {
		throw new NotImplementedException();
	}

	public void LinearToGamma(Span<float> linear, Span<float> gamma) {
		throw new NotImplementedException();
	}

	public float LightStyleValue(int style) {
		throw new NotImplementedException();
	}

	public void ComputeDynamicLighting(in Vector3 pt, in Vector3 normal, out Vector3 color) {
		throw new NotImplementedException();
	}

	public void GetAmbientLightColor(out Vector3 color) {
		throw new NotImplementedException();
	}

	public int GetDXSupportLevel() {
		throw new NotImplementedException();
	}

	public bool SupportsHDR() {
		throw new NotImplementedException();
	}

	public void Mat_Stub(IMaterialSystem? matSys) {
		throw new NotImplementedException();
	}

	public void GetChapterName(Span<char> buff) {
		throw new NotImplementedException();
	}

	public int GetLevelVersion() {
		throw new NotImplementedException();
	}

	public ref IVoiceTweak GetVoiceTweakAPI() {
		throw new NotImplementedException();
	}

	public void EngineStats_BeginFrame() {
		throw new NotImplementedException();
	}

	public void EngineStats_EndFrame() {
		throw new NotImplementedException();
	}

	public int GetLeavesArea(Span<int> leaves) {
		throw new NotImplementedException();
	}

	public bool DoesBoxTouchAreaFrustum(in Vector3 mins, in Vector3 maxs, int iArea) {
		throw new NotImplementedException();
	}

	public void SetAudioState(in AudioState state) {
		throw new NotImplementedException();
	}

	public int SentenceGroupPick(int groupIndex, Span<char> name, int nameBufLen) {
		throw new NotImplementedException();
	}

	public int SentenceGroupPickSequential(int groupIndex, Span<char> name, int nameBufLen, int sentenceIndex, int reset) {
		throw new NotImplementedException();
	}

	public int SentenceIndexFromName(ReadOnlySpan<char> sentenceName) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> SentenceNameFromIndex(int sentenceIndex) {
		throw new NotImplementedException();
	}

	public int SentenceGroupIndexFromName(ReadOnlySpan<char> grouname) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> SentenceGrounameFromIndex(int groupIndex) {
		throw new NotImplementedException();
	}

	public float SentenceLength(int sentenceIndex) {
		throw new NotImplementedException();
	}

	public void ComputeLighting(in Vector3 pt, in Vector3 normal, bool clamp, out Vector3 color, Span<Vector3> boxColors = default) {
		throw new NotImplementedException();
	}

	public void ActivateOccluder(int nOccluderIndex, bool bActive) {
		throw new NotImplementedException();
	}

	public bool IsOccluded(in Vector3 absMins, in Vector3 absMaxs) {
		throw new NotImplementedException();
	}

	public void DebugDrawPhysCollide(PhysCollide collide, IMaterial material, in Matrix3x4 transform, in Color color) {
		throw new NotImplementedException();
	}

	public void CheckPoint(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public void DrawPortals() {
		throw new NotImplementedException();
	}

	public bool IsRecordingDemo() {
		throw new NotImplementedException();
	}

	public int GetDemoRecordingTick() {
		throw new NotImplementedException();
	}

	public int GetDemoPlaybackTick() {
		throw new NotImplementedException();
	}

	public int GetDemoPlaybackStartTick() {
		throw new NotImplementedException();
	}

	public float GetDemoPlaybackTimeScale() {
		throw new NotImplementedException();
	}

	public int GetDemoPlaybackTotalTicks() {
		throw new NotImplementedException();
	}

	public bool IsTakingScreenshot() {
		throw new NotImplementedException();
	}

	public bool IsHLTV() => false; // not hltv ever, hltv probably will never be implemented

	public void SetOcclusionParameters(in OcclusionParams parms) {
		throw new NotImplementedException();
	}

	public void GetUILanguage(Span<char> dest) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMapEntitiesString() {
		throw new NotImplementedException();
	}

	public bool IsInEditMode() => false; //todo

	public uint GetEngineBuildNumber() => Protocol.VERSION;

	public ReadOnlySpan<char> GetProductVersionString() {
		throw new NotImplementedException();
	}

	public void GrabPreColorCorrectedFrame(int x, int y, int width, int height) {
		throw new NotImplementedException();
	}

	public bool IsHammerRunning() {
		throw new NotImplementedException();
	}

	public bool MapHasHDRLighting() {
		throw new NotImplementedException();
	}

	public int GetAppID() {
		throw new NotImplementedException();
	}

	public Vector3 GetLightForPointFast(in Vector3 pos, bool bClamp) {
		throw new NotImplementedException();
	}

	public void SetRestrictServerCommands(bool restrict) => cl.RestrictServerCommands = restrict;
	public void SetRestrictClientCommands(bool restrict) => cl.RestrictClientCommands = restrict;

	public void SetOverlayBindProxy(int overlayID, object bindProxy) {
		throw new NotImplementedException();
	}

	public bool CopyFrameBufferToMaterial(ReadOnlySpan<char> materialName) {
		throw new NotImplementedException();
	}

	public void ChangeTeam(ReadOnlySpan<char> teamName) {
		throw new NotImplementedException();
	}

	public void ReadConfiguration(bool readDefault = false) => Host.ReadConfiguration();
	public void SetAchievementMgr(IAchievementMgr? achievementMgr) {
		throw new NotImplementedException();
	}

	public IAchievementMgr? GetAchievementMgr() {
		throw new NotImplementedException();
	}

	public bool MapLoadFailed() => serverGlobalVariables.MapLoadFailed;
	public void SetMapLoadFailed(bool state) => serverGlobalVariables.MapLoadFailed = state;
	public bool IsLowViolence() => Host.LowViolence;

	public ReadOnlySpan<char> GetMostRecentSaveGame() {
		throw new NotImplementedException();
	}

	public void SetMostRecentSaveGame(ReadOnlySpan<char> lpszFilename) {
		throw new NotImplementedException();
	}

	public bool IsSaveInProgress() {
		throw new NotImplementedException();
	}

	public uint OnStorageDeviceAttached() {
		throw new NotImplementedException();
	}

	public void OnStorageDeviceDetached() {
		throw new NotImplementedException();
	}

	public void ResetDemoInterpolation() {
		throw new NotImplementedException();
	}

	public void SetGamestatsData(object? gamestatsData) {
		throw new NotImplementedException();
	}

	public object? GetGamestatsData() {
		throw new NotImplementedException();
	}

	public void ServerCmdKeyValues(KeyValues keyValues) => cl.SendServerCmdKeyValues(keyValues);
	public bool IsSkippingPlayback() => false; // TODO: demo support?
	public bool IsLoadingDemo() => false; // TODO: demo support?
	public bool IsPlayingDemoALocallyRecordedDemo() => false; // TODO: demo support?
	public ReadOnlySpan<char> Key_LookupBindingExact(ReadOnlySpan<char> binding) => Key.NameForBindingExact(binding);

	public bool IsWindowedMode() => videoMode!.IsWindowedMode();
	public void FlashWindow() => launcherMgr.FlashWindow(true);

	public int GetClientVersion() => GetSteamInfIDVersionInfo().ClientVersion;
	public bool IsActiveApp() => game.IsActiveApp();

	public void DisconnectInternal() => Host.Disconnect(true, "");

	public int GetInstancesRunningCount() {
		throw new NotImplementedException();
	}
}
