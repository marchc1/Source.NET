#define DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
using Source.Common;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Source;

public enum SpewType
{
	Message,
	Warning,
	Assert,
	Error,
	Log,

	Count
}

public enum SpewRetval
{
	Debugger,
	Continue,
	Abort
}

public class SpewGroup
{
	public string GroupName = "";
	public int Level;

	public SpewGroup(string name, int level) {
		GroupName = name;
		Level = level;
	}
}

public struct SpewInfo()
{
	public Color SpewOutputColor;
	public string SpewOutputGroup = "";
	public int SpewOutputLevel;
}

public delegate SpewRetval SpewOutputFunc(SpewType spewType, ReadOnlySpan<char> message);
public delegate void AssertFailedNotifyFunc(string file, int line, string message);

public static class Dbg
{
	
	static SpewOutputFunc _SpewOutputFunc = DefaultSpewFunc;
	static AssertFailedNotifyFunc? _AssertFailedNotifyFunc = null;

	static string FileName;
	static int Line;
	static SpewType SpewType;

	static int DefaultLevel;
	static Color DefaultOutputColor = new Color(255, 255, 255, 255);

	static readonly ThreadLocal<SpewInfo?> SpewInfo = new();

	public static readonly string GROUP_DEVELOPER = "developer";
	public static readonly string GROUP_CONSOLE = "console";
	public static readonly string GROUP_NETWORK = "network";

	static readonly ConcurrentDictionary<string, SpewGroup> SpewGroups = [];

	public static void SpewOutputFunc(SpewOutputFunc? func) => _SpewOutputFunc = func ?? DefaultSpewFunc;
	public static SpewOutputFunc GetSpewOutputFunc() => _SpewOutputFunc != null ? _SpewOutputFunc : DefaultSpewFunc;

	public static SpewRetval DefaultSpewFunc(SpewType type, ReadOnlySpan<char> message) {
		foreach (char c in message) {
			System.Console.Write(c);
		}
#if DEBUG
		if (type == SpewType.Assert) {
			return SpewRetval.Debugger;
		}
		else 
#endif
		if (type == SpewType.Error)
			return SpewRetval.Abort;
		else return SpewRetval.Continue;
	}
	public static SpewRetval DefaultSpewFuncAbortOnAsserts(SpewType type, string message) {
		SpewRetval r = DefaultSpewFunc(type, message);
		if (type == SpewType.Assert)
			r = SpewRetval.Abort;
		return r;
	}

	public static string? GetSpewOutputGroup() {
		SpewInfo? info = SpewInfo.IsValueCreated ? SpewInfo.Value : null;
		Debug.Assert(info != null);
		return info?.SpewOutputGroup;
	}
	public static int GetSpewOutputLevel() {
		SpewInfo? info = SpewInfo.IsValueCreated ? SpewInfo.Value : null;
		Debug.Assert(info != null);
		return info?.SpewOutputLevel ?? -1;
	}
	public static Color GetSpewOutputColor() {
		SpewInfo? info = SpewInfo.IsValueCreated ? SpewInfo.Value : null;
		Debug.Assert(info != null);
		return info?.SpewOutputColor ?? DefaultOutputColor;
	}
	public static void _SpewInfo(SpewType type, string file, int line) {
		FileName = file;
		Line = line;
		SpewType = type;
	}

	public static unsafe SpewRetval _SpewMessage(SpewType spewType, string groupName, int level, in Color color, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		char* piece = stackalloc char[2048];
		int writer = 0;
		SpewRetval ret = SpewRetval.Continue;

		SpewInfo info = new() {
			SpewOutputColor = color,
			SpewOutputGroup = groupName,
			SpewOutputLevel = level
		};
		SpewInfo.Value = info;

		SpewRetval writeOnePiece() {
			var result = _SpewOutputFunc(spewType, new(piece, writer));
			writer = 0;
			return result;
		}

		bool handleOnePiece(SpewRetval ret) {
			switch (ret) {
				case SpewRetval.Debugger:
					if (spewType != SpewType.Assert)
						Debugger.Break();
					return true;

				case SpewRetval.Abort:
					Environment.Exit(1);
					return false;

				case SpewRetval.Continue: return true;
			}

			return true;
		}

		CFormatReader reader = new(msgFormat);
		while (!reader.Overflowed()) {
			Span<char> target = new(piece, 2048);
			writer = sprintf(target, ref reader);

			ret = writeOnePiece();
			SpewInfo.Value = null;
			if (!handleOnePiece(ret))
				return ret;
		}

		return ret;
	}
	public static bool FindSpewGroup(string groupName, [NotNullWhen(true)] out SpewGroup? group) {
		return SpewGroups.TryGetValue(groupName, out group);
	}
	public static bool IsSpewActive(string groupName, int level) {
		if (FindSpewGroup(groupName, out SpewGroup? group))
			return group.Level >= level;
		else
			return DefaultLevel >= level;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpewRetval _SpewMessage(SpewType spewType, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(spewType, "", 0, in DefaultOutputColor, msgFormat, args);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpewRetval _SpewMessage([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(SpewType, msgFormat, args);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpewRetval _DSpewMessage(string groupName, int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(groupName, level))
			return SpewRetval.Continue;

		return _SpewMessage(SpewType, groupName, level, DefaultOutputColor, msgFormat, args);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SpewRetval _ColorSpewMessage(SpewType type, in Color color, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string msgFormat, params object?[] args) {
		return _SpewMessage(SpewType, "", 0, color, msgFormat, args);
	}

	public static void Msg([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(SpewType.Message, msgFormat, args);
	public static void DMsg(string groupName, int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string msgFormat, params object?[] args) {
		if (!IsSpewActive(groupName, level)) return;
		_SpewMessage(SpewType.Warning, groupName, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void Warning([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(SpewType.Message, msgFormat, args);
	public static void DWarning(string groupName, int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string msgFormat, params object?[] args) {
		if (!IsSpewActive(groupName, level)) return;
		_SpewMessage(SpewType.Warning, groupName, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void Log([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(SpewType.Log, msgFormat, args);
	public static void DLog(string groupName, int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string msgFormat, params object?[] args) {
		if (!IsSpewActive(groupName, level)) return;
		_SpewMessage(SpewType.Log, groupName, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void Error([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args)
		=> _SpewMessage(SpewType.Error, msgFormat, args);

	
	public static void ErrorIfNot([DoesNotReturnIf(false)] bool condition, ReadOnlySpan<char> msg) {
		if (!condition) {
			Debug.Assert(false);
			Error(msg);
		}
	}


	public static void DevMsg(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, level)) return;
		_SpewMessage(SpewType.Message, GROUP_DEVELOPER, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void DevWarning(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, level)) return;
		_SpewMessage(SpewType.Warning, GROUP_DEVELOPER, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void DevLog(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, level)) return;
		_SpewMessage(SpewType.Log, GROUP_DEVELOPER, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void DevMsg([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, 1)) return;
		_SpewMessage(SpewType.Message, GROUP_DEVELOPER, 1, in DefaultOutputColor, msgFormat, args);
	}
	public static void DevWarning([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, 1)) return;
		_SpewMessage(SpewType.Warning, GROUP_DEVELOPER, 1, in DefaultOutputColor, msgFormat, args);
	}
	public static void DevLog([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_DEVELOPER, 1)) return;
		_SpewMessage(SpewType.Log, GROUP_DEVELOPER, 1, in DefaultOutputColor, msgFormat, args);
	}



	public static void ConColorMsg(int level, in Color clr, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, level, in clr, msgFormat, args);
	}
	public static void ConMsg(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConWarning(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Warning, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConLog(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Log, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}

	public static void ConColorMsg(in Color clr, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 1)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, 1, in clr, msgFormat, args);
	}
	public static void ConMsg([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 1)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, 1, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConWarning([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 1)) return;
		_SpewMessage(SpewType.Warning, GROUP_CONSOLE, 1, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConLog([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 1)) return;
		_SpewMessage(SpewType.Log, GROUP_CONSOLE, 1, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConDColorMsg(in Color clr, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 2)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, 2, in clr, msgFormat, args);
	}
	public static void ConDMsg([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 2)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, 2, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConDWarning([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 2)) return;
		_SpewMessage(SpewType.Warning, GROUP_CONSOLE, 2, in DefaultOutputColor, msgFormat, args);
	}
	public static void ConDLog([StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, 2)) return;
		_SpewMessage(SpewType.Log, GROUP_CONSOLE, 2, in DefaultOutputColor, msgFormat, args);
	}





	public static void NetMsg(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Message, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void NetWarning(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Warning, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}
	public static void NetLog(int level, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] ReadOnlySpan<char> msgFormat, params object?[] args) {
		if (!IsSpewActive(GROUP_CONSOLE, level)) return;
		_SpewMessage(SpewType.Log, GROUP_CONSOLE, level, in DefaultOutputColor, msgFormat, args);
	}

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	static void _AssertMsg([DoesNotReturnIf(false)] bool exp, string message, object?[] parms, string file, int line, bool fatal) {
		if (!exp)
			_AssertMsg(true, string.Format(message, parms), file, line, fatal);
	}

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	static void _AssertMsg([DoesNotReturnIf(false)] bool exp, ReadOnlySpan<char> message, string file, int line, bool fatal) {
		if (exp) {

		}
		else {
			_SpewInfo(SpewType.Assert, file, line);
			if (!AssertDialog.ShouldUseNewAssertDialog() || AssertDialog.DoNewAssertDialog(file, line, message))
				Debugger.Break();


			if (fatal)
				_ExitOnFatalAssert(file, line);
		}
	}

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	public static void _ExitOnFatalAssert(string file, int line) {
		_SpewMessage("Fatal assert failed: {0}, line {1}.  Application exiting.\n", file, line);
		if (!Debugger.IsAttached) {
			// how would we even possibly minidump in C#? todo look into that
		}

		DevMsg(1, "_ExitOnFatalAssert\n");
		Environment.Exit(1);
	}

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Assert([DoesNotReturnIf(false)] bool exp,
		[CallerArgumentExpression(nameof(exp))] string? ____expI = null,
		[CallerFilePath] string? ____fileP = null,
		[CallerLineNumber] int ____lineNum = -1
	) {
		if (exp)
			return;

		const string ASSERTION_FAILED = "Assertion Failed: ";
		Span<char> finalMsg = stackalloc char[ASSERTION_FAILED.Length + (____expI?.Length) ?? 0];
		ASSERTION_FAILED.CopyTo(finalMsg);
		____expI?.CopyTo(finalMsg[ASSERTION_FAILED.Length..]);
		_AssertMsg(exp, finalMsg, ____fileP ?? "<nofile>", ____lineNum, false);
	}

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Assert(object? exp,
		[CallerArgumentExpression(nameof(exp))] string? ____expI = null,
		[CallerFilePath] string? ____fileP = null,
		[CallerLineNumber] int ____lineNum = -1
	) => _AssertMsg(exp != null, $"Assertion Failed: {____expI ?? "<NULL>"}", ____fileP ?? "<nofile>", ____lineNum, false);

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AssertMsg([DoesNotReturnIf(false)] bool exp, ReadOnlySpan<char> msg,
		[CallerArgumentExpression(nameof(exp))] string? ____expI = null,
		[CallerFilePath] string? ____fileP = null,
		[CallerLineNumber] int ____lineNum = -1,
		params object?[] args
	) => _AssertMsg(exp, $"Assertion Failed: {new(msg)}", ____fileP ?? "<nofile>", ____lineNum, false);

	[Conditional("DBGFLAG_ASSERT")]
#if DBGFLAG_HIDE_ASSERTS_FROM_DEBUGGING_STACK
	[DebuggerHidden]
#endif
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AssertEquals<T>(T? i1, T? i2,
		[CallerFilePath] string? ____fileP = null,
		[CallerLineNumber] int ____lineNum = -1,
		params object?[] args
	) => _AssertMsg(i1 == null ? i2 == null : i1.Equals(i2), "Expected {0} but got {1}!", args, ____fileP ?? "<nofile>", ____lineNum, false);

	public static void SpewActivate(string groupName, int level) {
		Assert(groupName != null);

		if (groupName[0] == '*' && groupName.Length == 1) {
			DefaultLevel = level;
			return;
		}

		if (!FindSpewGroup(groupName, out SpewGroup? spewGroup)) {
			// Insert an entry
			spewGroup = new SpewGroup(groupName, level);
			SpewGroups[groupName] = spewGroup;
		}

		spewGroup.Level = level;
	}

	public static void TimestampedLog(string v) {
		double curStamp = Platform.Time;
	}
}
