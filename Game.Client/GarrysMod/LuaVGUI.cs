using Source.Common.GarrysMod.Lua;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public static class LuaVGUI
{
	public static readonly LuaClass PanelClass = new("Panel", LuaType.Panel, null, null);

	static readonly LuaLibrary vgui = new("vgui");

	static LuaVGUI() {
		vgui.Add(new() { Name = "Create", Function = Create });
		// vgui.Add(new() { Name = "GetAll", Function = GetAll });
		// vgui.Add(new() { Name = "CursorVisible", Function = CursorVisible });
		// vgui.Add(new() { Name = "IsHoveringWorld", Function = IsHoveringWorld });
		// vgui.Add(new() { Name = "GetWorldPanel", Function = GetWorldPanel });
		// vgui.Add(new() { Name = "FocusedHasParent", Function = FocusedHasParent });
		// vgui.Add(new() { Name = "GetKeyboardFocus", Function = GetKeyboardFocus });
		// vgui.Add(new() { Name = "GetHoveredPanel", Function = GetHoveredPanel });
	}

	public static ILuaObject? GetLuaTable(Panel panel) {
		ILuaObject? table = panel.LuaTable;
		if (table == null && g_Lua != null) {
			if (panel.LuaObject != null && panel.LuaObject.GetType() != LuaType.Panel) {
				panel.LuaObject.UnReference();
				panel.LuaObject = null;
			}

			LuaObject newTable = new();
			newTable.Set(g_Lua.GetNewTable());
			panel.LuaTable = newTable;
			Push_Panel(panel);
			panel.LuaTable.SetMember("Panel", g_Lua.GetObject(-1));
			return panel.LuaTable;
		}
		return table;
	}

	public static void PushToLua(Panel panel, LuaClass luaClass) {
		if (panel.LuaObject != null && panel.LuaObject.isNil()) {
			Warning("Panel object is fucked - might be using an older Lua interface.. why wasn't it cleared??\n");
			ClearLuaReferences(panel);
		}

		if (panel.LuaObject != null) {
			if (panel.LuaObject.GetType() == LuaType.Panel) {
				panel.LuaObject.Push();
				return;
			}
			Warning("NOT A PANEL!!!\n");
		}

		if (panel.LuaHandle)
			g_Lua!.ReleaseUserTypeObject(panel);
		panel.LuaHandle = true;
		luaClass.Push(panel);
		panel.LuaObject = new LuaObject(-1, LuaType.None);
	}

	public static void ClearLuaReferences(Panel panel) {
		panel.LuaThink?.UnReference();
		panel.LuaThink = null;
		panel.LuaPaint?.UnReference();
		panel.LuaPaint = null;
		panel.LuaPaintOver?.UnReference();
		panel.LuaPaintOver = null;
		panel.LuaAnimationThink?.UnReference();
		panel.LuaAnimationThink = null;
		panel.LuaOnChildRemoved?.UnReference();
		panel.LuaOnChildRemoved = null;
		panel.LuaOnChildAdded?.UnReference();
		panel.LuaOnChildAdded = null;
		if (panel.LuaHandle) {
			g_Lua!.ReleaseUserTypeObject(panel);
			panel.LuaHandle = false;
		}
		panel.LuaTable?.UnReference();
		panel.LuaTable = null;
		panel.LuaObject?.UnReference();
		panel.LuaObject = null;
	}

	public static void Push_Panel(Panel? panel) {
		if (panel != null)
			PushToLua(panel, PanelClass);
		else
			g_Lua!.PushNil();
	}

	public static Panel? Get_Panel(int stackPos) {
		return (Panel?)PanelClass.Get(stackPos);
	}

	public static bool IsValidPanel(Panel? panel) {
		if (panel != null && panel != GModBase.GetGModBasePanel(true) /* && panel != g_HudGMod */ && panel != GModBase.GetGModParentToHUDPanel())
			return panel.LuaPanel && !panel.IsMarkedForDeletion();
		return true;
	}

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
			return new HtmlPanel(null, "HtmlPanel");

		return null;
	}

	public static int Create(ILuaInterface lua) {
		string className = lua.CheckString(1);
		Panel? panel = CreateControl(className);
		if (panel == null) {
			lua.ErrorFromLua($"vgui.Create failed to create the VGUI component ({className})");
			return 0;
		}

		panel.LuaPanel = true;
		panel.SetAutoDelete(true);

		if (lua.GetType(2) == LuaType.Panel) {
			Panel? parent = Get_Panel(2);
			if (IsValidPanel(parent))
				panel.SetParent(parent);
		}
		else {
			if (lua.GetType(2) != LuaType.Nil)
				lua.ErrorFromLua($"bad argument #2 to 'Create' (Panel expected, got {lua.GetActualTypeName(2)})");
			panel.SetParent(GModBase.GetGModBasePanel(true));
		}

		if (lua.GetType(3) == LuaType.String)
			panel.SetName(lua.CheckString(3));
		else if (lua.GetType(3) != LuaType.Nil)
			lua.ErrorFromLua($"bad argument #3 to 'Create' (string expected, got {lua.GetActualTypeName(3)})");

		Push_Panel(panel);
		panel.InvalidateLayout(false, false);
		return 1;
	}
}
