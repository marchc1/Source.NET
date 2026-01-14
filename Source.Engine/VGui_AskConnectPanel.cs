using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

class AskConnectPanel : EditablePanel
{
	string? HostName;
	Color BgColor;
	int OriginalWidth;
	TimeUnit_t AnimationEndTime;
	Label InfoLabel;
	Label HostNameLabel;
	int HostNameLabelRightSidePadding;
	Label AcceptLabel;
	AnimationController AnimationController;

	static AskConnectPanel? Instance;

	public AskConnectPanel(Panel parent) : base(parent, "AskConnectPanel") {
		BgColor = new Color(0, 0, 0, 198);

		SetParent(parent);
		Assert(Instance == null);
		Instance = this;
		AnimationEndTime = -1;

		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		SetVisible(false);

		HostNameLabel = new(this, "HostNameLabel", "");
		AcceptLabel = new(this, "AcceptLabel", "");
		InfoLabel = new(this, "InfoLabel", "");

		VGui.AddTickSignal(this);
		SetAutoDelete(true);

		AnimationController = new AnimationController(this);
		AnimationController.SetParent(this);
		AnimationController.SetScriptFile(parent, "scripts/plugin_animations.txt");
		AnimationController.SetProportional(true);

		LoadControlSettings("resource/AskConnectPanel.res");
		InvalidateLayout(true);

		OriginalWidth = GetWide();
		HostNameLabel.GetBounds(out int x, out _, out int wide, out _);
		HostNameLabelRightSidePadding = OriginalWidth - (x + wide);
	}

	void GetHostName(out ReadOnlySpan<char> hostName) => hostName = HostName;

	public void SetHostName(ReadOnlySpan<char> hostName) {
		HostName = new(hostName);
		HostNameLabel.SetText(hostName);

		HostNameLabel.SizeToContents();
		HostNameLabel.GetBounds(out int x, out int y, out int wide, out int tall);

		Span<char> message = stackalloc char[512];
		Localize.ConstructString(message, Localize.Find("#Valve_ServerOfferingToConnect"), hostName);
		InfoLabel.SetText(message);
		InfoLabel.SizeToContents();
		InfoLabel.GetBounds(out int x2, out int y2, out int wide2, out int tall2);

		int desiredWide = Math.Max(x + wide, x2 + wide2) + HostNameLabelRightSidePadding;
		if (desiredWide < OriginalWidth)
			desiredWide = OriginalWidth;

		SetWide(desiredWide);
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> bgColor = resourceData.GetString("bgcolor", null);
		if (!bgColor.IsEmpty) {
			var scanf = new ScanF(bgColor, "%d %d %d %d")
				.Read(out int r)
				.Read(out int g)
				.Read(out int b)
				.Read(out int a)
				.ReadArguments;

			if (scanf == 4) {
				BgColor = new(r, g, b, a);
				SetBgColor(BgColor);
			}
		}
	}

	private void StartSlideAnimation(float duration) {
		AnimationEndTime = clientGlobalVariables.CurTime + duration;

		if (false) {
			// Key NameForBinding todo
		}
		else
			AcceptLabel.SetText("#Valve_BindKeyToAccept");

		AnimationController.StartAnimationSequence("AskConnectShow");
		SetVisible(true);
		InvalidateLayout(true);
		UpdateCurrentPosition();
	}

	private void Hide() {
		AnimationEndTime = -1;
		SetVisible(false);
	}

	public override void OnTick() {
		if (AnimationEndTime != -1 && clientGlobalVariables.CurTime >= AnimationEndTime) {
			AnimationEndTime = -1;
			AnimationController.StartAnimationSequence("AskConnectHide");
		}

		AnimationController.UpdateAnimations(Sys.Time);

		if (GetAlpha() == 0)
			SetVisible(false);

		if (IsVisible())
			UpdateCurrentPosition();

		base.OnTick();
	}

	private void UpdateCurrentPosition() {
		int x = 0, y = 0, h = 0;
		// if (PluginManager != null)
		// 	PluginManager.GetHudMessagePosition(out x, out y, out _, out h);

		SetPos(x, y + h);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetBgColor(BgColor);
		SetPaintBackgroundType(PaintBackgroundType.Box);
	}

	public static void ShowAskConnectPanel(ReadOnlySpan<char> hostName, float duration) {
		int hostNameLength = hostName.Length;
		if (hostNameLength <= 0)
			return;

		// Hostname is not allowed to contain semicolon, whitespace, or control characters
		for (int i = 0; i < hostNameLength; i++) {
			char c = hostName[i];
			if (c == ';' || char.IsWhiteSpace(c) || char.IsControl(c))
				return;
		}

		if (Instance == null)
			return;

		Instance.SetHostName(hostName);
		Instance.StartSlideAnimation(duration);
		Instance.MoveToFront();
	}

	public static void HideAskConnectPanel() => Instance?.Hide();

	public static bool IsAskConnectPanelActive(ref ReadOnlySpan<char> hostName) {
		if (Instance != null && Instance.IsVisible() && Instance.GetAlpha() > 0) {
			Instance.GetHostName(out hostName);
			return true;
		}
		else
			return false;
	}
}