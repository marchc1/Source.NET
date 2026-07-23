using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.ToolFramework;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Engine;

public interface IEngineToolInternal : IEngineTool { 

}

public class EngineToolImpl : IEngineToolInternal
{
	bool SuppressDeInit;
	public bool ShouldSuppressDeInit() => SuppressDeInit;

	public void CancelMovieRecording() {
		throw new NotImplementedException();
	}

	public void ChangeToMap(ReadOnlySpan<char> mapname) {
		throw new NotImplementedException();
	}

	public double ClientFrameTime() {
		throw new NotImplementedException();
	}

	public long ClientTick() {
		throw new NotImplementedException();
	}

	public double ClientTime() {
		throw new NotImplementedException();
	}

	public void Command(ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}

	public void Con_NPrint(int pos, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void Con_NXPrint(in Con_NPrint_s info, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public ushort CreatePartitionHandle(IHandleEntity? entity, int listMask, in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public void CreatePickingRay(in ViewSetup viewSetup, int x, int y, ref Vector3 org, ref Vector3 forward) {
		throw new NotImplementedException();
	}

	public void DestroyPartitionHandle(ushort partition) {
		throw new NotImplementedException();
	}

	public void ElementMoved(ushort handle, in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public void EndMovieRecording() {
		throw new NotImplementedException();
	}

	public void Execute() {
		throw new NotImplementedException();
	}

	public void ForceSend() {
		throw new NotImplementedException();
	}

	public void ForceUpdateDuringPause() {
		throw new NotImplementedException();
	}

	public void GetClientFactory(out IServiceProvider factory) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetCurrentMap() {
		throw new NotImplementedException();
	}

	public void GetGameDir(Span<char> getGameDir) {
		throw new NotImplementedException();
	}

	public int GetMaxClients() {
		throw new NotImplementedException();
	}

	public Model? GetModel(uint hEntity) {
		throw new NotImplementedException();
	}

	public float GetMono16Samples(ReadOnlySpan<char> pszName, List<short> sampleList) {
		throw new NotImplementedException();
	}

	public bool GetPlayerView(out ViewSetup playerView, int x, int y, int w, int h) {
		throw new NotImplementedException();
	}

	public int GetPointContents(in Vector3 position) {
		throw new NotImplementedException();
	}

	public double GetRealFrameTime() {
		throw new NotImplementedException();
	}

	public double GetRealTime() {
		throw new NotImplementedException();
	}

	public void GetScreenSize(out int width, out int height) {
		throw new NotImplementedException();
	}

	public void GetServerFactory(out IServiceProvider factory) {
		throw new NotImplementedException();
	}

	public float GetSoundDuration(ReadOnlySpan<char> pszName) {
		throw new NotImplementedException();
	}

	public float GetSoundDuration(int guid) {
		throw new NotImplementedException();
	}

	public StudioHeader? GetStudioModel(uint hEntity) {
		throw new NotImplementedException();
	}

	public double GetTimescale() {
		throw new NotImplementedException();
	}

	public void GetWorldToScreenMatrixForView(in ViewSetup view, in Matrix4x4 matrix) {
		throw new NotImplementedException();
	}

	public long HostFrameCount() {
		throw new NotImplementedException();
	}

	public double HostFrameTime() {
		throw new NotImplementedException();
	}

	public long HostTick() {
		throw new NotImplementedException();
	}

	public double HostTime() {
		throw new NotImplementedException();
	}

	public void InstallPartitionQueryCallback<T>(T query) where T : IPartitionQueryCallback {
		throw new NotImplementedException();
	}

	public bool IsConnected() {
		throw new NotImplementedException();
	}

	public bool IsConsoleVisible() {
		throw new NotImplementedException();
	}

	public bool IsGamePaused() {
		throw new NotImplementedException();
	}

	public bool IsInGame() {
		throw new NotImplementedException();
	}

	public bool IsLoopingSound(int guid) {
		throw new NotImplementedException();
	}

	public bool IsMapValid(ReadOnlySpan<char> mapname) {
		throw new NotImplementedException();
	}

	public bool IsRecordingMovie() {
		throw new NotImplementedException();
	}

	public bool IsSoundStillPlaying(int guid) {
		throw new NotImplementedException();
	}

	public bool IsVoiceRecording() {
		throw new NotImplementedException();
	}

	public bool PrecacheModel(ReadOnlySpan<char> name, bool preload = false) {
		throw new NotImplementedException();
	}

	public bool PrecacheSound(ReadOnlySpan<char> name, bool preload = false) {
		throw new NotImplementedException();
	}

	public void ReloadSound(ReadOnlySpan<char> pSample) {
		throw new NotImplementedException();
	}

	public void RemovePartitionQueryCallback<T>(T query) where T : IPartitionQueryCallback {
		throw new NotImplementedException();
	}

	public void RenderView(ref ViewSetup view, int flags, int whatToRender) {
		throw new NotImplementedException();
	}

	public double ServerFrameTime() {
		throw new NotImplementedException();
	}

	public long ServerTick() {
		throw new NotImplementedException();
	}

	public double ServerTickInterval() {
		throw new NotImplementedException();
	}

	public double ServerTime() {
		throw new NotImplementedException();
	}

	public void SetAudioState(in AudioState audioState) {
		throw new NotImplementedException();
	}

	public void SetClientFrameTime(double frametime) {
		throw new NotImplementedException();
	}

	public void SetGamePaused(bool paused) {
		throw new NotImplementedException();
	}

	public void SetMainView(in Vector3 origin, in QAngle angles) {
		throw new NotImplementedException();
	}

	public void SetTimescale(double scale) {
		throw new NotImplementedException();
	}

	public void StartMovieRecording(KeyValues? movieParams) {
		throw new NotImplementedException();
	}

	public void StartRecordingVoiceToFile(ReadOnlySpan<char> filename, ReadOnlySpan<char> pathID = default) {
		throw new NotImplementedException();
	}

	public int StartSound(int userData, bool staticsound, int entIndex, SoundEntityChannel channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, in Vector3 origin, in Vector3 direction, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, bool updatePositions = true, float delay = 0, int speakerentity = -1) {
		throw new NotImplementedException();
	}

	public void StopAllSounds() {
		throw new NotImplementedException();
	}

	public void StopRecordingVoiceToFile() {
		throw new NotImplementedException();
	}

	public void StopSoundByGuid(int guid) {
		throw new NotImplementedException();
	}

	public void TakeTGAScreenShot(ReadOnlySpan<char> filename, int width, int height) {
		throw new NotImplementedException();
	}

	public double Time() {
		throw new NotImplementedException();
	}

	public void TraceRay<ITF>(in Ray ray, Mask mask, scoped in ITF traceFilter, ref Trace trace) where ITF : ITraceFilter {
		throw new NotImplementedException();
	}

	public void TraceRayServer<ITF>(in Ray ray, Mask mask, scoped in ITF traceFilter, ref Trace trace) where ITF : ITraceFilter {
		throw new NotImplementedException();
	}
}

public static class EngineTool
{
	public static bool SuppressDeInit() {
		return g_EngineTool.ShouldSuppressDeInit();
	}
	internal static void OverrideSampleRate(ref int rate) {
		if (SuppressDeInit())
			rate = 11025;
	}
}
