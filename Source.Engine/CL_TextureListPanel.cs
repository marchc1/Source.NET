using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.Engine;
using Source.GUI.Controls;

using System.Collections;

class TileViewPanelEx : Panel
{
	enum HitTest
	{
		Nothing = 0,
		Tile
	}

	int NumTiles;
	int NumVisibleTiles;
	int Wide;
	int Tall;
	int WideItem;
	int TallItem;
	int ColVisible;
	int RowVisible;
	int RowNeeded;
	int StartTile;
	int EndTile;

	ScrollBar Hbar;
	IFont Font;

	public TileViewPanelEx(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

class AutoMatSysDebugMode
{
	bool OldDebugMode;
	List<IMaterialVar?> ArrCleanupVars;

	public AutoMatSysDebugMode() {
		ArrCleanupVars = [];

		// materialSystem.Flush();
		OldDebugMode = materialSystemDebugTextureInfo.SetDebugTextureRendering(true);
	}

	public void ScheduleCleanupTextureVar(IMaterialVar? Var) {
		if (Var == null)
			return;

		if (!ArrCleanupVars.Contains(Var))
			ArrCleanupVars.Add(Var);
	}
}

class VmtTextEntry
{

}

class P4Requirement
{

}

class RenderTextureEditor : Frame
{
	public RenderTextureEditor(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

class RenderTexturesListViewPanel : TileViewPanelEx
{
	public const int TileBorder = 20;
	public const int TileSize = 192;
	public const int TileTextureSize = 256;
	public const int TileText = 35;

	public ListPanel? ListPanel;
	RenderTextureEditor RenderTxEditor;
	bool PaintAlpha;
	public RenderTexturesListViewPanel(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {
		ListPanel = null;
		PaintAlpha = false;

		RenderTxEditor = new(this, "TxEdit");
		RenderTxEditor.SetPos(10, 10);
		RenderTxEditor.PerformLayout();
		RenderTxEditor.SetMoveable(true);
		RenderTxEditor.SetSizeable(false);
		RenderTxEditor.SetClipToParent(true);
		RenderTxEditor.SetTitle("", true);
		RenderTxEditor.SetCloseButtonVisible(false);
		RenderTxEditor.SetVisible(false);
	}

	public override void OnMousePressed(ButtonCode code) {
		base.OnMousePressed(code);


	}

	// public int GetNumTiles() => ListPanel != null ? ListPanel.GetItemCount() : 0;

	public void GetTileSize(out int wide, out int tall) {
		wide = 2 * TileBorder + TileSize;
		tall = 2 * TileBorder + TileSize + TileText;
	}

	public void RenderTile(int tile, int x, int y) {

	}

	public void SetDataListPanel(ListPanel panel) {
		ListPanel = panel;
		InvalidateLayout();
	}

	public void SetPaintAlpha(bool alpha) {
		PaintAlpha = alpha;
		Repaint();
	}

	public RenderTextureEditor GetRenderTxEditor() => RenderTxEditor;
}

class TextureListPanel : Frame
{
	public static TextureListPanel? g_TextureListPanel;
	static int SaveQueueState = int.MinValue;
	static int WarnTxListSize = 1499;
	static int WarnTextDimensions = 1024;
	static bool WarnEnable = true;
	static bool CursorSet = false;
	static ConVar mat_texture_list = new("mat_texture_list", "0", FCvar.Cheat, "For debugging, show a list of used textures per frame");
	static ConVar mat_texture_list_all = new("mat_texture_list_all", "0", FCvar.NeverAsString | FCvar.Cheat, "If this is nonzero, then the texture list panel will show all currently-loaded textures.");
	static ConVar mat_texture_list_view = new("mat_texture_list_view", "1", FCvar.NeverAsString | FCvar.Cheat, "If this is nonzero, then the texture list panel will render thumbnails of currently-loaded textures.");
	static ConVar mat_show_texture_memory_usage = new("mat_show_texture_memory_usage", "0", FCvar.NeverAsString | FCvar.Cheat, "Display the texture memory usage on the HUD.");

	const string KeyName_Name = "Name";
	const string KeyName_Path = "Path";
	const string KeyName_Binds_Max = "BindsMax";
	const string KeyName_Binds_Frame = "BindsFrame";
	const string KeyName_Size = "Size";
	const string KeyName_Format = "Format";
	const string KeyName_Width = "Width";
	const string KeyName_Height = "Height";
	const string KeyName_Texture_Group = "TexGroup";

	enum TxListPanelRequest
	{
		TxrNone,
		TxrShow,
		TxrRunning,
		TxrHide
	}
	TxListPanelRequest TxListPanelReq = TxListPanelRequest.TxrNone;

	IFont Font;
	ListPanel ListPanel;
	RenderTexturesListViewPanel ViewPanel;
	CheckButton SpecialTexs;
	CheckButton ResolveTexturePath;
	ConVarCheckbutton ShowTextureMemoryUsageOption;
	ConVarCheckbutton AllTextures;
	ConVarCheckbutton ViewTextures;
	Button CopyToClipboardButton;
	ToggleButton Collapse;
	CheckButton Alpha;
	CheckButton ThumbWarnings;
	CheckButton HideMipped;
	CheckButton FilteringChk;
	TextEntry FilteringText;
	int NumDisplayedSizeKB;
	Button ReloadAllMaterialsButton;
	Button CommitChangesButton;
	Button DiscardChangesButton;
	Label CVarListLabel;
	Label TotalUsageLabel;

	public TextureListPanel(Panel parent) : base(parent, "TextureListPanel") {
		SetSize(((VideoMode_Common)videoMode).GetModeStereoWidth() - 20, ((VideoMode_Common)videoMode).GetModeStereoHeight() - 20);
		SetPos(10, 10);
		SetVisible(true);

		SetTitle("Texture list", false);
		SetMenuButtonVisible(false);

		SetFgColor(new Color(0, 0, 0, 255));
		SetPaintBackgroundEnabled(true);

		CVarListLabel = new Label(this, "CVarListLabel", "cvars: mat_texture_limit, mat_texture_list, mat_picmip, mat_texture_list_txlod, mat_texture_list_txlod_sync");
		CVarListLabel.SetVisible(false); // CVarListLabel.SetVisible(true);

		TotalUsageLabel = new Label(this, "TotalUsageLabel", "");
		TotalUsageLabel.SetVisible(true);

		SpecialTexs = new CheckButton(this, "service", "Render Targets and Special Textures");
		SpecialTexs.SetVisible(true);
		SpecialTexs.AddActionSignalTarget(this);
		SpecialTexs.SetCommand("service");

		ResolveTexturePath = new CheckButton(this, "resolvepath", "Resolve Full Texture Path");
		ResolveTexturePath.SetVisible(true);
		ResolveTexturePath.AddActionSignalTarget(this);
		ResolveTexturePath.SetCommand("resolvepath");

		ShowTextureMemoryUsageOption = new ConVarCheckbutton(this, "ShowTextureMemoryUsageOption", "Show Memory Usage on HUD");
		ShowTextureMemoryUsageOption.SetVisible(true);
		ShowTextureMemoryUsageOption.SetConVar(mat_show_texture_memory_usage);

		AllTextures = new ConVarCheckbutton(this, "AllTextures", "Show ALL textures");
		AllTextures.SetVisible(true);
		AllTextures.SetConVar(mat_texture_list_all);
		AllTextures.AddActionSignalTarget(this);
		AllTextures.SetCommand("AllTextures");

		ViewTextures = new ConVarCheckbutton(this, "ViewTextures", "View textures thumbnails");
		ViewTextures.SetVisible(true);
		ViewTextures.SetConVar(mat_texture_list_view);
		ViewTextures.AddActionSignalTarget(this);
		ViewTextures.SetCommand("ViewThumbnails");

		CopyToClipboardButton = new Button(this, "CopyToClipboard", "Copy to Clipboard");
		CopyToClipboardButton.AddActionSignalTarget(this);
		CopyToClipboardButton.SetCommand("CopyToClipboard");

		Collapse = new ToggleButton(this, "Collapse", " ");
		Collapse.AddActionSignalTarget(this);
		Collapse.SetCommand("Collapse");
		Collapse.SetSelected(true);

		Alpha = new CheckButton(this, "ShowAlpha", "Alpha");
		Alpha.AddActionSignalTarget(this);
		Alpha.SetCommand("ShowAlpha");
		bool DefaultTxAlphaOn = true;
		Alpha.SetSelected(DefaultTxAlphaOn);

		ThumbWarnings = new CheckButton(this, "ThumbWarnings", "Warns");
		ThumbWarnings.AddActionSignalTarget(this);
		ThumbWarnings.SetCommand("ThumbWarnings");
		ThumbWarnings.SetSelected(WarnEnable);

		HideMipped = new CheckButton(this, "HideMipped", "Hide Mipped");
		HideMipped.AddActionSignalTarget(this);
		HideMipped.SetCommand("HideMipped");
		HideMipped.SetSelected(false);

		FilteringChk = new CheckButton(this, "FilteringChk", "Filter: ");
		FilteringChk.AddActionSignalTarget(this);
		FilteringChk.SetCommand("FilteringChk");
		FilteringChk.SetSelected(true);

		FilteringText = new TextEntry(this, "FilteringTxt");
		FilteringText.AddActionSignalTarget(this);

		ReloadAllMaterialsButton = new Button(this, "ReloadAllMaterials", "Reload All Materials");
		ReloadAllMaterialsButton.AddActionSignalTarget(this);
		ReloadAllMaterialsButton.SetCommand("ReloadAllMaterials");

		CommitChangesButton = new Button(this, "CommitChanges", "Commit Changes");
		CommitChangesButton.AddActionSignalTarget(this);
		CommitChangesButton.SetCommand("CommitChanges");

		DiscardChangesButton = new Button(this, "DiscardChanges", "Discard Changes");
		DiscardChangesButton.AddActionSignalTarget(this);
		DiscardChangesButton.SetCommand("DiscardChanges");

		ListPanel = new(this, "List Panel");
		ListPanel.SetVisible(!mat_texture_list_view.GetBool());

		int col = -1;
		ListPanel.AddColumnHeader(++col, KeyName_Name, "Texture Name", 200, 100, 700, (int)ListPanel.ColumnFlags.ResizeWithWindow);
		ListPanel.AddColumnHeader(++col, KeyName_Path, "Path", 50, 50, 300, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Size, "Kilobytes", 50, 50, 50, 0);
		ListPanel.SetSortFunc(col, KilobytesSortFunc);
		ListPanel.SetSortColumnEx(col, 0, true);
		ListPanel.AddColumnHeader(++col, KeyName_Texture_Group, "Group", 100, 100, 300, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Format, "Format", 250, 50, 300, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Width, "Width", 50, 50, 50, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Height, "Height", 50, 50, 50, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Binds_Frame, "# Binds", 50, 50, 50, 0);
		ListPanel.AddColumnHeader(++col, KeyName_Binds_Max, "BindsMax", 50, 50, 50, 0);

		SetBgColor(new Color(0, 0, 0, 100));

		ListPanel.SetBgColor(new Color(0, 0, 0, 100));

		ViewPanel = new RenderTexturesListViewPanel(this, "View Panel");
		ViewPanel.SetVisible(mat_texture_list_view.GetBool());
		ViewPanel.SetBgColor(new Color(0, 0, 0, 255));
		// ViewPanel.SetDragEnabled(false);
		ViewPanel.SetDropEnabled(false);
		ViewPanel.SetPaintAlpha(DefaultTxAlphaOn);
		ViewPanel.SetDataListPanel(ListPanel);
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		g_TextureListPanel = null;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		Font = scheme.GetFont("DefaultVerySmall", false)!;
		Assert(Font != null);
	}

	private bool ShouldDraw() {
		if (mat_texture_list.GetBool())
			return true;

		if (TxListPanelReq == TxListPanelRequest.TxrShow || TxListPanelReq == TxListPanelRequest.TxrRunning)
			return true;

		return false;
	}

	public void UpdateTotalUsageLabel() {
		Span<char> data = stackalloc char[1024];
		Span<char> kb1 = stackalloc char[20];
		Span<char> kb2 = stackalloc char[20];
		Span<char> kb3 = stackalloc char[20];

		FmtCommaNumber(kb1, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryBoundLastFrame + 511) / 1024);
		FmtCommaNumber(kb2, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryTotalLoaded + 511) / 1024);
		FmtCommaNumber(kb3, (uint)NumDisplayedSizeKB);

		if (Collapse.IsSelected()) {
			ReadOnlySpan<char> title = "";
			sprintf(data, "%s[F %s Kb] / [T %s Kb] / [S %s Kb]").S(title).S(kb1).S(kb2).S(kb3);
		}
		else {
			ReadOnlySpan<char> title = "Texture Memory Usage";
			Span<char> kbMip1 = stackalloc char[20];
			Span<char> kbMip2 = stackalloc char[20];
			FmtCommaNumber(kbMip1, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryEstimatePicmip1 + 511) / 1024);
			FmtCommaNumber(kbMip2, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryEstimatePicmip2 + 511) / 1024);
			sprintf(data, "%s:  frame %s Kb  /  total %s Kb ( picmip1 = %s Kb, picmip2 = %s Kb )  /  shown %s Kb")
				.S(title).S(kb1).S(kb2).S(kbMip1).S(kbMip2).S(kb3);
		}

		// ansitounicode

		TotalUsageLabel.SetText(data);
	}

	public override void OnTextChanged(Panel from) => OnCommand("FilteringTxt");
	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("Close", StringComparison.OrdinalIgnoreCase)) {
			base.OnCommand(command);
			return;
		}

		if (command.Equals("Collapse", StringComparison.OrdinalIgnoreCase)) {
			InvalidateLayout();
			return;
		}

		if (command.Equals("ShowAlpha", StringComparison.OrdinalIgnoreCase)) {
			ViewPanel.SetPaintAlpha(Alpha.IsSelected());
			return;
		}

		if (command.Equals("ThumbWarnings", StringComparison.OrdinalIgnoreCase)) {
			WarnEnable = ThumbWarnings.IsSelected();
			return;
		}

		if (command.Equals("ViewThumbnails", StringComparison.OrdinalIgnoreCase)) {
			InvalidateLayout();
			return;
		}

		if (command.Equals("CopyToClipboard", StringComparison.OrdinalIgnoreCase)) {
			// CopyListPanelToClipboard(ListPanel);
			return;
		}

		if (command.Equals("ReloadAllMaterials", StringComparison.OrdinalIgnoreCase)) {
			cbuf.AddText("mat_reloadallmaterials");
			cbuf.Execute();
			return;
		}

		if (command.Equals("CommitChanges", StringComparison.OrdinalIgnoreCase)) {
			cbuf.AddText("mat_texture_list_txlod_sync save");
			cbuf.Execute();
			return;
		}

		if (command.Equals("DiscardChanges", StringComparison.OrdinalIgnoreCase)) {
			cbuf.AddText("mat_texture_list_txlod_sync reset");
			cbuf.Execute();
			return;
		}

		mat_texture_list_on_f();
		InvalidateLayout();
	}

	private void UpdateDisplayedItem(KeyValues dispData, KeyValues kv) {

	}

	private int AddListItem(KeyValues kv) {
		int item = ListPanel.GetItem(kv.GetString(KeyName_Name))!;
		if (item == -1) {
			kv.SetName(kv.GetString(KeyName_Name));

			item = ListPanel.AddItem(kv, 0, false, false);
			ViewPanel.InvalidateLayout();
		}
		else {
			KeyValues? values = ListPanel.GetItem(item);
			bool needsUpdate = false;//UpdateDisplayedItem(values, kv);

			if (needsUpdate) {
				// ListPanel.ApplyItemChanges();
				ViewPanel.Repaint();
			}
		}

		return item;
	}

	struct LayoutHorz
	{
		public Panel Panel;
		public int Width;
	}

	public override void PerformLayout() {
		base.PerformLayout();

		Collapse.SetPos(2, 10);
		Collapse.SetSize(10, 10);
		Collapse.SetVisible(true);

		bool Collapsed = Collapse.IsSelected();

		GetClientArea(out int x, out int y, out int w, out int t);

		int yOffset = y;

		CVarListLabel.SetPos(x, yOffset);
		CVarListLabel.SetWide(w);
		// yOffset += CVarListLabel.GetTall();
		CVarListLabel.SetVisible(false); // CVarListLabel.SetVisible(!Collapsed);

		TotalUsageLabel.SetPos(x, yOffset);
		TotalUsageLabel.SetWide(w);
		yOffset += TotalUsageLabel.GetTall();
		TotalUsageLabel.SetVisible(!Collapsed);

		Panel[] buttons = [
			SpecialTexs,
			ShowTextureMemoryUsageOption,
			AllTextures,
			ViewTextures,
			FilteringChk,
			HideMipped,
			ResolveTexturePath,
			CopyToClipboardButton
		];

		for (int i = 0; i < buttons.Length; i++) {
			buttons[i].SetPos(x, yOffset);
			buttons[i].SetWide(w / 2);
			yOffset += buttons[i].GetTall();
			buttons[i].SetVisible(!Collapsed);

			if (buttons[i] == ViewTextures) {
				ViewTextures.SetWide(170);
				int accumw = 170;

				Alpha.SetPos(x + accumw + 5, yOffset - ViewTextures.GetTall());
				Alpha.SetWide(85);
				accumw += 85;

				ThumbWarnings.SetPos(x + accumw + 5, yOffset - ViewTextures.GetTall());
				ThumbWarnings.SetWide(85);
				// accumw += 85;
			}

			if (buttons[i] == FilteringChk) {
				FilteringChk.SetWide(60);
				int accumw = 60;

				FilteringText.SetPos(x + accumw + 5, yOffset - FilteringChk.GetTall());
				FilteringText.SetWide(170);
				FilteringText.SetTall(FilteringChk.GetTall());
				FilteringText.SetVisible(!Collapsed);
				// accumw += 170;
			}
		}

		if (Collapsed) {
			int xOffset = 85;
			int Width;

			LayoutHorz[] layout = [
				new () { Panel = TotalUsageLabel, Width = 290 },
				new () { Panel = ViewTextures, Width = 170 },
				new () { Panel = Alpha, Width = 60 },
				new () { Panel = AllTextures, Width = 135 },
				new () { Panel = HideMipped, Width = 100 },
				new () { Panel = FilteringChk, Width = 60 },
				new () { Panel = FilteringText, Width = 130 },
				new () { Panel = ReloadAllMaterialsButton, Width = 130 },
				new () { Panel = CommitChangesButton, Width = 130 },
				new () { Panel = DiscardChangesButton, Width = 130 }
			];

			for (int k = 0; k < layout.Length; k++) {
				layout[k].Panel.SetPos(xOffset, 2);
				Width = layout[k].Width;
				Width = Math.Min(w - xOffset - 30, Width);
				layout[k].Panel.SetWide(Width);
				layout[k].Panel.SetVisible(Width > 50);

				if (Width > 50)
					xOffset += Width + 5;
			}

			yOffset = y;
		}

		Alpha.SetVisible(ViewTextures.IsSelected());
		ThumbWarnings.SetVisible(!Collapsed && ViewTextures.IsSelected());

		ListPanel.SetBounds(x, yOffset, w, t - (yOffset - y));
		ViewPanel.SetBounds(x, yOffset, w, t - (yOffset - y));

		ListPanel.SetVisible(!mat_texture_list_view.GetBool());
		ViewPanel.SetVisible(mat_texture_list_view.GetBool());
	}

	public void OnTurnedOn() {
		// RecursiveRequestToShowTextureList

		ListPanel?.DeleteAllItems();
		ViewPanel.GetRenderTxEditor()?.Close();

		MakePopup(false, false);
		MoveToFront();
	}

	private void EndPaint() => UpdateTotalUsageLabel();

	public override void Paint() {
		if (Font == null)
			return;

		if (!mat_texture_list.GetBool() || !materialSystemDebugTextureInfo.IsDebugTextureListFresh()) {
			EndPaint();
			return;
		}

		using SmartTextureKeyValues textureList = new();
		if (textureList.Get() == null)
			return;

		RenderTextureEditor rte = ViewPanel.GetRenderTxEditor();

		if (TxListPanelReq == TxListPanelRequest.TxrRunning && rte.IsVisible()) {
			KeyValues? kv = null;
			int hint = 0;

			// todo
		}

		if (mat_texture_list_all.GetBool()) {
			if (TxListPanelReq != TxListPanelRequest.TxrRunning) {
				mat_texture_list.SetValue(0);
				TxListPanelReq = TxListPanelRequest.TxrShow;
			}
			else
				TxListPanelReq = TxListPanelRequest.TxrRunning;
		}
		else if (TxListPanelReq == TxListPanelRequest.TxrShow) {
			ListPanel.RemoveAll();
			ViewPanel.InvalidateLayout();
			TxListPanelReq = TxListPanelRequest.TxrRunning;
			EndPaint();
			return;
		}

		BitArray itemsTouched = new(4096 * 8);

		KeepSpecialKeys(textureList.Get()!, SpecialTexs.IsSelected());

		if (FilteringChk.IsSelected() && FilteringText.GetTextLength() > 0) {
			Span<char> filter = stackalloc char[260];
			FilteringText.GetText(filter);
			KeepKeysMatchingFilter(textureList.Get()!, filter);
		}

		if (HideMipped.IsSelected())
			KeepKeysMarkedNoMip(textureList.Get()!);

		int totalDisplayedSizeInBytes = 0;
		Span<char> resolveName = stackalloc char[256];
		Span<char> resolveNameArg = stackalloc char[256];
		for (KeyValues? cur = textureList.Get()!.GetFirstSubKey(); cur != null; cur = cur.GetNextKey()) {
			int sizeInBytes = cur.GetInt(KeyName_Size);
			totalDisplayedSizeInBytes += sizeInBytes;

			int numCount = cur.GetInt(KeyName_Size);
			if (numCount > 1)
				sizeInBytes *= numCount;

			int sizeInKilo = (sizeInBytes + 511) / 1024;
			cur.SetInt(KeyName_Size, sizeInKilo);

			if (ResolveTexturePath.IsSelected()) {
				resolveName.Clear();
				resolveNameArg.Clear();
				sprintf(resolveNameArg, "materials/%s.vtf").S(cur.GetString(KeyName_Name));
				ReadOnlySpan<char> rseolvedName = fileSystem.RelativePathToFullPath(resolveNameArg, "game", resolveName);
				if (resolveName.Length > 0)
					cur.SetString(KeyName_Path, rseolvedName);
			}

			int item = AddListItem(cur);
			if (item < itemsTouched.Length)
				itemsTouched.Set(item, true);
		}

		NumDisplayedSizeKB = (totalDisplayedSizeInBytes + 511) / 1024;

		int next = 0;
		int numRemoved = 0;

		// todo remove items, sort, layout

		EndPaint();
	}

	public static void CreateTextureListPanel(Panel parent) => g_TextureListPanel = new(parent);

	[ConCommand("+mat_texture_list")]
	static private void mat_texture_list_on_f() {
		ConVarRef sv_cheats = new("sv_cheats");
		if (sv_cheats.IsValid() && sv_cheats.GetBool() == false)
			return;

		ConVarRef mat_queue_mode = new("mat_queue_mode");
		if (mat_queue_mode.IsValid() && SaveQueueState == int.MinValue) {
			SaveQueueState = mat_queue_mode.GetInt();
			mat_queue_mode.SetValue(0);
		}

		mat_texture_list.SetValue(1);

		g_TextureListPanel?.OnTurnedOn();
	}

	[ConCommand("-mat_texture_list")]
	private void mat_texture_list_off_f() {
		mat_texture_list.SetValue(0);
		TxListPanelReq = TxListPanelRequest.TxrHide;

		if (CursorSet) {
			Surface.SetCursorAlwaysVisible(false);
			CursorSet = false;
		}

		if (SaveQueueState != int.MinValue) {
			ConVarRef mat_queue_mode = new("mat_queue_mode");
			mat_queue_mode.SetValue(SaveQueueState);
			SaveQueueState = int.MinValue;
		}
	}

	static int KilobytesSortFunc(Panel _, ListPanelItem item1, ListPanelItem item2) {
		var a = int.Parse(item1.kv!.GetString(KeyName_Size));
		var b = int.Parse(item2.kv!.GetString(KeyName_Size));

		if (a < b) return 1;
		if (a > b) return -1;
		return 0;
	}


	private static bool StripDirName(Span<char> filename) {
		if (filename.Length == 0 || filename[0] == '\0')
			return false;

		Span<char> lastSlash = filename;
		while (true) {
			Span<char> testSlash = lastSlash.Slice(0, lastSlash.IndexOf('/'));
			if (testSlash.Length == 0) {
				testSlash = lastSlash[..lastSlash.IndexOf('\\')];
				if (testSlash.Length == 0)
					break;
			}

			testSlash = testSlash[1..];
			lastSlash = testSlash;
		}

		if (lastSlash == filename)
			return false;
		else {
			Assert(lastSlash[^1] == '/' || lastSlash[^1] == '\\');
			lastSlash[^1] = '\0';
			return true;
		}
	}

	private static void ToLowerInplace(Span<char> str) {
		for (int i = 0; i < str.Length; i++) {
			if (char.IsUpper(str[i]))
				str[i] = char.ToLower(str[i]);
		}
	}
	private static void KeepSpecialKeys(KeyValues textureList, bool serviceKeys) {
		KeyValues? pNext;
		for (KeyValues? pCur = textureList.GetFirstSubKey(); pCur != null; pCur = pNext) {
			pNext = pCur.GetNextKey();

			bool isServiceKey = false;

			ReadOnlySpan<char> name = pCur.GetString(KeyName_Name);
			if (name.StartsWith("_") ||
				name.StartsWith("[") ||
				name.Equals("backbuffer", StringComparison.OrdinalIgnoreCase) ||
				name.StartsWith("colorcorrection", StringComparison.OrdinalIgnoreCase) ||
				name.Equals("depthbuffer", StringComparison.OrdinalIgnoreCase) ||
				name.Equals("frontbuffer", StringComparison.OrdinalIgnoreCase) ||
				name.Equals("normalize", StringComparison.OrdinalIgnoreCase) ||
				name.Length == 0) {
				isServiceKey = true;
			}

			if (isServiceKey != serviceKeys)
				textureList.RemoveSubKey(pCur);
			else if (isServiceKey)
				pCur.SetInt("SpecialTx", 1);
		}
	}

	public static void UpdateTextureListPanel() {
		if (mat_show_texture_memory_usage.GetInt() != 0) {
			Con_NPrint_s info = new() {
				Index = 4,
				TimeToLive = 0.2,
				FixedWidthFont = true,
				Color = new(1, 0.5f, 0)
			};

			Span<char> kb1 = stackalloc char[20];
			Span<char> kb2 = stackalloc char[20];

			FmtCommaNumber(kb1, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryBoundLastFrame + 511) / 1024);
			FmtCommaNumber(kb2, (uint)materialSystemDebugTextureInfo.GetTextureMemoryUsed(TextureMemoryType.MemoryTotalLoaded + 511) / 1024);

			// todo Con_NXPrintf
		}

		// MatViewOverride::DisplaySelectedTextures();

		materialSystemDebugTextureInfo.EnableGetAllTextures(mat_texture_list_all.GetBool());
		materialSystemDebugTextureInfo.EnableDebugTextureList(mat_texture_list.GetInt() > 0);

		bool shouldDrawTxListPanel = g_TextureListPanel!.ShouldDraw();
		if (g_TextureListPanel.IsVisible() != shouldDrawTxListPanel) {
			g_TextureListPanel.SetVisible(shouldDrawTxListPanel);

			if (shouldDrawTxListPanel)
				mat_texture_list_on_f();
			else
				g_TextureListPanel.mat_texture_list_off_f();
		}
	}

	private void KeepKeysMatchingFilter(KeyValues textureList, ReadOnlySpan<char> filter) {
		if (filter.Length == 0)
			return;

		Span<char> chFilter = stackalloc char[260];
		Span<char> chName = stackalloc char[260];

		filter.CopyTo(chFilter);
		ToLowerInplace(chFilter);

		KeyValues? next;
		for (KeyValues? cur = textureList.GetFirstSubKey(); cur != null; cur = next) {
			next = cur.GetNextKey();

			ReadOnlySpan<char> name = cur.GetString(KeyName_Name);
			name.CopyTo(chName);
			ToLowerInplace(chName);

			if (!chName.Contains(chFilter, StringComparison.OrdinalIgnoreCase))
				textureList.RemoveSubKey(cur);
		}
	}

	private void KeepKeysMarkedNoMip(KeyValues textureList) {
		KeyValues? next;
		for (KeyValues? cur = textureList.GetFirstSubKey(); cur != null; cur = next) {
			next = cur.GetNextKey();

			ReadOnlySpan<char> textureFile = cur.GetString(KeyName_Name);
			ReadOnlySpan<char> textureGroup = cur.GetString(KeyName_Texture_Group);
			if (!textureFile.IsEmpty) {
				ITexture? matTexture = materialSystem.FindTexture(textureFile, textureGroup, false);
				if (matTexture != null && (matTexture.GetFlags() & (int)TextureFlags.NoMip) == 0)
					textureList.RemoveSubKey(cur);
			}
		}
	}

	public static void FmtCommaNumber(Span<char> buffer, uint number) {
		buffer[0] = '\0';
		for (uint divisor = 1000 * 1000 * 1000; divisor > 0; divisor /= 1000) {
			if (number >= divisor) {
				uint print = number / divisor % 1000;
				sprintf(buffer, (number / divisor < 1000) ? "{%d}," : "{%03d},").D(print);
			}
		}

		int len = buffer.IndexOf('\0');
		if (len == 0)
			sprintf(buffer, "0");
		else if (buffer[len - 1] == ',')
			buffer[len - 1] = '\0';
	}
}


class SmartTextureKeyValues : IDisposable
{
	private KeyValues? _p;

	public SmartTextureKeyValues() {
		var p = materialSystemDebugTextureInfo.GetDebugTextureList();
		if (p != null)
			_p = p.MakeCopy();
	}

	public KeyValues? Get() => _p;

	public void Dispose() {
		_p = null;
	}
}
