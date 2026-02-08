using Source.Common.GUI;
using Source.Common.ShaderAPI;

namespace Source.Common.Launcher;

public interface ILauncherManager
{
	bool CreateGameWindow(string title, bool windowed, int width, int height);

	void SetWindowFullScreen(bool fullscreen, int width, int height);
	bool IsWindowFullScreen();

	void MoveWindow(int x, int y);
	void SizeWindow(int width, int tall);
	void PumpWindowsMessageLoop();

	void DestroyGameWindow();
	void SetApplicationIcon(ReadOnlySpan<char> appIconFile);

	void GetMouseDelta(out int x, out int y, bool ignoreNextMouseDelta = false);

	void GetNativeDisplayInfo(int nDisplay, out uint width, out uint height, out uint refreshHz);
	int GetDisplayModeCount(int displayIndex);
	void GetDisplayMode(int displayIndex, int modeIndex, out ShaderDisplayMode info);
	int GetCurrentDisplayIndex();

	void RenderedSize(bool set, ref int width, ref int height);
	void DisplayedSize(out int width, out int height);

	IWindow GetWindow();
	int GetEvents(WindowEvent[] eventBuffer, int length);
	void CenterWindow(int v2, int v3);
	ICursor? GetHardwareCursor(HCursor cursor);
	ICursor? GetSoftwareCursor(HCursor cursor, out float x, out float y);
	void SetMouseCursor(ICursor? currentlySetCursor);
	void SetMouseVisible(bool v);
	void SetWindowRelativeMouseMode(bool cursorLocked);
}
