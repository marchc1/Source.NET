﻿using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Engine.Client;
using Source.Engine.Server;

using System;
using System.Text;

namespace Source.Engine;

public class Cmd(IEngineAPI provider, IFileSystem fileSystem)
{
	public const string CMDSTR_ADD_EXECUTION_MARKER = "[$&*,`]";

	Cbuf Cbuf;
	Host Host;
	ICvar cvar;
	ClientState cl;
	GameServer sv;
	ICommandLine CommandLine;
	CvarUtilities cv;
	public void Init() {
		Cbuf = provider.GetRequiredService<Cbuf>();
		Host = provider.GetRequiredService<Host>();
		cvar = provider.GetRequiredService<ICvar>();
		cl = provider.GetRequiredService<ClientState>();
		sv = provider.GetRequiredService<GameServer>();
		cv = provider.GetRequiredService<CvarUtilities>();
		CommandLine = provider.GetRequiredService<ICommandLine>();
	}
	public void Shutdown() { }

	Dictionary<string, string> aliases = [];

	public CommandSource Source;
	public int ClientSlot;

	private void HandleExecutionMarker(ReadOnlySpan<char> command, ReadOnlySpan<char> markerCodeS) {
		char cCommand = command[0];
		int markerCode = (int.TryParse(markerCodeS, out int i) ? i : -1);

		if (Cbuf.FindAndRemoveExecutionMarker(markerCode)) {
			if (cCommand == (char)CmdExecutionMarker.EnableServerCanExecute) ++filterCommandsByServerCanExecute;
			else if (cCommand == (char)CmdExecutionMarker.DisableServerCanExecute) --filterCommandsByServerCanExecute;
			else if (cCommand == (char)CmdExecutionMarker.EnableClientCmdCanExecute) ++filterCommandsByClientCmdCanExecute;
			else if (cCommand == (char)CmdExecutionMarker.DisableClientCmdCanExecute) --filterCommandsByClientCmdCanExecute;
		}
		else {
			Dbg.Warning("Invalid execution marker code.\n");
		}
	}

	public ConCommandBase? ExecuteCommand(ref TokenizedCommand command, CommandSource source, int clientSlot = -1) {
		if (command.ArgC() == 0)
			return null;

		if (command[0].Equals(CMDSTR_ADD_EXECUTION_MARKER, StringComparison.OrdinalIgnoreCase)) {
			if (command.ArgC() == 3)
				HandleExecutionMarker(command[1], command[2]);
			else
				Dbg.Warning("WARNING: INVALID EXECUTION MARKER.\n");

			return null;
		}

		if (aliases.TryGetValue(new(command[0]), out var aliasValue)) {
			Cbuf.InsertText(aliasValue);
			return null;
		}

		Source = source;
		ClientSlot = clientSlot;

		ConCommandBase? commandBase = cvar.FindCommandBase(command[0]);

		if (ShouldPreventServerCommand(commandBase))
			return null;

		if (commandBase != null && commandBase.IsCommand()) {
			if (!ShouldPreventClientCommand(commandBase) && commandBase.IsCommand()) {
				bool isServerCommand = commandBase.IsFlagSet(FCvar.GameDLL) && source == CommandSource.Command && !sv.IsDedicated();

				if (sv.IsActive()) {
					//todo: all that command stuff for servers one day
				}
				else if (isServerCommand) {
					// We're not the server, but we are connected - so we'll try to forward it
					if (cl.IsConnected()) {
						ForwardToServer(in command);
						return null;
					}

					// Server command, not connected, not executing
					return null;
				}

				if (commandBase.IsFlagSet(FCvar.Cheat)) {
					if (!Host.IsSinglePlayerGame() && !Host.CanCheat()) {
						// TODO; allow server to run it...
						Dbg.Msg($"Can't use cheat command {commandBase.GetName()} in multiplayer, unless the server has sv_cheats set to 1.\n");
						return null;
					}
				}

				if (commandBase.IsFlagSet(FCvar.SingleplayerOnly)) {
					if (!Host.IsSinglePlayerGame()) {
						Dbg.Msg($"Can't use command {commandBase.GetName()} in multiplayer.\n");
						return null;
					}
				}

				// development only stuff

				Dispatch(commandBase, in command);
				return commandBase;
			}
		}

		if (commandBase != null && source == CommandSource.Command && CommandLine.CheckParm("-default") && !commandBase.IsFlagSet(FCvar.ExecDespiteDefault)) {
			Dbg.Msg($"Ignoring cvar \"{commandBase.GetName()}\" due to -default on command line\n");
			return null;
		}

		if (cv.IsCommand(command))
			return commandBase;

		if (source == CommandSource.Command) {
			if (cl.IsConnected()) {
				ForwardToServer(command);
			}
		}

		Dbg.Msg($"Unknown command \"{command[0]}\"\n");
		return null;
	}

	public void ForwardToServer(in TokenizedCommand command) {
#if !SWDS
		Span<char> str = stackalloc char[1024];
		if (command.Arg(0).Equals("cmd", StringComparison.OrdinalIgnoreCase))
			command.ArgS(1).CopyTo(str);
		else
			command.ArgS(0).CopyTo(str);
		cl.SendStringCmd(str);
#endif
	}

	private void Dispatch(ConCommandBase commandBase, in TokenizedCommand command) {
		ConCommand conCommand = (ConCommand)commandBase;
		conCommand.Dispatch(in command, Source, ClientSlot);
	}

	public bool ShouldPreventClientCommand(ConCommandBase? cmd) {
		if (filterCommandsByClientCmdCanExecute > 0 && cmd != null && cmd.IsFlagSet(FCvar.ClientCmdCanExecute)) {
			// If this command is in the game DLL, don't mention it because we're going to forward this
			// request to the server and let the server handle it.
			if (!cmd.IsFlagSet(FCvar.GameDLL)) 
				Dbg.Warning($"FCvar.ServerCanExecute prevented server running command: {cmd.GetName()}\n");

			return true;
		}

		return false;
	}

	int filterCommandsByServerCanExecute = 0;
	int filterCommandsByClientCmdCanExecute = 0;

	public bool ShouldPreventServerCommand(ConCommandBase? cmd) {
		if (!Host.IsSinglePlayerGame()) {
			// todo: make this work.
			if (filterCommandsByServerCanExecute > 0) {
				if (cmd == null)
					return false;

				if (!cmd.IsFlagSet(FCvar.ServerCanExecute)) {
					Dbg.Warning($"FCvar.ServerCanExecute prevented server running command: {cmd.GetName()}\n");
					return true;
				}
			}
		}

		return false;
	}

	[ConCommand(helpText: "Forward command to server.")]
	void cmd(in TokenizedCommand args) {
		ForwardToServer(in args);
	}

	[ConCommand(helpText: "Alias a command.")]
	void alias(in TokenizedCommand args) {
		int argc = args.ArgC();
		if (argc == 1) {
			Dbg.ConMsg("Current alias commands:\n");
			foreach (var alias in aliases) {
				Dbg.ConMsg($"  {alias.Key} : {alias.Value}\n");
			}
			return;
		}

		ReadOnlySpan<char> s = args[1];
		ReadOnlySpan<char> restOfCommandLine = args.ArgS(startingArg: 2);

		aliases[new string(s)] = new string(restOfCommandLine);
	}
	[ConCommand(helpText: "Echos text to console.")]
	void echo(in TokenizedCommand args) {
		for (int i = 1, argc = args.ArgC(); i < argc; i++)
			Dbg.ConMsg($"{args[i]} ");
		Dbg.ConMsg("\n");
	}
	[ConCommand(helpText: "Parses and stuffs command line + commands to command buffer.")]
	void stuffcmds(in TokenizedCommand args) {
		if (args.ArgC() != 1) {
			Dbg.ConMsg("stuffcmds: execute command line parameters\n");
			return;
		}

		string build = "";
		for (int i = 1; i < CommandLine.ParmCount(); i++) {
			ReadOnlySpan<char> parm = CommandLine.GetParm(i);
			if (parm == null || parm.Length == 0) continue;

			if (parm[0] == '-') {
				ReadOnlySpan<char> value = CommandLine.ParmValueByIndex(i);
				if (value != null)
					i++;
				continue;
			}
			if (parm[0] == '+') {
				ReadOnlySpan<char> value = CommandLine.ParmValueByIndex(i);
				if (value != null && value.Length > 0) {
					build += ($"{parm[1..]} {value}\n");
					i++;
				}
				else {
					build += new string(parm[1..]);
					build += ('\n');
				}
			}
			else {
				ReadOnlySpan<char> translated = TranslateFileAssociation(CommandLine.GetParm(i));
				if (translated != null) {
					build += new string(translated);
					build += ('\n');
				}
			}
		}
		build += ('\0');
		if (build.Length > 0)
			Cbuf.InsertText(build);
	}

	static bool IsValidFileExtension(ReadOnlySpan<char> filename) {
		if (filename == null)
			return false;

		if (
			filename.Contains(".exe", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".vbs", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".com", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".bat", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".dll", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".ini", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".gcf", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".sys", StringComparison.OrdinalIgnoreCase) ||
			filename.Contains(".blob", StringComparison.OrdinalIgnoreCase)
		)
			return false;

		return true;
	}

	[ConCommand(helpText: "Execute script file.")]
	void exec(in TokenizedCommand args) {
		lock (Cbuf.Buffer) {
			int argc = args.ArgC();
			if (argc != 2) {
				Dbg.ConMsg("exec <filename>: execute a script file\n");
				return;
			}

			ReadOnlySpan<char> file = $"cfg/{args[1]}";
			ReadOnlySpan<char> pathID = "MOD";

			if (!Common.IsValidPath(file)) {
				Dbg.ConMsg("exec {0}: invalid path.\n", new string(file));
				return;
			}

			if (!IsValidFileExtension(file)) {
				Dbg.ConMsg("exec {0}: invalid file type.\n", new string(file));
				return;
			}

			if (true) {
				if (fileSystem.FileExists(file)) {
					long size = fileSystem.Size(file);
					if (size > 1 * 1024 * 1024) {
						Dbg.ConMsg($"exec {file}: file size larger than 1 MiB!\n");
						return;
					}
				}
				else {
					Dbg.ConMsg($"'{file}' not present; not executing.\n");
					return;
				}
			}

			using var execFile = fileSystem.Open(file, FileOpenOptions.Read, pathID);
			using StreamReader reader = new StreamReader(execFile!.Stream);
			Dbg.ConDMsg($"execing {file}\n");
			LinkedListNode<CommandBuffer.Command>? hCommand = Cbuf.Buffer.GetNextCommandHandle();
			// check to make sure we're not going to overflow later
			while (!reader.EndOfStream) {
				string? line = reader.ReadLine().Trim() + "\0";
				if (line == null)
					break;

				Cbuf.InsertText(line);

				while (Cbuf.Buffer.GetNextCommandHandle() != hCommand) {
					if (Cbuf.Buffer.DequeueNextCommand()) {
						Cbuf.ExecuteCommand(ref Cbuf.Buffer.GetCommand(), CommandSource.Command);
					}
					else {
						Dbg.Assert(false);
						break;
					}
				}
			}
		}
	}

	private ReadOnlySpan<char> TranslateFileAssociation(string v) {
		// do later!
		return null;
	}
}
