namespace Source.Common.Engine;


public struct ViewRect
{
	public int X, Y, Width, Height;
}

public interface IVideoMode
{
	bool Init();
	void DrawStartupGraphic();
	bool CreateGameWindow(int width, int height, bool windowed);
	void SetGameWindow(nint window);
	bool SetMode(int width, int height, bool windowed);
	ViewRects GetClientViewRect();
}