using Source.Engine.Client;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class SoundServices
{
	ClientState? cl;
	TimeUnit_t frameTime;
	public TimeUnit_t GetHostFrametime() {
		return frameTime;
	}
	public int GetServerCount() => (cl ??= Singleton<ClientState>()).ServerCount;
	public bool IsConnected() => (cl ??= Singleton<ClientState>()).IsConnected();
	public void SetSoundFrametime(TimeUnit_t realDT, TimeUnit_t hostDt) {
		frameTime = realDT;
	}
}
