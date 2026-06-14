using Source.Common.Bitmap;

namespace Source.Common.Engine;


public struct ViewRect
{
	public int X, Y, Width, Height;
}

/// <summary>
/// General merging of things, differentiates from Source's garbage when managing video modes. Allows arbitrary user data for the video mode
/// </summary>
public struct UserVideoMode {
	public int Width;
	public int Height;
	public int RefreshRate;
	public bool Windowed;
	public bool Borderless;
}

public interface IVideoMode
{
	bool Init();
	void DrawStartupGraphic();
	bool CreateGameWindow(in UserVideoMode videomode);
	void SetGameWindow(nint window);
	bool SetMode(in UserVideoMode videomode);
	ViewRects GetClientViewRect();
}
