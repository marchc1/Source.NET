using Source.Common;
using Source.Common.Commands;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public partial class Host
{

	[ConCommand("maps", "Displays list of maps.", FCvar.DontRecord, autoCompleteMethod: nameof(Map_CompletionFunc))]
	public void Maps_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		ReadOnlySpan<char> subString = null;

		if (args.ArgC() != 2 && args.ArgC() != 3) {
			ConMsg("Usage:  maps <substring>\nmaps * for full listing\n");
			return;
		}

		if (args.ArgC() == 2) {
			subString = args[1];
			if (subString.IsStringEmpty)
				return;
		}

		if (subString.Length != 0 && (subString[0] == '*'))
			subString = null;

		Msg("Finish the maps command!!\n");
	}

	[ConCommand("map", "Start playing on specified map.", FCvar.DontRecord, autoCompleteMethod: nameof(Map_CompletionFunc))]
	public void Map_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		Map_Helper(in args, source, false, false, false);
	}

	[ConCommand("map_background", "Runs a map as the background to the main menu.", FCvar.DontRecord, autoCompleteMethod: nameof(Map_CompletionFunc))]
	public void Host_Map_Background_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		Map_Helper(in args, source, false, true, false);
	}

	[ConCommand("map_commentary", "Start playing, with commentary, on a specified map.", FCvar.DontRecord, autoCompleteMethod: nameof(Map_CompletionFunc))]
	public void Host_Map_Commentary_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		Map_Helper(in args, source, false, false, true);
	}

	[ConCommand("changelevel", "Change server to the specified map", FCvar.DontRecord, autoCompleteMethod: nameof(Map_CompletionFunc))]
	public void Host_Changelevel_f(in TokenizedCommand args, CommandSource source, int clientSlot = -1) {
		if (args.ArgC() < 2) {
			ConMsg("changelevel <levelname> : continue game on a new level\n");
			return;
		}

		if (!sv.IsActive()) {
			ConMsg("Can't changelevel, not running server\n");
			return;
		}


		if (args.ArgC() >= 2)
			HostState.ChangeLevelMP(args[1], args[2]);
		else
			HostState.ChangeLevelMP(args[1], null);
	}
}
