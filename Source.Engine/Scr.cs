using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.MaterialSystem;
using Source.Engine.Client;

namespace Source.Engine;

/// <summary>
/// Screen library.
/// </summary>
public class Scr(IEngineAPI engineAPI)
{
	public bool Initialized;
	public bool DisabledForLoading;
	public bool DrawLoading;
	public int NextDrawTick;

	ClientDLL ClientDLL;
	Host Host;
	Sys Sys;
	CL CL;
	IEngineVGuiInternal? EngineVGui;
	Con Con;
	Common Common;
	Render Render;
	View View;
	IHostState HostState;
	IFileSystem FileSystem;
	IBaseClientDLL clientDll;
	ClientState cl;
	ClientGlobalVariables clientGlobalVariables;
	IMaterialSystem materials;
	public void Init() {
		Initialized = true;

		ClientDLL = engineAPI.GetRequiredService<ClientDLL>();
		Host = engineAPI.GetRequiredService<Host>();
		Common = engineAPI.GetRequiredService<Common>();
		View = engineAPI.GetRequiredService<View>();
		Render = engineAPI.GetRequiredService<Render>();
		Sys = engineAPI.GetRequiredService<Sys>();
		EngineVGui = engineAPI.GetRequiredService<IEngineVGuiInternal>();
		Con = engineAPI.GetRequiredService<Con>();
		HostState = engineAPI.GetRequiredService<IHostState>();
		FileSystem = engineAPI.GetRequiredService<IFileSystem>();
		clientDll = engineAPI.GetRequiredService<IBaseClientDLL>();
		CL = engineAPI.GetRequiredService<CL>();
		cl = engineAPI.GetRequiredService<ClientState>();
		materials = engineAPI.GetRequiredService<IMaterialSystem>();
		clientGlobalVariables = engineAPI.GetRequiredService<ClientGlobalVariables>();
	}

	public void BeginLoadingPlaque() {
		if (!DrawLoading) {
			// EngineVGui.SetNotAllowedToShowGameUI(false);
			Host.AllowQueuedMaterialSystem(false);
			DrawLoading = true;

			// todo: sounds

			Con.ClearNotify();
			CenterStringOff();

			EngineVGui!.OnLevelLoadingStarted();

			clientDll?.HudText(null);

			clientGlobalVariables.FrameTime = 0;

			clientGlobalVariables.FrameCount = ++Host.FrameCount;

			// Ensures the screen is painted to reflect the loading state
			UpdateScreen();
			clientGlobalVariables.FrameCount = ++Host.FrameCount;
			UpdateScreen();

			clientGlobalVariables.FrameTime = cl.GetFrameTime();

			DisabledForLoading = true;
		}
	}

	public void EndLoadingPlaque() {
		if (DrawLoading) {
			EngineVGui?.OnLevelLoadingFinished();
		}
		else if (Sys.ExtendedError) {
			EngineVGui?.ShowErrorMessage();
		}

		DisabledForLoading = false;
		DrawLoading = false;
	}

	public void UpdateScreen() {
		if(NextDrawTick != 0) {
			if (Host.TickCount < NextDrawTick)
				return;
			NextDrawTick = 0;
		}

		if (DisabledForLoading) {
			if (!Host.IsSinglePlayerGame()) {
				View.RenderGuiOnly();
			}

			return;
		}

		if (!Initialized || !Common.Initialized)
			return;

		materials.BeginFrame(Host.FrameTime);
		{
			EngineVGui?.Simulate();
		}

		ClientDLL.FrameStageNotify(ClientFrameStage.RenderStart);
		{
			Render.FrameBegin();
		}

		View.RenderView();
		CL.TakeSnapshotAndSwap();

		ClientDLL.FrameStageNotify(ClientFrameStage.RenderEnd);
		{
			Render.FrameEnd();
		}

		materials.EndFrame();
	}

	public void CenterPrint(ReadOnlySpan<char> str) { }
	public void CenterStringOff() { }

	internal void Shutdown() {

	}
}
