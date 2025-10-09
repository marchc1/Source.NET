using Source.Common.Formats.Keyvalues;
using Source.Common.Input;
using Source.Common.Launcher;
using System.Text.RegularExpressions;
using Source.Common.Commands;
using SDL;
using System.Runtime.InteropServices;
using System.Text;






#if WIN32
using Microsoft.Win32;
#endif

namespace Source.SDLManager;

public unsafe class SDL3_System(ICommandLine commandLine) : ISystem
{
	public bool CommandLineParamExists(ReadOnlySpan<char> paramName) {
		return commandLine.FindParm(paramName) != 0;
	}

	public bool CreateShortcut(ReadOnlySpan<char> linkFileName, ReadOnlySpan<char> targetPath, ReadOnlySpan<char> arguments, ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> iconFile) {
		throw new NotImplementedException();
	}

	public bool DeleteRegistryKey(ReadOnlySpan<char> keyName) {
		throw new NotImplementedException();
	}

	public int GetAvailableDrives(Span<char> buf) {
		return 0;
	}

	public unsafe nuint GetClipboardText(nint offset, Span<char> buf) {
		if (!SDL3.SDL_HasClipboardText())
			return 0;

		byte* clipboard = SDL3.Unsafe_SDL_GetClipboardText();
		nuint len = SDL3.SDL_strlen(clipboard);
		nuint clipboardSize = (nuint)Encoding.UTF8.GetCharCount(clipboard, (int)len) * sizeof(char);
		char* clipboardCast = (char*)NativeMemory.Alloc(clipboardSize);
		Encoding.UTF8.GetChars(clipboard, (int)len, clipboardCast, (int)clipboardSize);
		new Span<char>(clipboardCast, (int)clipboardSize)[..Math.Min(buf.Length, (int)clipboardSize)].CopyTo(buf);
		NativeMemory.Free(clipboardCast);
		return len;
	}

	public nuint GetClipboardTextCount() {
		if (!SDL3.SDL_HasClipboardText())
			return 0;

		byte* clipboard = SDL3.Unsafe_SDL_GetClipboardText();
		nuint len = SDL3.SDL_strlen(clipboard);
		return len;
	}

	public bool GetCommandLineParamValue(ReadOnlySpan<char> paramName, Span<char> value) {
		throw new NotImplementedException();
	}

	public double GetCurrentTime() {
		return Platform.Time;
	}

	public bool GetCurrentTimeAndDate(out int year, out int month, out int dayOfWeek, out int day, out int hour, out int minute, out int second) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetDesktopFolderPath() {
		throw new NotImplementedException();
	}

	public double GetFrameTime() {
		return FrameTime;
	}

	public double GetFreeDiskSpace(ReadOnlySpan<char> path) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetFullCommandLine() {
		throw new NotImplementedException();
	}

	public bool GetRegistryInteger(ReadOnlySpan<char> key, out int value) {
		throw new NotImplementedException();
	}

	public bool GetShortcutTarget(ReadOnlySpan<char> linkFileName, Span<char> targetPath, Span<char> arguments) {
		throw new NotImplementedException();
	}

	public long GetTimeMillis() {
		return (long)(GetCurrentTime() * 1000);
	}

	public double GetTimeSinceLastUse() {
		throw new NotImplementedException();
	}

	public KeyValues? GetUserConfigFileData(ReadOnlySpan<char> dialogName, int dialogID) {
		throw new NotImplementedException();
	}

	public ButtonCode KeyCode_VirtualKeyToVGUI(int keyCode) {
		throw new NotImplementedException();
	}

	public bool ModifyShortcutTarget(ReadOnlySpan<char> linkFileName, ReadOnlySpan<char> targetPath, ReadOnlySpan<char> arguments, ReadOnlySpan<char> workingDirectory) {
		throw new NotImplementedException();
	}

	double FrameTime;

	public void RunFrame() {
		FrameTime = GetCurrentTime();
	}

	public void SaveUserConfigFile() {
		throw new NotImplementedException();
	}

	public void SetClipboardImage(IWindow wnd, int x1, int y1, int x2, int y2) {
		throw new NotImplementedException();
	}

	public unsafe void SetClipboardText(ReadOnlySpan<char> text) {
		if (text.IsEmpty)
			SDL3.SDL_SetClipboardText("");

		if (text.Length == 0)
			SDL3.SDL_SetClipboardText("");

		nuint bytes = (nuint)Encoding.UTF8.GetByteCount(text);
		byte* rawData = (byte*)NativeMemory.Alloc(bytes);
		Encoding.UTF8.GetBytes(text, new Span<byte>(rawData, (int)bytes));
		SDL3.SDL_SetClipboardText(rawData);
		NativeMemory.Free(rawData);
	}

	public bool SetRegistryInteger(ReadOnlySpan<char> key, int value) {
		throw new NotImplementedException();
	}

	public void SetUserConfigFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathName) {
		throw new NotImplementedException();
	}

	public bool SetWatchForComputerUse(bool state) {
		throw new NotImplementedException();
	}

	public void ShellExecute(ReadOnlySpan<char> command, ReadOnlySpan<char> file) {
		throw new NotImplementedException();
	}

	public void ShellExecuteEx(ReadOnlySpan<char> command, ReadOnlySpan<char> file, ReadOnlySpan<char> pParams) {
		throw new NotImplementedException();
	}

#if WIN32
#pragma warning disable CA1416 // Validate platform compatibility (WIN32 ifdef catches this instead)
	static string[] possibleKeys = [@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Fonts"];
	public ReadOnlySpan<char> GetSystemFontPath(ReadOnlySpan<char> fontName) {
		foreach (var keyPath in possibleKeys) {
			using var baseKey = Registry.LocalMachine.OpenSubKey(keyPath, false);
			if (baseKey is null)
				continue;

			foreach (var valueName in baseKey.GetValueNames()) {
				var normalized = Regex.Replace(valueName, @"\s*\(.*?\)$", "").Trim();
				if (fontName.Equals(normalized, StringComparison.OrdinalIgnoreCase)) {
					var fontFile = baseKey.GetValue(valueName)?.ToString();
					if (string.IsNullOrEmpty(fontFile))
						continue;

					return Path.IsPathRooted(fontFile)
						? fontFile
						: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontFile);
				}
			}
		}
		return null;
	}
#pragma warning restore CA1416 // Validate platform compatibility
#elif LINUX
public ReadOnlySpan<char> GetSystemFontPath(ReadOnlySpan<char> fontName) {
		try {
			var processStartInfo = new System.Diagnostics.ProcessStartInfo {
				FileName = "fc-match",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			// Use ArgumentList to escape fontName
			processStartInfo.ArgumentList.Add("--format=%{file}");
			processStartInfo.ArgumentList.Add(new string(fontName));

			using var process = System.Diagnostics.Process.Start(processStartInfo);
			if (process != null) {
				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output)) {
					return output.Trim();
				}
			}
		} catch(Exception ex) {
			Assert($"GetSystemFontPath: {ex.Message}");
		}

		return null;
	}
#else
#error Please implement System.GetSystemFontPath for this platform
#endif

	string language = "english";
	public void GetUILanguage(Span<char> destination) {
		language.AsSpan().ClampedCopyTo(destination);
	}
	public void SetUILanguage(ReadOnlySpan<char> incoming) {
		language = new(incoming);
	}
}
