namespace Source.Common.Engine;


public struct ViewRect
{
	public int X, Y, Width, Height;
}

public interface IVideoMode
{
	bool Init();
	void DrawStartupGraphic();
	bool CreateGameWindow(int width, int height, bool windowed, bool borderless);
	void SetGameWindow(nint window);
	bool SetMode(int width, int height, bool windowed, bool borderless);
	ViewRects GetClientViewRect();
	int GetModeCount();
	ref VMode GetMode(int mode);
	Span<VMode> GetModes();
	Span<VMode> GetCustomModes();
}
