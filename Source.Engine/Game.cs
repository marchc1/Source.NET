using Microsoft.Extensions.DependencyInjection;

using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Common.Launcher;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Source.Engine;

public delegate void GameMessageFn(in InputEvent ev);
public struct GameMessageHandler
{
	public InputEventType EventType;
	public GameMessageFn Function;
	public GameMessageHandler(InputEventType eventType, GameMessageFn function) {
		EventType = eventType;
		Function = function;
	}
}

public class Game : IGame
{
	readonly ILauncherManager? launcherManager;
	readonly Sys Sys;
	readonly IFileSystem fileSystem;
	readonly IInputSystem inputSystem;
	readonly IMatSystemSurface surface;
	readonly IEngine eng;
	readonly Host Host;
	readonly IBaseClientDLL? clientDLL;
	readonly IServiceProvider services;
	readonly Key Key;
	public Game(Host host, ILauncherManager? launcherManager, IBaseClientDLL? clientDLL, Sys Sys, IFileSystem fileSystem, IInputSystem inputSystem, IMatSystemSurface surface, IEngine eng, IServiceProvider services, Key Key) {
		Host = host;

		this.launcherManager = launcherManager;
		this.Sys = Sys;
		this.fileSystem = fileSystem;
		this.inputSystem = inputSystem;
		this.surface = surface;
		this.eng = eng;
		this.services = services;
		this.Key = Key;
		this.clientDLL = clientDLL;
	}
	GameMessageHandler[] GameMessageHandlers;

	void AppActivate(bool active) {
		if (IsActiveApp() == active)
			return;

		SetCanPostActivateEvents(false);

#if !SWDS
		if (Host.Initialized) {
			ClearIOStates();
			if (!active)
				services.GetRequiredService<IBaseClientDLL>()?.IN_DeactivateMouse();
		}
#endif
		SetActiveApp(active);
		SetCanPostActivateEvents(true);
	}

	private void ClearIOStates() => clientDLL?.IN_ClearStates();
	private void SetCanPostActivateEvents(bool v) {

	}

	public void HandleMsg_ActivateApp(in InputEvent ev) {
		AppActivate(ev.Data != 0);
	}
	public void HandleMsg_WindowMove(in InputEvent ev) { }
	public void HandleMsg_Close(in InputEvent ev) {
		if (eng.GetState() == IEngine.State.Active)
			eng.SetQuitting(IEngine.Quit.ToDesktop);
	}

	public bool CreateGameWindow(int width, int height, bool windowed) {
		GameMessageHandlers = [
			new(InputEventType.App_AppActivated, HandleMsg_ActivateApp),
			new(InputEventType.App_WindowMove, HandleMsg_WindowMove),
			new(InputEventType.App_Close, HandleMsg_Close),
			new(InputEventType.System_Quit, HandleMsg_Close),
		];

		if (launcherManager == null) {
			Sys.Error("Tried to call Game.CreateGameWindow without a valid ILauncherManager implementation.");
			return false;
		}

		string windowName = "HALF-LIFE 2";
		{
			KeyValues modinfo = new();
			if (modinfo.LoadFromFile(fileSystem, "gameinfo.txt")) {
				string name = new string(modinfo.GetString("game"));
				if (!string.IsNullOrEmpty(name))
					windowName = name;
			}
		}

		Console.Title = windowName;
		if (!launcherManager.CreateGameWindow(windowName, windowed, width, height))
			return false;

		SetGameWindow(launcherManager.GetWindow());
		AttachToWindow();

		return true;
	}

	private void AttachToWindow() {
		inputSystem.AttachToWindow(window);
		inputSystem.EnableInput(true);
		inputSystem.EnableMessagePump(false);

		surface.AttachToWindow(window, true);
		surface.EnableWindowsMessages(true);
	}

	IWindow window;

	public void DestroyGameWindow() {
		throw new NotImplementedException();
	}

	public void DispatchAllStoredGameMessages() {
		foreach (var ev in inputSystem.GetEventData()) {
			DispatchInputEvent(in ev);
		}
	}

	IEngineVGui? engineVGui;
	[MemberNotNull(nameof(engineVGui))]
	public void FetchEngineVGui() => engineVGui ??= services.GetRequiredService<IEngineVGui>();

	private void DispatchInputEvent(in InputEvent ev) {
		switch (ev.Type) {
			case InputEventType.IE_ButtonPressed:
			case InputEventType.IE_ButtonDoubleClicked:
			case InputEventType.IE_ButtonReleased:
				Key.Event(in ev);
				break;
			default:
				if (surface?.HandleInputEvent(in ev) ?? false)
					break;

				foreach (GameMessageHandler h in GameMessageHandlers) {
					if (h.EventType == ev.Type) {
						h.Function(in ev);
						break;
					}
				}

				break;
		}
	}

	int DesktopWidth;
	int DesktopHeight;
	int DesktopRefreshRate;
	public void GetDesktopInfo(out int width, out int height, out int refreshRate) {
		// order of initialization means that this might get called early.  In that case go ahead and grab the current
		// screen window and setup based on that.
		// we need to do this when initializing the base list of video modes, for example
		if (DesktopWidth == 0) {
			IntPtr dc = GetDC(IntPtr.Zero);
			width = GetDeviceCaps(dc, 8); // HORZRES
			height = GetDeviceCaps(dc, 10); // VERTRES
			refreshRate = GetDeviceCaps(dc, 116); // VREFRESH
			return;
		}

		width = DesktopWidth;
		height = DesktopHeight;
		refreshRate = DesktopRefreshRate;
	}

	// FIXME: Probably shouldn't be here
	[DllImport("user32.dll")]
	static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("gdi32.dll")]
	static extern int GetDeviceCaps(IntPtr hdc, int index);

	void UpdateDesktopInformation() {
		IntPtr dc = GetDC(window.GetHandle());
		DesktopWidth = GetDeviceCaps(dc, 8); // HORZRES
		DesktopHeight = GetDeviceCaps(dc, 10); // VERTRES
		DesktopRefreshRate = GetDeviceCaps(dc, 116); // VREFRESH
	}

	public IWindow GetMainDeviceWindow() {
		return window;
	}

	public nint GetMainWindow() {
		throw new NotImplementedException();
	}

	public nint GetMainWindowAddress() {
		throw new NotImplementedException();
	}

	public void GetWindowRect(out int x, out int y, out int w, out int h) {
		throw new NotImplementedException();
	}
	bool ExternallySuppliedWindow = false;
	public bool InputAttachToGameWindow() {
		if (!ExternallySuppliedWindow)
			return true;

		AttachToWindow();
		return true;
	}

	public void InputDetachFromGameWindow() {
		throw new NotImplementedException();
	}

	bool ActiveApp;

	public bool IsActiveApp() {
		return ActiveApp;
	}

	public void PlayStartupVideos() {
		throw new NotImplementedException();
	}

	public void SetGameWindow(IWindow window) {
		this.window = window;

		if (DesktopWidth == 0 || DesktopHeight == 0)
			UpdateDesktopInformation();
	}

	public void SetWindowSize(int w, int h) {

	}
	public void SetActiveApp(bool active) {
		ActiveApp = active;
	}
	public void SetWindowXY(int x, int y) {

	}
}
