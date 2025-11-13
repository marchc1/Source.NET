using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Engine;
using Source.Common.Filesystem;

namespace Source.Engine;

/// <summary>
/// Common functionality
/// </summary>
/// <param name="providers"></param>
public class Common(IServiceProvider providers, ILocalize? Localize, Sys Sys)
{
	readonly static CharacterSet BreakSet = new("{}()");
	readonly static CharacterSet BreakSetIncludingColons = new("{}()':");

	public static string Gamedir { get; private set; }

	public void InitFilesystem(ReadOnlySpan<char> fullModPath) {
		CFSSearchPathsInit initInfo = new();
		IEngineAPI engineAPI = providers.GetRequiredService<IEngineAPI>();
		Host Host = providers.GetRequiredService<Host>();
		FileSystem FileSystem = providers.GetRequiredService<FileSystem>();

		initInfo.FileSystem = engineAPI.GetRequiredService<IFileSystem>();
		initInfo.DirectoryName = new(fullModPath);
		if (initInfo.DirectoryName == null)
			initInfo.DirectoryName = Host.GetCurrentGame();

		Host.CheckGore();

		initInfo.LowViolence = Host.LowViolence;
		initInfo.MountHDContent = false; // Study this further

		FileSystem.LoadSearchPaths(in initInfo);

		Gamedir = initInfo.ModPath ?? "";
	}

	public bool Initialized { get; private set; }
	public void Init() {
		Initialized = true;
	}

	public static bool IsValidPath(ReadOnlySpan<char> filename) {
		if (filename.IsEmpty)
			return false;

		if (filename.Length == 0
			|| filename.Contains("\\\\", StringComparison.OrdinalIgnoreCase) // To protect network paths
			|| filename.Contains(":", StringComparison.OrdinalIgnoreCase) // To protect absolute paths
			|| filename.Contains("..", StringComparison.OrdinalIgnoreCase) // To protect relative paths
			|| filename.Contains("\n", StringComparison.OrdinalIgnoreCase)
			|| filename.Contains("\r", StringComparison.OrdinalIgnoreCase)
		)
			return false;

		return true;
	}

	public void ExplainDisconnection(bool print, ReadOnlySpan<char> disconnectReason) {
		if (print && !disconnectReason.IsEmpty) {
			if (disconnectReason.Length > 0 && disconnectReason[0] == '#')
				disconnectReason = Localize == null ? disconnectReason : Localize.Find(disconnectReason);

			ConMsg($"{disconnectReason}\n");
		}
		Sys.DisconnectReason = new(disconnectReason);
		Sys.ExtendedError = true;
	}

	internal static void TimestampedLog(ReadOnlySpan<char> msg) {
		string time = DateTime.Now.ToString("G");
		Span<char> finalMsg = stackalloc char[msg.Length + 5 + time.Length];
		finalMsg[0] = '[';
		time.CopyTo(finalMsg[1..]);
		"]: ".CopyTo(finalMsg[(1 + time.Length)..]);
		msg.CopyTo(finalMsg[(1 + time.Length + 3)..]);
		finalMsg[^1] = '\n';
		Msg(finalMsg);
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}
}
