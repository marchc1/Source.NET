using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class ServerPlugin : IServerPluginHelpers
{
	public void ClientCommand(Edict entity, ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}

	public void CreateMessage(Edict entity, DialogType type, KeyValues data, IServerPluginCallbacks plugin) {
		throw new NotImplementedException();
	}

	public int StartQueryCvarValue(Edict entity, ReadOnlySpan<char> pName) {
		throw new NotImplementedException();
	}

	public void GameFrame(bool simulating) {
		serverGameDLL.GameFrame(simulating);
	}
}
