using Source.Common;
using Source.Common.Bitbuffers;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class GameEventManager : IGameEventManager2
{
	public bool AddListener(IGameEventListener2 listener, ReadOnlySpan<char> name, bool serverSide) {
		throw new NotImplementedException();
	}

	public IGameEvent CreateEvent(ReadOnlySpan<char> name, bool force = false) {
		throw new NotImplementedException();
	}

	public IGameEvent DuplicateEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public bool FindListener(IGameEventListener2 listener, ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public bool FireEvent(IGameEvent ev, bool dontBroadcast = false) {
		throw new NotImplementedException();
	}

	public bool FireEventClientSide(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public void FreeEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public int LoadEventsFromFile(ReadOnlySpan<char> filename) {
		throw new NotImplementedException();
	}

	public void RemoveListener(IGameEventListener2 listener) {
		throw new NotImplementedException();
	}

	public void Reset() {
		throw new NotImplementedException();
	}

	public bool SerializeEvent(IGameEvent ev, bf_write buf) {
		throw new NotImplementedException();
	}

	public IGameEvent UnserializeEvent(bf_read buf) {
		throw new NotImplementedException();
	}
}
