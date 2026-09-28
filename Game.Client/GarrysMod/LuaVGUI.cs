using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public static class LuaVGUI
{
	public static Panel? CreateControl(ReadOnlySpan<char> className) {
		if (stricmp(className, "Awesomium") == 0 || stricmp(className, "Chromium") == 0)
			className = "HTML";

		if (stricmp(className, "Frame") == 0) {
			Frame frame = new(null, "Frame", true, true);
			frame.SetBuildModeEditable(true);
			return frame;
		}

		if (stricmp(className, "EditablePanel") == 0)
			return new LuaEditablePanel(null, "EditablePanel");

		if (stricmp(className, "Panel") == 0 || stricmp(className, "Divider") == 0)
			return new Panel(null, "Panel");

		if (stricmp(className, "EditablePanel") == 0)
			return new EditablePanel(null, null);

		if (stricmp(className, "Label") == 0)
			return new Label(null, null, "Label");

		if (stricmp(className, "URLLabel") == 0)
			return new URLLabel(null, null, "URLLabel", null);

		if (stricmp(className, "Button") == 0)
			return new Button(null, null, "Button");

		if (stricmp(className, "TextEntry") == 0) {
			TextEntry textEntry = new(null, null);
			textEntry.SendNewLine(true);
			return textEntry;
		}

		if (stricmp(className, "RichText") == 0)
			return new RichText(null, "RichText");

		if (stricmp(className, "TGAImage") == 0)
			return null; // TGAImagePanel

		if (stricmp(className, "AchievementIcon") == 0)
			return null; // AchievementIcon

		if (stricmp(className, "ModelImage") == 0)
			return null; // ModelImage

		if (stricmp(className, "AvatarImage") == 0)
			return null; // AvatarImage

		if (stricmp(className, "HTML") == 0)
			return null; // GarrysMod::HtmlPanel

		return null;
	}

	public static Panel? Create(ReadOnlySpan<char> className, Panel? parent, ReadOnlySpan<char> name) {
		Panel? panel = CreateControl(className);
		if (panel == null) {
			// lua error "vgui.Create failed to create the VGUI component (%s)"
			return null;
		}

		panel.LuaPanel = true;
		panel.SetAutoDelete(true);

		if (parent != null)
			panel.SetParent(parent);
		else
			panel.SetParent(GModBase.GetGModBasePanel(true));

		if (!name.IsEmpty)
			panel.SetName(name);

		// todo: push lua panel object
		panel.InvalidateLayout();
		return panel;
	}
}
