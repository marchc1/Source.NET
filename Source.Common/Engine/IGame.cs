using Source.Common.Launcher;

namespace Source.Common.Engine;

public interface IGame
{
	bool CreateGameWindow(int width, int height, bool windowed);
	void DestroyGameWindow();
	void SetGameWindow(IWindow window);
	bool InputAttachToGameWindow();
	void InputDetachFromGameWindow();

	void PlayStartupVideos();
	nint GetMainWindow();
	IWindow GetMainDeviceWindow();

	nint GetMainWindowAddress();
	void GetDesktopInfo(out uint width, out uint height, out uint refreshrate);

	void SetWindowXY(int x, int y);
	void SetWindowSize(int w, int h);

	void GetWindowRect(out int x, out int y, out int w, out int h);

	// Not Alt-Tabbed away
	bool IsActiveApp();
	void SetActiveApp(bool state);

	void DispatchAllStoredGameMessages();
}
