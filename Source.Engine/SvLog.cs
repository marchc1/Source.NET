using Source.Common;

using System;

namespace Source.Engine;

[EngineComponent]
public class Log : IGameEventListener2
{
	public void FireGameEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public bool IsActive() => Active;
	public void SetLoggingState(bool state) => Active = state;

	bool Active;
}
