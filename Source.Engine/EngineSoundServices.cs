using Source.Common.Audio;
using Source.Engine.Client;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class EngineSoundServices : ISoundServices
{
	ClientState? cl;
	Host? host;
	TimeUnit_t frameTime;

	public void CacheBuildingFinish() {
		throw new NotImplementedException();
	}

	public void CacheBuildingStart() {
		throw new NotImplementedException();
	}

	public void CacheBuildingUpdateProgress(float percent, ReadOnlySpan<char> cachefile) {
		throw new NotImplementedException();
	}

	public void EmitCloseCaption(ReadOnlySpan<char> captionname, double duration) {
		throw new NotImplementedException();
	}

	public void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream) {
		throw new NotImplementedException();
	}

	public double GetClientTime() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetGameDir() {
		throw new NotImplementedException();
	}

	public TimeUnit_t GetHostFrametime() {
		return frameTime;
	}

	public double GetHostTime() {
		return (host ??= Singleton<Host>()).Time;
	}

	public ReadOnlySpan<char> GetPrecachedSound(int index) {
		throw new NotImplementedException();
	}

	public int GetPrecachedSoundCount() {
		throw new NotImplementedException();
	}

	public int GetServerCount() => (cl ??= Singleton<ClientState>()).ServerCount;

	public ReadOnlySpan<char> GetUILanguage() {
		throw new NotImplementedException();
	}

	public int GetViewEntity() => (cl ??= Singleton<ClientState>()).ViewEntity;

	public bool IsConnected() => (cl ??= Singleton<ClientState>()).IsConnected();

	public bool IsGameActive() {
		throw new NotImplementedException();
	}

	public bool IsGamePaused() {
		throw new NotImplementedException();
	}

	public bool IsPlayer(int source) {
		return source == (cl ??= Singleton<ClientState>()).PlayerSlot + 1;
	}

	public object? LevelAlloc(uint bytes, ReadOnlySpan<char> tag) {
		throw new NotImplementedException();
	}

	public void OnChangeVoiceStatus(int entity, bool status) {
		throw new NotImplementedException();
	}

	public void OnExtraUpdate() {
		throw new NotImplementedException();
	}

	public void OnSoundStarted(long guid, ref StartSoundParams parms, ReadOnlySpan<char> soundname) {

	}

	public void OnSoundStopped(long guid, int soundsource, SoundEntityChannel channel, ReadOnlySpan<char> soundname) {
	
	}

	public void RestartSoundSystem() {
		throw new NotImplementedException();
	}

	public void SetSoundFrametime(TimeUnit_t realDT, TimeUnit_t hostDt) {
		frameTime = realDT;
	}
}
