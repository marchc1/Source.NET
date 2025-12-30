using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

public class FocusNavGroup
{
	readonly public IVGui VGui = Singleton<IVGui>();
	readonly public ISurface Surface = Singleton<ISurface>();
	readonly WeakReference<Panel?> DefaultButton = new(null);
	readonly WeakReference<Panel?> CurrentDefaultButton = new(null);
	readonly WeakReference<Panel?> CurrentFocus = new(null);
	readonly Panel MainPanel;
	bool TopLevelFocus;

	public FocusNavGroup(Panel panel) {
		MainPanel = panel;
		TopLevelFocus = false;
	}

	public bool RequestFocusPrev(Panel? panel) {
		if (panel == null)
			return false;

		CurrentFocus.SetTarget(null);

		int newPosition = panel.GetTabPosition();

		bool found = false;
		bool repeat = true;
		Panel? best = null;

		while (true) {
			newPosition--;

			if (newPosition > 0) {
				int bestPosition = 0;

				for (int i = 0; i < MainPanel.GetChildCount(); i++) {
					Panel? child = MainPanel.GetChild(i);
					if (child != null && child.IsVisible() && child.IsEnabled() && child.GetTabPosition() != 0) {
						int tabPosition = child.GetTabPosition();
						if (tabPosition == newPosition) {
							best = child;
							bestPosition = newPosition;
							break;
						}
						else if (tabPosition < newPosition && tabPosition > bestPosition) {
							best = child;
							bestPosition = tabPosition;
						}
					}
				}

				if (!repeat)
					break;

				if (best != null)
					break;
			}
			else
				newPosition = 9999999;

			if (!TopLevelFocus) {
				if (MainPanel.GetParent() != null && MainPanel.GetParent() != Surface.GetEmbeddedPanel()) {
					if (MainPanel.GetParent()!.RequestFocusPrev(MainPanel)) {
						found = true;
						SetCurrentDefaultButton(null);
						break;
					}
				}
			}

			newPosition = 9999999;
			repeat = false;
		}

		if (best != null) {
			CurrentFocus.SetTarget(best);
			best.RequestFocus(-1);

			if (!CanButtonBeDefault(best)) {
				if (DefaultButton.TryGetTarget(out Panel? r))
					SetCurrentDefaultButton(r);
				else {
					SetCurrentDefaultButton(null);

					if (MainPanel != null)
						VGui.PostMessage(MainPanel, new KeyValues("FindDefaultButton"), null);
				}
			}
			else
				SetCurrentDefaultButton(best);
		}

		return found;
	}

	static readonly KeyValues KV_FindDefaultButton = new("FindDefaultButton");
	static int stack_depth = 0; // basic recursion guard, in case user has set up a bad focus hierarchy
	public bool RequestFocusNext(Panel? panel) {
		stack_depth++;
		CurrentFocus.SetTarget(null);

		int newPosition = 0;
		if (panel != null)
			newPosition = panel.GetTabPosition();

		bool found = false;
		bool repeat = true;
		Panel? best = null;

		while (true) {
			newPosition++;
			int bestPosition = 999999;

			for (int i = 0; i < MainPanel.GetChildCount(); i++) {
				Panel? child = MainPanel.GetChild(i);
				if (child != null && child.IsVisible() && child.IsEnabled() && child.GetTabPosition() != 0) {
					int tabPosition = child.GetTabPosition();
					if (tabPosition == newPosition) {
						best = child;
						bestPosition = newPosition;

						break;
					}
					else if (tabPosition > newPosition && tabPosition < bestPosition) {
						bestPosition = tabPosition;
						best = child;
					}
				}
			}

			if (!repeat)
				break;

			if (best != null)
				break;


			if (!TopLevelFocus) {
				if (MainPanel.GetParent() != null && MainPanel.GetParent() != Surface.GetEmbeddedPanel()) {
					if (stack_depth < 15) {
						if (MainPanel.GetParent()!.RequestFocusNext(MainPanel)) {
							found = true;
							SetCurrentDefaultButton(null);
							break;
						}
					}
				}
			}

			newPosition = 0;
			repeat = false;
		}

		if (best != null) {
			CurrentFocus.SetTarget(best);
			best.RequestFocus(1);
			found = true;

			if (!CanButtonBeDefault(best)) {
				if (DefaultButton.TryGetTarget(out Panel? r))
					SetCurrentDefaultButton(r);
				else {
					SetCurrentDefaultButton(null);

					if (MainPanel != null)
						VGui.PostMessage(MainPanel, KV_FindDefaultButton, null);
				}
			}
			else
				SetCurrentDefaultButton(best);
		}

		stack_depth--;
		return found;
	}

	public void SetFocusTopLevel(bool state) => TopLevelFocus = state;

	public void SetDefaultButton(Panel panel) {
		if ((DefaultButton.TryGetTarget(out Panel? d) && d == panel) || panel == null)
			return;
		DefaultButton.SetTarget(panel);
		SetCurrentDefaultButton(panel);
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

	public Panel? GetCurrentDefaultButton() => CurrentDefaultButton.TryGetTarget(out Panel? t) ? t : null;
	public Panel? GetDefaultButton() => DefaultButton.TryGetTarget(out Panel? t) ? t : null;

	public void FindPanelByHotKey() {
		// todo hotkeys
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

	public Panel? GetCurrentFocus() => CurrentFocus.TryGetTarget(out Panel? t) ? t : null;
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
			result = data.GetInt("result") == 1;

		return result;
	}
}