using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class EditablePanel : Panel
{
	public static Panel Create_EditablePanel() => new EditablePanel(null, null);
	readonly public IFileSystem fileSystem = Singleton<IFileSystem>();

	public EditablePanel(Panel? parent, string? panelName, bool showTaskbarIcon = true) : base(parent, panelName, showTaskbarIcon) {
		BuildGroup = new BuildGroup(this, this);
		NavGroup = new FocusNavGroup(this);

		SetBuildGroup(GetBuildGroup());
	}

	public virtual BuildGroup? GetBuildGroup() {
		return BuildGroup;
	}

	public override bool RequestInfo(KeyValues outputData) {
		if (outputData.Name.Equals("BuildDialog", StringComparison.OrdinalIgnoreCase)) {
			// todo
			return false;
		}
		else if (outputData.Name.Equals("ControlFactory")) {
			Panel? newPanel = CreateControlByName(outputData.GetString("ControlName"));
			if (newPanel != null) {
				outputData.SetPtr("PanelPtr", newPanel);
				return true;
			}
		}

		return base.RequestInfo(outputData);
	}

	protected virtual Panel? CreateControlByName(ReadOnlySpan<char> controlName) => InstancePanel(controlName);

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);
		BuildGroup.ApplySettings(resourceData);
	}

	public void ActivateBuildMode() {
		// BuildGroup.SetEnabled(true);
	}

	public virtual void LoadControlSettings(ReadOnlySpan<char> resourceName, ReadOnlySpan<char> pathID = default, KeyValues? keyValues = null, KeyValues? conditions = null) {
		if (!fileSystem.FileExists(resourceName))
			Msg($"Resource file \"{resourceName}\" not found on disk!\n");

		BuildGroup.LoadControlSettings(resourceName, pathID, keyValues, conditions);
		ForceSubPanelsToUpdateWithNewDialogVariables();
		InvalidateLayout();
	}

	private void ForceSubPanelsToUpdateWithNewDialogVariables() {
		// TODO
	}

	static ConVar vgui_nav_lock_default_button = new(nameof(vgui_nav_lock_default_button), 0);
	public override void OnKeyCodePressed(ButtonCode code) {
		if (vgui_nav_lock_default_button.GetInt() == 0) {
			ButtonCode nButtonCode = code.GetBaseButtonCode();

			IPanel? panel = GetFocusNavGroup().GetCurrentDefaultButton();
			if (panel != null && !IsConsoleStylePanel()) {
				switch (nButtonCode) {
					case ButtonCode.KeyEnter:
						if (panel.IsVisible() && panel.IsEnabled()) {
							PostMessage(panel, new KeyValues("Hotkey"));
							return;
						}
						break;
				}
			}
		}

		if (!PassUnhandledInput)
			return;

		base.OnKeyCodePressed(code);
	}
	public override void OnChildAdded(IPanel child) {
		base.OnChildAdded(child);

		Panel? panel = (Panel?)child;
		if (panel != null) {
			panel.SetBuildGroup(BuildGroup);
			panel.AddActionSignalTarget(this);
		}
	}
	public override IPanel? GetCurrentKeyFocus() {
		Panel? focus = NavGroup.GetCurrentFocus();
		if (focus == this)
			return null;

		if (focus != null) {
			if (focus.IsPopup())
				return base.GetCurrentKeyFocus();

			IPanel? subFocus = focus.GetCurrentKeyFocus();
			if (subFocus != null)
				return subFocus;

			return focus;
		}

		return base.GetCurrentKeyFocus();
	}
	public override void OnRequestFocus(Panel subFocus, Panel? defaultPanel) {
		if (!subFocus.IsPopup())
			defaultPanel = NavGroup.SetCurrentFocus(subFocus, defaultPanel);

		base.OnRequestFocus(subFocus, defaultPanel);
	}

	public FocusNavGroup GetFocusNavGroup() => NavGroup;
	BuildGroup BuildGroup;
	readonly FocusNavGroup NavGroup;
}
public class FocusNavGroup
{
	readonly public IVGui VGui = Singleton<IVGui>();
	readonly WeakReference<Panel?> DefaultButton = new(null);
	readonly WeakReference<Panel?> CurrentDefaultButton = new(null);
	readonly WeakReference<Panel?> CurrentFocus = new(null);
	readonly Panel MainPanel;

	bool TopLevelFocus;

	public FocusNavGroup(Panel panel) {
		MainPanel = panel;
	}

	public Panel? SetCurrentFocus(Panel focus, Panel? defaultPanel) {
		CurrentFocus.SetTarget(focus);
		if (defaultPanel == null) {
			if (CanButtonBeDefault(focus))
				defaultPanel = focus;
			else if (DefaultButton.TryGetTarget(out Panel? def))
				defaultPanel = def;
		}

		SetCurrentDefaultButton(defaultPanel);
		return defaultPanel;
	}

	public bool CanButtonBeDefault(Panel panel) {
		if (panel == null)
			return false;

		KeyValues data = new("CanBeDefaultButton");
		bool result = false;
		if (panel.RequestInfo(data))
			result = (data.GetInt("result") == 1);

		return result;
	}

	public void SetCurrentDefaultButton(Panel? panel, bool sendCurrentDefaultButtonMessage = true) {
		CurrentDefaultButton.TryGetTarget(out Panel? currentDefaultButton);

		if (panel == currentDefaultButton)
			return;

		if (sendCurrentDefaultButtonMessage && currentDefaultButton != null)
			VGui.PostMessage(currentDefaultButton, new KeyValues("SetAsCurrentDefaultButton", "state", 0), null);

		CurrentDefaultButton.SetTarget(panel);

		if (sendCurrentDefaultButtonMessage && currentDefaultButton != null)
			VGui.PostMessage(currentDefaultButton, new KeyValues("SetAsCurrentDefaultButton", "state", 1), null);
	}

	public Panel? GetCurrentFocus() => CurrentFocus.TryGetTarget(out Panel? t) ? t : null;

	public void SetDefaultButton(Panel? submit) {
		if ((DefaultButton.TryGetTarget(out Panel? d) && d == submit) || submit == null)
			return;
		DefaultButton.SetTarget(submit);
		SetCurrentDefaultButton(submit);
	}

	public IPanel? GetCurrentDefaultButton() {
		if (CurrentDefaultButton.TryGetTarget(out Panel? t))
			return t;
		return null;
	}

	public void SetFocusTopLevel(bool state) {
		TopLevelFocus = state;
	}

	public Panel? GetDefaultPanel() {
		for (int i = 0; i < MainPanel.GetChildCount(); i++) {
			Panel? child = MainPanel.GetChild(i);
			if (child == null)
				continue;

			if (child.GetTabPosition() == 1) 
				return child;
		}

		return null; 
	}
}
