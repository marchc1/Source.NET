#if CLIENT_DLL || GAME_DLL
using Source.Common;

namespace Game.Shared;

public abstract class GameEventListener : IGameEventListener2, IExtDisposable
{
	public void ListenForGameEvent(ReadOnlySpan<char> name) {
		RegisteredForEvents = true;
#if CLIENT_DLL
		bool serverSide = false;
#else
		bool serverSide = true;
#endif
		gameeventmanager?.AddListener(this, name, serverSide);
	}

	public void StopListeningForAllEvents() {
		if (RegisteredForEvents) {
			gameeventmanager?.RemoveListener(this);
			RegisteredForEvents = false;
		}
	}

	public abstract void FireGameEvent(IGameEvent ev);
	bool RegisteredForEvents;

	bool disposed;
	public void Dispose() {
		disposed = true;
		StopListeningForAllEvents();
		GC.SuppressFinalize(this);
	}
	public bool Disposed() => !disposed;
}
#endif
