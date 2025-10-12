using Pastel;

using Source.Common.Commands;
using Source.Engine.Server;
using Source.GUI.Controls;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Source.Engine;
public class Sys(Host host, GameServer sv, ICommandLine CommandLine)
{
	public static double Time => Platform.Time;
	public bool Dedicated;
	public bool TextMode;

	public string? DisconnectReason = null;
	public string? ExtendedDisconnectReason = null;
	public bool ExtendedError = false;

	public Thread? MainThread;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool InMainThread() => Thread.CurrentThread == MainThread;

	public bool InitGame(bool dedicated, string rootDirectory) {
		MainThread = Thread.CurrentThread;
		Dbg.SpewActivate("console", 1);
		Dbg.SpewOutputFunc(SpewFunc);
		host.Initialized = false;
		Dedicated = dedicated;

		RunDataTableTest();

		host.Init(Dedicated);
		if (!host.Initialized)
			return false;

		return true;
	}

	private void RunDataTableTest() {
		// later
	}

	public void ShutdownGame() {

	}


	ThreadLocal<bool> inSpew = new();
	ThreadLocal<string> groupWrite = new();
	private void Write(string group, ReadOnlySpan<char> str, in Color color, bool routeInGame = false) {
		if (!groupWrite.IsValueCreated)
			groupWrite.Value = "";

		Span<char> buffer = stackalloc char[256];
		int bufferIdx = 0;
		unsafe void writeTxt(ReadOnlySpan<char> sub, in Color color) {
			Console.Write(sub.Pastel(color));
			if (routeInGame) {
				host.Con?.ColorPrintf(in color, sub);
			}
		}
		void flushTxt(Span<char> buffer, in Color color) {
			if (bufferIdx > 0) {
				ReadOnlySpan<char> sub = buffer[..bufferIdx];
				writeTxt(sub, in color);
			}
			bufferIdx = 0;
		}
		void writeGroup(ReadOnlySpan<char> group, in Color color) {
			writeTxt("[", in color);
			writeTxt(group, in color);
			writeTxt("] ", in color);
		}
		void writeNewLine() {
			writeTxt(Environment.NewLine, new(255, 255, 255));
		}
		void writeBuffer(Span<char> buffer, in Color color, char c) {
			if (bufferIdx >= buffer.Length)
				flushTxt(buffer, in color);
			buffer[bufferIdx++] = c;
		}
		for (int i = 0; i < str.Length; i++) {
			char c = str[i];
			if (c == '\n') {
				groupWrite.Value = "";

				flushTxt(buffer, in color);
				writeNewLine();
			}
			else {
				if (groupWrite.Value != group) {
					flushTxt(buffer, in color);
					writeGroup(group, in color);
					groupWrite.Value = group;
				}
				writeBuffer(buffer, in color, c);
			}
		}
		flushTxt(buffer, in color);
	}
	public SpewRetval SpewFunc(SpewType spewType, ReadOnlySpan<char> msg) {
		if (!inSpew.IsValueCreated)
			inSpew.Value = false;

		bool suppress = inSpew.Value;
		inSpew.Value = true;

		const string engineGroup = "engine";
		string? group = Dbg.GetSpewOutputGroup();
		group = string.IsNullOrEmpty(group) ? engineGroup : group;

		if (!suppress) {
			/*if (TextMode) {
				if(spewType == SpewType.Message || spewType == SpewType.Log) {
					Console.Write($"[{group}] {msg}");
				}
				else {
					Console.Write($"[{group}] {msg}");
				}
			}*/

			if ((spewType != SpewType.Log) || sv.GetMaxClients() == 1) {
				Color color = new();
				switch (spewType) {
					case SpewType.Warning: color.SetColor(255, 90, 90, 255); break;
					case SpewType.Assert: color.SetColor(255, 20, 20, 255); break;
					case SpewType.Error: color.SetColor(20, 70, 255, 255); break;
					default: color = Dbg.GetSpewOutputColor(); break;
				}
				Write(group, msg, color, true);
			}
			else {
				Color color = new Color(255, 255, 255);
				Write(group, msg, in color);
			}
		}

		inSpew.Value = false;
		if (spewType == SpewType.Error) {
			Error($"[{group}] {msg}");
			return SpewRetval.Abort;
		}

		if (spewType == SpewType.Assert) {
			if (CommandLine.FindParm("-noassert") == 0)
				return SpewRetval.Debugger;
			else
				return SpewRetval.Continue;
		}

		return SpewRetval.Continue;
	}

	public static void Error(ReadOnlySpan<char> msg) {
		Singleton<MessageBoxFn>().Invoke("Engine Error", msg, false);
		Environment.Exit(100);
	}

	public bool InEditMode() => false;
}
