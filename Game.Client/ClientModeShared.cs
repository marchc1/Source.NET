using Game.Client.HUD;
using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.Client;

public enum GameActionSet {
	None = -1,
	MenuControls,
	FPSControls,
	InGameHUD,
	Spectator
}

public class ClientModeShared(IServiceProvider services, ClientGlobalVariables gpGlobals, Hud Hud, IEngineVGui enginevgui, ISurface Surface) : IClientMode
{
	IEngineClient engine;
	public void Init() {
		engine = services.GetRequiredService<IEngineClient>();
		ChatElement = (BaseHudChat?)Hud.FindElement("CHudChat");
		Assert(ChatElement != null);
	}

	public bool IsTyping() => ChatElement!.GetMessageMode() != MessageModeType.None;

	public void Enable() {
		IPanel? root = enginevgui.GetPanel(VGuiPanelType.ClientDll);

		if (root != null) 
			Viewport.SetParent(root);

		Viewport.SetProportional(true);
		Viewport.SetCursor(CursorCode.None);
		Surface.SetCursor(CursorCode.None);

		Viewport.SetVisible(true);
		if (Viewport.IsKeyboardInputEnabled())
			Viewport.RequestFocus();

		Layout();
	}

	public bool CreateMove(TimeUnit_t inputSampleTime, ref UserCmd cmd) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return true;

		return player.CreateMove(inputSampleTime, ref cmd);
	}
	public virtual int KeyInput(int down, ButtonCode keynum, ReadOnlySpan<char> currentBinding) {
		if (engine.Con_IsVisible())
			return 1;

		if (!currentBinding.IsEmpty && currentBinding.Equals("messagemode", StringComparison.Ordinal) || currentBinding.Equals("say", StringComparison.Ordinal)) {
			if (down != 0)
				StartMessageMode(MessageModeType.Say);

			return 0;
		}
		else if (!currentBinding.IsEmpty && currentBinding.Equals("messagemode2", StringComparison.Ordinal) || currentBinding.Equals("say_team", StringComparison.Ordinal)) {
			if (down != 0)
				StartMessageMode(MessageModeType.SayTeam);

			return 0;
		}

		// In-game spectator
		// Hud element key input
		// Weapon input

		return 1;
	}

	public void StartMessageMode(MessageModeType messageModeType) {
		if (gpGlobals.MaxClients == 1)
			return;

		ChatElement?.StartMessageMode(messageModeType);
	}
	public void StopMessageMode() {
		ChatElement?.StopMessageMode();
		
	}

	public void OverrideMouseInput(ref float mouse_x, ref float mouse_y) {
		// nothing yet
	}

	protected BaseViewport Viewport;

	public Panel GetViewport() {
		return Viewport;
	}

	public float GetViewModelFOV() {
		return v_viewmodel_fov.GetFloat();
	}

	public void Layout() {
		IPanel? root = enginevgui.GetPanel(VGuiPanelType.ClientDll);

		if (root != null) {
			root.GetSize(out int wide, out int tall);

			bool changed = wide != RootSize[0] || tall != RootSize[1];
			RootSize[0] = wide;
			RootSize[1] = tall;

			Viewport.SetBounds(0, 0, wide, tall);
			if (changed) 
				ReloadScheme(false);
		}
	}

	private void ReloadScheme(bool v) {
		//BuildGroup.ClearResFileCache(); << needs to be done later. Also the internals for buildgroup cache data is not static!!! So do that too!!!!!!!!

		Viewport.ReloadScheme("resource/ClientScheme.res");
	}

	public AnimationController? GetViewportAnimationController() => Viewport.GetAnimationController();

	InlineArray2<int> RootSize;

	public BaseHudChat? ChatElement;
}
