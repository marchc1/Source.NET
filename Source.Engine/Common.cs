using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Engine;
using Source.Common.Filesystem;

using System.Text;

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

	const int COM_TOKEN_MAX_LENGTH = 1024;
	static readonly byte[] com_token = new byte[COM_TOKEN_MAX_LENGTH];
	static bool com_ignorecolons = false;

	public static ReadOnlySpan<byte> ParseFile(ReadOnlySpan<byte> data, Span<char> token) {
		ReadOnlySpan<byte> returnData = Parse(data);
		ReadOnlySpan<byte> nullTermToken = com_token[..MemoryExtensions.IndexOf(com_token, (byte)0)];
		token.Clear(); // todo: only set one char
		Encoding.ASCII.GetChars(nullTermToken, token);

		return returnData;
	}

	static ReadOnlySpan<byte> Parse(ReadOnlySpan<byte> data) {
		byte c;
		int len;
		CharacterSet breaks;

		breaks = BreakSetIncludingColons;
		if (com_ignorecolons)
			breaks = BreakSet;

		len = 0;
		com_token[0] = 0;

		if (data.IsEmpty)
			return null;

		skipwhite:
		while ((c = data[0]) <= ' ') {
			if (c == 0)
				return null; 
			data = data[1..];
			if (data.IsEmpty)
				return null;
		}

		if (c == '/' && data[1] == '/') {
			while (!data.IsEmpty && data[0] != '\0' && data[0] != '\n')
				data = data[1..];
			goto skipwhite;
		}

		if (c == '\"') {
			data = data[1..];
			while (true) {
				c = data[0];
				data = data[1..];
				if (c == '\"' || c == '\0') {
					com_token[len] = 0;
					return data;
				}
				com_token[len] = c;
				len++;
			}
		}

		if (breaks.Contains((char)c)) {
			com_token[len] = c;
			len++;
			com_token[len] = 0;
			return data[1..];
		}

		do {
			com_token[len] = c;
			data = data[1..];
			len++;
			c = data[0];
			if (breaks.Contains((char)c))
				break;
		} while (c > 32);

		com_token[len] = 0;
		return data;
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

	}

	public static ReadOnlySpan<char> FormatSeconds(double v) {
		throw new NotImplementedException();
	}
}
