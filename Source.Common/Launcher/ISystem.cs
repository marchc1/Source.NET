using Source.Common.Formats.Keyvalues;
using Source.Common.Input;

namespace Source.Common.Launcher;

public interface ISystem
{
	void RunFrame();
	void ShellExecute(ReadOnlySpan<char> command, ReadOnlySpan<char> file);
	double GetFrameTime();
	double GetCurrentTime();
	long GetTimeMillis();
	void GetUILanguage(Span<char> destination);
	void SetUILanguage(ReadOnlySpan<char> incoming);
	nuint GetClipboardTextCount();
	void SetClipboardText(ReadOnlySpan<char> text);
	nuint GetClipboardText(nint offset, Span<char> buf);
	KeyValues? GetUserConfigFileData(ReadOnlySpan<char> dialogName, int dialogID);
	void SetUserConfigFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathName);
	void SaveUserConfigFile();
	bool SetWatchForComputerUse(bool state);
	double GetTimeSinceLastUse();
	int GetAvailableDrives(Span<char> buf);
	bool CommandLineParamExists(ReadOnlySpan<char> paramName);
	ReadOnlySpan<char> GetFullCommandLine();
	ButtonCode KeyCode_VirtualKeyToVGUI(int keyCode);
	bool GetCurrentTimeAndDate(out int year, out int month, out int dayOfWeek, out int day, out int hour, out int minute, out int second);

	// returns the amount of available disk space, in bytes, on the drive
	// path can be any path, drive letter is stripped out
	double GetFreeDiskSpace(ReadOnlySpan<char> path);

	// shortcut (.lnk) modification functions
	bool CreateShortcut(ReadOnlySpan<char> linkFileName, ReadOnlySpan<char> targetPath, ReadOnlySpan<char> arguments, ReadOnlySpan<char> workingDirectory, ReadOnlySpan<char> iconFile);
	bool GetShortcutTarget(ReadOnlySpan<char> linkFileName, Span<char> targetPath, Span<char> arguments);
	bool ModifyShortcutTarget(ReadOnlySpan<char> linkFileName, ReadOnlySpan<char> targetPath, ReadOnlySpan<char> arguments, ReadOnlySpan<char> workingDirectory);

	// gets the string following a command line param
	//!! move this function up on changing interface version number
	bool GetCommandLineParamValue(ReadOnlySpan<char> paramName, Span<char> value);

	// recursively deletes a registry key and all it's subkeys
	//!! move this function next to other registry function on changing interface version number
	bool DeleteRegistryKey(ReadOnlySpan<char> keyName);

	ReadOnlySpan<char> GetDesktopFolderPath();

	// use this with the "open" command to launch web browsers/explorer windows, eg. ShellExecute("open", "www.valvesoftware.com")
	void ShellExecuteEx(ReadOnlySpan<char> command, ReadOnlySpan<char> file, ReadOnlySpan<char> pParams);

	// Copy a portion of the application client area to the clipboard
	//  (x1,y1) specifies the top-left corner of the client rect to copy
	//  (x2,y2) specifies the bottom-right corner of the client rect to copy
	// Requires: x2 > x1 && y2 > y1
	// Dimensions of the copied rectangle are (x2 - x1) x (y2 - y1)
	// Pixel at (x1,y1) is copied, pixels at column x2 and row y2 are *not* copied
	void SetClipboardImage(IWindow wnd, int x1, int y1, int x2, int y2);

	/// <summary>
	/// Makes an attempt to query the operating system for an installed font's filepath.
	/// </summary>
	/// <param name="fontName"></param>
	/// <returns></returns>
	ReadOnlySpan<char> GetSystemFontPath(ReadOnlySpan<char> fontName, int weight = 0);
}
