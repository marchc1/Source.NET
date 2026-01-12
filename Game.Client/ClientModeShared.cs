using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.Client;

public enum GameActionSet
{
	None = -1,
	MenuControls,
	FPSControls,
	InGameHUD,
	Spectator
}

public class ClientModeShared : GameEventListener, IClientMode
{
	public void Init() {
		ChatElement = (BaseHudChat?)gHUD.FindElement("CHudChat");
		Assert(ChatElement != null);

		WeaponSelection = (BaseHudWeaponSelection?)gHUD.FindElement("CHudWeaponSelection");
		Assert(WeaponSelection != null);

		ListenForGameEvent("player_connect_client");
		ListenForGameEvent("player_disconnect");
		ListenForGameEvent("player_team");
		ListenForGameEvent("server_cvar");
		ListenForGameEvent("player_changename");
		ListenForGameEvent("teamplay_broadcast_audio");
		ListenForGameEvent("achievement_earned");
	}

	public bool IsTyping() => ChatElement!.GetMessageMode() != MessageModeType.None;

	public void Enable() {
		IPanel? root = enginevgui.GetPanel(VGuiPanelType.ClientDll);

		if (root != null)
			Viewport.SetParent(root);

		Viewport.SetProportional(true);
		Viewport.SetCursor(CursorCode.None);
		surface.SetCursor(CursorCode.None);

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
		// Weapon input

		if (HudElementKeyInput(down, keynum, currentBinding) == 0)
			return 0;

		return 1;
	}

	private int HudElementKeyInput(int down, ButtonCode keynum, ReadOnlySpan<char> currentBinding) {
		if (WeaponSelection != null) {
			if (WeaponSelection.KeyInput(down, keynum, currentBinding) == 0)
				return 0;
		}

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

	protected BaseViewport Viewport = null!;

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
		BuildGroup.ClearResFileCache();

		Viewport.ReloadScheme("resource/ClientScheme.res");
	}

	public AnimationController? GetViewportAnimationController() => Viewport.GetAnimationController();

	bool PlayerNameNotSetYet(ReadOnlySpan<char> name) {
		if (!name.IsEmpty) {
			if (strieq(name, "unnamed"))
				return true;
			if (strieq(name, "NULLNAME"))
				return true;
		}
		return false;
	}

	public static C_BasePlayer? USERID2PLAYER(int i) => ToBasePlayer(cl_entitylist.GetEnt(engine.GetPlayerForUserID(i)));

	public override void FireGameEvent(IGameEvent ev) {
		BaseHudChat? hudChat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		ReadOnlySpan<char> eventname = ev.GetName();

		switch (eventname) {
			case "player_connect_client": {
					if (hudChat == null)
						return;

					if (PlayerNameNotSetYet(ev.GetString("name")))
						return;

					if (!IsInCommentaryMode()) {
						Span<char> localized = stackalloc char[100];
						ReadOnlySpan<char> playerName = localize.TryFind(ev.GetString("name")).SliceNullTerminatedString();
						ReadOnlySpan<char> joined = localize.Find("#game_player_joined_game");

						int written = playerName.ClampedCopyTo(localized);
						written += " ".ClampedCopyTo(localized[written..]);
						written += joined.ClampedCopyTo(localized[written..]);

						hudChat.Printf(ChatFilters.JoinLeave, localized);
					}
				}
				break;
			case "player_disconnect": {
					C_BasePlayer? player = USERID2PLAYER(ev.GetInt("userid"));
					if (hudChat == null || player == null)
						return;
					if (PlayerNameNotSetYet(ev.GetString("name")))
						return;

					if (!IsInCommentaryMode()) {
						Span<char> localized = stackalloc char[100];
						ReadOnlySpan<char> playerName = player.GetPlayerName();
						ReadOnlySpan<char> reason = localize.TryFind(ev.GetString("reason")).SliceNullTerminatedString();

						localize.ConstructString(localized, localize.Find("#game_player_left_game"), playerName, reason);

						hudChat.Printf(ChatFilters.JoinLeave, localized);
					}
				}
				break;
			case "player_team": {

				}
				break;
			case "player_changename": {

				}
				break;
			case "teamplay_broadcast_audio": {

				}
				break;
			case "server_cvar": {
					if (!IsInCommentaryMode()) {
						ReadOnlySpan<char> cvarName = localize.TryFind(ev.GetString("cvarname"));
						ReadOnlySpan<char> cvarValue = localize.TryFind(ev.GetString("cvarvalue"));
						Span<char> localized = stackalloc char[256];
						localize.ConstructString(localized, localize.Find("#game_server_cvar_changed"), cvarName, cvarValue);

						hudChat?.Printf(ChatFilters.ServerMsg, localized);
					}
				}
				break;
			case "achievement_earned": {

				}
				break;
			default:
				DevMsg(2, $"Unhandled GameEvent in ClientModeShared.FireGameEvent - {ev.GetName()}\n");
				break;
		}
	}


	public void LevelInit(ReadOnlySpan<char> newmap) {
		Viewport.GetAnimationController().StartAnimationSequence("LevelInit");
	}

	public void ProcessInput(bool active) {
		gHUD.ProcessInput(active);
	}

	InlineArray2<int> RootSize;

	public BaseHudChat? ChatElement;
	public BaseHudWeaponSelection? WeaponSelection;
}
