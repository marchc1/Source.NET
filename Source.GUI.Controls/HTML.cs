using Source.Common.Bitmap;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

using Steamworks;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Source.GUI.Controls;

class HTMLInterior : Panel
{
	HTML HTML;
	public HTMLInterior(HTML parent) : base(parent, "HTMLInterior") {
		HTML = parent;
		SetPaintBackgroundEnabled(false);
		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
	}
}

class HTMLPopup : Frame
{
	public HTMLPopup(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

public class HTML : Panel
{
	public static Panel Create_HTML() => new HTML(null, null);

	Panel InteriorPanel;
	ScrollBar HBar, VBar;
	FileOpenDialog FileOpenDialog;

	public class HTMLFindBar : EditablePanel
	{
		TextEntry FindBar;
		HTML Parent;
		Label FindCountLabel;
		bool Hidden;
		public HTMLFindBar(HTML parent) : base(parent, null) {

		}

		internal bool BIsHidden() {
			return false;//todo
		}

		internal void GetText(Span<char> txt) {
			throw new NotImplementedException();
		}
	}

	HTMLFindBar FindBar;

	int MouseX, MouseY;

	int ScrollBorderX, ScrollBorderY;
	int WideLastHTMLSize, TallLastHTMLSize;
	int CopyLinkMenuItemID;

	bool ScrollBarEnabled;
	bool ContextMenuEnabled;
	int ScrollbarSize;
	bool bNewWindowsOnly;
	int ViewSourceAllowedIndex;
	string? DragURL;
	int DragStartX, DragStartY;

	struct CustomURLHandler_t
	{
		public Panel Panel;
		public InlineArray32<char> URL;
	}
	List<CustomURLHandler_t> CustomURLHandlers;

	TextureID HTMLTextureID;

	int AllocedTextureWidth;
	int AllocedTextureHeight;
	bool NeedsFullTextureUpload;
	string CurrentURL;
	bool InFind;
	string LastSearchString;

	bool CanGoBack;
	bool CanGoForward;

	struct LinkAtPos_t()
	{
		public UInt32 X;
		public UInt32 Y;
		string URL;
	}
	LinkAtPos_t LinkAtPos;
	bool RequestingDragURL;
	bool RequestingCopyLink;

	struct ScrollData_t()
	{
		public bool Visible;
		public int Max;
		public int Scroll;
		public float Zoom;

		public static bool operator ==(ScrollData_t lhs, ScrollData_t rhs) => lhs.Visible == rhs.Visible && lhs.Max == rhs.Max && lhs.Scroll == rhs.Scroll;
		public static bool operator !=(ScrollData_t lhs, ScrollData_t rhs) => !(lhs == rhs);
		public override readonly bool Equals(object? obj) => obj is ScrollData_t other && this == other;
		public override readonly int GetHashCode() => HashCode.Combine(Visible, Max, Scroll);
	}

	ScrollData_t ScrollHorizontal;
	ScrollData_t ScrollVertical;
	float Zoom;

	string PendingURLLoad;
	string PendingPostData;

	Menu ContextMenu;

	struct CustomCursorCache_t
	{
		public float CacheTime;
		public CursorCode Cursor;
		public object? Data;

		public CustomCursorCache_t() { }
		public CustomCursorCache_t(object? data) {
			CacheTime = -1;
			Cursor = CursorCode.No;
			Data = data;
		}

		public static bool operator ==(CustomCursorCache_t lhs, CustomCursorCache_t rhs) => ReferenceEquals(lhs.Data, rhs.Data);
		public static bool operator !=(CustomCursorCache_t lhs, CustomCursorCache_t rhs) => !(lhs == rhs);
		public override readonly bool Equals(object? obj) => obj is CustomCursorCache_t other && this == other;
		public override readonly int GetHashCode() => Data?.GetHashCode() ?? 0;
	}
	List<CustomCursorCache_t> HCursor;

	HHTMLBrowser BrowserHandle;

	Callback<HTML_NeedsPaint_t> NeedsPaint;
	Callback<HTML_StartRequest_t> StartRequest;
	Callback<HTML_URLChanged_t> URLChanged;
	Callback<HTML_FinishedRequest_t> FinishedRequest;
	Callback<HTML_OpenLinkInNewTab_t> LinkInNewTab;
	Callback<HTML_ChangedTitle_t> ChangeTitle;
	Callback<HTML_NewWindow_t> NewWindow;
	Callback<HTML_FileOpenDialog_t> FileLoadDialog;
	Callback<HTML_SearchResults_t> SearchResults;
	Callback<HTML_CloseBrowser_t> CloseBrowser;
	Callback<HTML_HorizontalScroll_t> HorizScroll;
	Callback<HTML_VerticalScroll_t> VertScroll;
	Callback<HTML_LinkAtPosition_t> LinkAtPosResp;
	Callback<HTML_JSAlert_t> JSAlert;
	Callback<HTML_JSConfirm_t> JSConfirm;
	Callback<HTML_CanGoBackAndForward_t> CanGoBackForward;
	Callback<HTML_SetCursor_t> SetCursor;
	Callback<HTML_StatusText_t> StatusText;
	Callback<HTML_ShowToolTip_t> ShowTooltip;
	Callback<HTML_UpdateToolTip_t> UpdateTooltip;
	Callback<HTML_HideToolTip_t> HideTooltip;

	CallResult<HTML_BrowserReady_t> SteamCallResultBrowserReady;

	public HTML(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		NeedsPaint = Callback<HTML_NeedsPaint_t>.Create(BrowserNeedsPaint);
		StartRequest = Callback<HTML_StartRequest_t>.Create(BrowserStartRequest);
		URLChanged = Callback<HTML_URLChanged_t>.Create(BrowserURLChanged);
		FinishedRequest = Callback<HTML_FinishedRequest_t>.Create(BrowserFinishedRequest);
		LinkInNewTab = Callback<HTML_OpenLinkInNewTab_t>.Create(BrowserOpenNewTab);
		ChangeTitle = Callback<HTML_ChangedTitle_t>.Create(BrowserSetHTMLTitle);
		NewWindow = Callback<HTML_NewWindow_t>.Create(BrowserPopupHTMLWindow);
		FileLoadDialog = Callback<HTML_FileOpenDialog_t>.Create(BrowserFileLoadDialog);
		SearchResults = Callback<HTML_SearchResults_t>.Create(BrowserSearchResults);
		CloseBrowser = Callback<HTML_CloseBrowser_t>.Create(BrowserClose);
		HorizScroll = Callback<HTML_HorizontalScroll_t>.Create(BrowserHorizontalScrollBarSizeResponse);
		VertScroll = Callback<HTML_VerticalScroll_t>.Create(BrowserVerticalScrollBarSizeResponse);
		LinkAtPosResp = Callback<HTML_LinkAtPosition_t>.Create(BrowserLinkAtPositionResponse);
		JSAlert = Callback<HTML_JSAlert_t>.Create(BrowserJSAlert);
		JSConfirm = Callback<HTML_JSConfirm_t>.Create(BrowserJSConfirm);
		CanGoBackForward = Callback<HTML_CanGoBackAndForward_t>.Create(BrowserCanGoBackandForward);
		SetCursor = Callback<HTML_SetCursor_t>.Create(BrowserSetCursor);
		StatusText = Callback<HTML_StatusText_t>.Create(BrowserStatusText);
		ShowTooltip = Callback<HTML_ShowToolTip_t>.Create(BrowserShowToolTip);
		UpdateTooltip = Callback<HTML_UpdateToolTip_t>.Create(BrowserUpdateToolTip);
		HideTooltip = Callback<HTML_HideToolTip_t>.Create(BrowserHideToolTip);

		HTMLTextureID = 0;
		CanGoBack = false;
		CanGoForward = false;
		InFind = false;
		RequestingDragURL = false;
		RequestingCopyLink = false;
		Zoom = 100.0f;
		NeedsFullTextureUpload = false;

		InteriorPanel = new HTMLInterior(this);
		SetPostChildPaintEnabled(true);

		BrowserHandle = HHTMLBrowser.Invalid;

		SteamAPI.Init();
		SteamHTMLSurface.Init();

		SteamCallResultBrowserReady = CallResult<HTML_BrowserReady_t>.Create(OnBrowserReady);

		SteamAPICall_t SteamAPICall = SteamHTMLSurface.CreateBrowser("Valve Client", null); // surface().GetWebkitHTMLUserAgentString() todo
		SteamCallResultBrowserReady.Set(SteamAPICall);

		ScrollBorderX = ScrollBorderY = 0;
		ScrollBarEnabled = true;
		ContextMenuEnabled = true;
		bNewWindowsOnly = false;
		MouseX = MouseY = 0;
		DragStartX = DragStartY = 0;
		ViewSourceAllowedIndex = -1;
		WideLastHTMLSize = TallLastHTMLSize = 0;

		HBar = new ScrollBar(this, "HorizScrollBar", false);
		HBar.SetVisible(false);
		HBar.AddActionSignalTarget(this);

		VBar = new ScrollBar(this, "VertScrollBar", true);
		VBar.SetVisible(false);
		VBar.AddActionSignalTarget(this);

		FindBar = new HTMLFindBar(this);
		FindBar.SetZPos(2);
		FindBar.SetVisible(false);

		ContextMenu = new Menu(this, "contextmenu");
		ContextMenu.AddMenuItem("#vgui_HTMLBack", new KeyValues("Command", "command", "back"), this);
		ContextMenu.AddMenuItem("#vgui_HTMLForward", new KeyValues("Command", "command", "forward"), this);
		ContextMenu.AddMenuItem("#vgui_HTMLReload", new KeyValues("Command", "command", "reload"), this);
		ContextMenu.AddMenuItem("#vgui_HTMLStop", new KeyValues("Command", "command", "stop"), this);
		ContextMenu.AddSeparator();
		ContextMenu.AddMenuItem("#vgui_HTMLCopyUrl", new KeyValues("Command", "command", "copyurl"), this);
		CopyLinkMenuItemID = ContextMenu.AddMenuItem("#vgui_HTMLCopyLink", new KeyValues("Command", "command", "copylink"), this);
		ContextMenu.AddMenuItem("#TextEntry_Copy", new KeyValues("Command", "command", "copy"), this);
		ContextMenu.AddMenuItem("#TextEntry_Paste", new KeyValues("Command", "command", "paste"), this);
		ContextMenu.AddSeparator();
		ViewSourceAllowedIndex = ContextMenu.AddMenuItem("#vgui_HTMLViewSource", new KeyValues("Command", "command", "viewsource"), this);
	}

	public override void Dispose() {
		base.Dispose();

		ContextMenu?.MarkForDeletion();
		SteamHTMLSurface.RemoveBrowser(BrowserHandle);
		HCursor.Clear();
	}

	void OnBrowserReady(HTML_BrowserReady_t browserReady, bool ioFailure) {
		BrowserHandle = browserReady.unBrowserHandle;
		BrowserResize();

		if (!string.IsNullOrEmpty(PendingURLLoad)) {
			PostURL(PendingURLLoad, PendingPostData, false);
			PendingURLLoad = "";
		}
	}

	void OnSetCursorVGUI(int cursor) { }

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		BrowserResize();
	}

	public override void Paint() {
		base.Paint();

		if (HTMLTextureID != 0) {
			Surface.DrawSetTexture(HTMLTextureID);
			Surface.DrawSetColor(255, 255, 255, 255);
			GetSize(out int tw, out int tt);
			Surface.DrawTexturedRect(0, 0, tw, tt);
		}

		if (ScrollBorderX > 0 || ScrollBorderY > 0) {
			// todo
		}
	}

	public override void PerformLayout() {
		base.PerformLayout();
		Repaint();
		int vbarInset = VBar.IsVisible() ? VBar.GetWide() : 0;
		int maxw = GetWide() - vbarInset;
		InteriorPanel.SetBounds(0, 0, maxw, GetTall());

		IScheme clientScheme = SchemeManager.GetScheme("ClientScheme");

		int SearchInsetY = QuickPropScale(5);
		int SearchInsetX = QuickPropScale(5);
		int SearchTall = QuickPropScale(24);
		int SearchWide = QuickPropScale(150);

		ReadOnlySpan<char> resourceString = clientScheme.GetResourceString("HTML.SearchInsetY");
		if (!resourceString.IsEmpty)
			SearchInsetY = QuickPropScale(int.Parse(resourceString));

		resourceString = clientScheme.GetResourceString("HTML.SearchInsetX");
		if (!resourceString.IsEmpty)
			SearchInsetX = QuickPropScale(int.Parse(resourceString));

		resourceString = clientScheme.GetResourceString("HTML.SearchTall");
		if (!resourceString.IsEmpty)
			SearchTall = QuickPropScale(int.Parse(resourceString));

		resourceString = clientScheme.GetResourceString("HTML.SearchWide");
		if (!resourceString.IsEmpty)
			SearchWide = QuickPropScale(int.Parse(resourceString));

		FindBar.SetBounds(GetWide() - SearchWide - SearchInsetX - vbarInset, FindBar.BIsHidden() ? -1 * SearchTall - QuickPropScale(5) : SearchInsetY, SearchWide, SearchTall);
	}

	void OnMove() { }

	void OpenURL(string URL, string postData, bool force) => PostURL(URL, postData, force);

	void PostURL(string URL, string postData, bool force) {
		if (BrowserHandle == HHTMLBrowser.Invalid) {
			PendingURLLoad = URL;
			PendingPostData = postData;
			return;
		}

		if (false) { // Offline Mode

		}
		else {
			if (!string.IsNullOrEmpty(postData))
				SteamHTMLSurface.LoadURL(BrowserHandle, URL, postData);
			else
				SteamHTMLSurface.LoadURL(BrowserHandle, URL, null);
		}
	}

	bool StopLoading() {
		throw new NotImplementedException();
	}

	bool Refresh() {
		throw new NotImplementedException();
	}

	void GoBack() { }

	void GoForward() { }

	bool BCanGoBack() {
		throw new NotImplementedException();
	}

	bool BCanGoFoward() {
		throw new NotImplementedException();
	}

	public override void OnSizeChanged(int wide, int tall) {
		base.OnSizeChanged(wide, tall);
		UpdateSizeAndScrollBars();
	}

	void RunJavascript(ReadOnlySpan<char> script) { }

	static EHTMLMouseButton ConvertMouseCodeToCEFCode(ButtonCode code) => code switch {
		ButtonCode.MouseLeft => EHTMLMouseButton.eHTMLMouseButton_Left,
		ButtonCode.MouseRight => EHTMLMouseButton.eHTMLMouseButton_Right,
		ButtonCode.MouseMiddle => EHTMLMouseButton.eHTMLMouseButton_Middle,
		_ => EHTMLMouseButton.eHTMLMouseButton_Left,
	};


	public override void OnMousePressed(ButtonCode code) {
		DragURL = null;

		if (code == ButtonCode.Mouse4) {
			PostActionSignal(new("HTMLBackRequested"));
			return;
		}

		if (code == ButtonCode.Mouse5) {
			PostActionSignal(new("HTMLForwardRequested"));
			return;
		}

		if (code == ButtonCode.MouseRight && ContextMenuEnabled) {
			GetLinkAtPosition(MouseX, MouseY);
			Menu.PlaceContextMenu(this, ContextMenu);
		}

		RequestFocus();

		if (code != ButtonCode.MouseRight)
			SteamHTMLSurface.MouseDown(BrowserHandle, ConvertMouseCodeToCEFCode(code));

		if (code == ButtonCode.MouseLeft) {
			Input.GetCursorPos(out DragStartX, out DragStartY);
			GetAbsPos(out int htmlx, out int htmly);

			GetLinkAtPosition(DragStartX - htmlx, DragStartY - htmly);

			RequestingDragURL = true;

			if (!string.IsNullOrEmpty(DragURL))
				Input.SetMouseCapture(this);
		}
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (code == ButtonCode.MouseLeft) {
			Input.SetMouseCapture(null);
			Input.SetCursorOveride(0);

			if (!string.IsNullOrEmpty(DragURL) && Input.GetMouseOver() != this && Input.GetMouseOver() != null) {
				KeyValues kv = new("DragDrop");
				if (((Panel?)Input.GetMouseOver())!.RequestInfo(kv) && kv.GetPtr("AcceptPanel") != null) {
					IPanel vpanel = (IPanel)kv.GetPtr("AcceptPanel")!;
					VGui.PostMessage(vpanel, new KeyValues("DragDrop", "text", DragURL), this);
				}
			}
			DragURL = null;
		}

		SteamHTMLSurface.MouseUp(BrowserHandle, ConvertMouseCodeToCEFCode(code));
	}

	public override void OnCursorMoved(int x, int y) {
		if (Input.GetMouseOver() == this) {
			MouseX = x;
			MouseY = y;

			SteamHTMLSurface.MouseMove(BrowserHandle, MouseX, MouseY);
		}
		else if (!string.IsNullOrEmpty(DragURL)) {
			if (Input.GetMouseOver() == null)
				DragURL = null;
		}

		if (!string.IsNullOrEmpty(DragURL) && Input.GetCursorOveride() == 0)
			Input.GetCursorPos(out int gx, out int gy);
	}

	public override void OnMouseDoublePressed(ButtonCode code) => SteamHTMLSurface.MouseDoubleClick(BrowserHandle, ConvertMouseCodeToCEFCode(code));

	public override void OnKeyTyped(char unichar) => SteamHTMLSurface.KeyChar(BrowserHandle, unichar, (EHTMLKeyModifiers)GetKeyModifiers());

	int GetKeyModifiers() {
		int modifierCodes = 0;
		if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl))
			modifierCodes |= (int)EHTMLKeyModifiers.k_eHTMLKeyModifier_CtrlDown;

		if (Input.IsKeyDown(ButtonCode.KeyLAlt) || Input.IsKeyDown(ButtonCode.KeyRAlt))
			modifierCodes |= (int)EHTMLKeyModifiers.k_eHTMLKeyModifier_AltDown;

		if (Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift))
			modifierCodes |= (int)EHTMLKeyModifiers.k_eHTMLKeyModifier_ShiftDown;

		return modifierCodes;
	}

	void ShowFindDialog() { }

	void HideFindDialog() { }

	bool FindDialogVisible() {
		throw new NotImplementedException();
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		switch (code) {
			case ButtonCode.KeyPageDown: {
					int val = VBar.GetValue();
					val += 200;
					VBar.SetValue(val);
					break;
				}
			case ButtonCode.KeyPageUp: {
					int val = VBar.GetValue();
					val -= 200;
					VBar.SetValue(val);
					break;
				}
			case ButtonCode.KeyF5: {
					Refresh();
					break;
				}
			case ButtonCode.KeyF: {
					if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl)
						|| (IsOSX() && (Input.IsKeyDown(ButtonCode.KeyLWin) || Input.IsKeyDown(ButtonCode.KeyRWin)))) {
						if (!FindDialogVisible())
							ShowFindDialog();
						else
							HideFindDialog();
						break;
					}
					goto case ButtonCode.KeyEscape;
				}
			case ButtonCode.KeyEscape: {
					if (FindDialogVisible()) {
						HideFindDialog();
						break;
					}
					break;
				}
			case ButtonCode.KeyTab: {
					if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl)) {
						base.OnKeyTyped((char)code);
						return;
					}
					break;
				}
		}

		SteamHTMLSurface.KeyDown(BrowserHandle, KeyCode_VGUIToVirtualKey(code), (EHTMLKeyModifiers)GetKeyModifiers());
	}

	public override void OnKeyCodeReleased(ButtonCode code) => SteamHTMLSurface.KeyUp(BrowserHandle, KeyCode_VGUIToVirtualKey(code), (EHTMLKeyModifiers)GetKeyModifiers());

	static uint KeyCode_VGUIToVirtualKey(ButtonCode code) => (uint)Singleton<IInputSystem>().ButtonCodeToVirtualKey(code);

	public override void OnMouseWheeled(int delta) {
		if (VBar != null) {
			int val = VBar.GetValue();
			val -= (int)(delta * 100.0f / 3);
			VBar.SetValue(val);

			SteamHTMLSurface.MouseWheel(BrowserHandle, (int)(delta * 100.0f / 3));
		}
	}

	void AddCustomURLHandler(ReadOnlySpan<char> customProtocolName, Panel target) { }

	void BrowserResize() {
		if (BrowserHandle == HHTMLBrowser.Invalid)
			return;

		GetSize(out int w, out int h);
		int right = 0, bottom = 0;

		if (WideLastHTMLSize != (w - ScrollBorderX - right) || TallLastHTMLSize != (h - ScrollBorderY - bottom)) {
			WideLastHTMLSize = w - ScrollBorderX - right;
			TallLastHTMLSize = h - ScrollBorderY - bottom;
			if (TallLastHTMLSize <= 0) {
				SetTall(QuickPropScale(64));
				TallLastHTMLSize = QuickPropScale(64) - bottom;
			}

			SteamHTMLSurface.SetSize(BrowserHandle, (uint)WideLastHTMLSize, (uint)TallLastHTMLSize);

			int scrollV = VBar.GetValue();
			int scrollH = HBar.GetValue();

			SteamHTMLSurface.SetHorizontalScroll(BrowserHandle, (uint)scrollH);
			SteamHTMLSurface.SetVerticalScroll(BrowserHandle, (uint)scrollV);
		}
	}

	void OnSliderMoved() { }

	bool IsScrolledToBottom() {
		throw new NotImplementedException();
	}

	bool IsScrollbarVisible() {
		throw new NotImplementedException();
	}

	void SetScrollbarsEnabled(bool state) { }

	void SetContextMenuEnabled(bool state) { }

	void SetViewSourceEnabled(bool state) { }

	void NewWindowsOnly(bool state) { }

	void AddHeader(ReadOnlySpan<char> pchHeader, ReadOnlySpan<char> pchValue) { }

	public override void OnSetFocus() {
		SteamHTMLSurface.SetKeyFocus(BrowserHandle, true);
		base.OnSetFocus();
	}

	public override void OnKillFocus(Panel panel) {
		base.OnKillFocus(panel);

		if (!ContextMenu.HasFocus())
			SteamHTMLSurface.SetKeyFocus(BrowserHandle, false);
	}

	void OnCommand(ReadOnlySpan<char> command) { }

	void OnFileSelected(ReadOnlySpan<char> selectedFile) { }

	void OnFileSelectionCancelled() { }

	void Find(ReadOnlySpan<char> subStr) { }

	void FindPrevious() { }

	void FindNext() { }

	void StopFind() { }

	void OnEditNewLine(Panel panel) { }

	public override void OnTextChanged(Panel panel) {
		Span<char> txt = stackalloc char[2000];
		FindBar.GetText(txt);
		Find(txt);
	}

	void BrowserNeedsPaint(HTML_NeedsPaint_t callback) {
		int tw = 0, tt = 0;
		if (HTMLTextureID != 0) {
			tw = AllocedTextureWidth;
			tt = AllocedTextureHeight;
		}

		if (HTMLTextureID != 0 && ((VBar.IsVisible() && callback.unScrollY > 0 && Math.Abs((int)callback.unScrollY - ScrollVertical.Scroll) > 5) || (HBar.IsVisible() && callback.unScrollX > 0 && Math.Abs((int)callback.unScrollX - ScrollHorizontal.Scroll) > 5))) {
			NeedsFullTextureUpload = true;
			return;
		}

		if (NeedsFullTextureUpload || HTMLTextureID == 0 || tw != (int)callback.unWide || tt != (int)callback.unTall) {
			NeedsFullTextureUpload = false;
			if (HTMLTextureID != 0)
				Surface.DeleteTextureByID(HTMLTextureID);

			HTMLTextureID = Surface.CreateNewTextureID(true);
			Surface.DrawSetTextureRGBAEx(HTMLTextureID, callback.pBGRA, (int)callback.unWide, (int)callback.unTall, ImageFormat.BGRA8888);
			AllocedTextureWidth = (int)callback.unWide;
			AllocedTextureHeight = (int)callback.unTall;
		}
		else if ((int)callback.unUpdateWide > 0 && (int)callback.unUpdateTall > 0)
			Surface.DrawUpdateRegionTextureRGBA(HTMLTextureID, (int)callback.unUpdateX, (int)callback.unUpdateY, callback.pBGRA, (int)callback.unUpdateWide, (int)callback.unUpdateTall, ImageFormat.BGRA8888);
		else
			Surface.DrawSetTextureRGBAEx(HTMLTextureID, callback.pBGRA, (int)callback.unWide, (int)callback.unTall, ImageFormat.BGRA8888);

		Repaint();
	}

	bool OnStartRequest(ReadOnlySpan<char> url, ReadOnlySpan<char> target, ReadOnlySpan<char> oostData, bool isRedirect) {
		throw new NotImplementedException();
	}

	void BrowserStartRequest(HTML_StartRequest_t cmd) { }

	void BrowserURLChanged(HTML_URLChanged_t cmd) { }

	void BrowserFinishedRequest(HTML_FinishedRequest_t cmd) { }

	void BrowserOpenNewTab(HTML_OpenLinkInNewTab_t cmd) { }

	void BrowserPopupHTMLWindow(HTML_NewWindow_t cmd) { }

	void BrowserSetHTMLTitle(HTML_ChangedTitle_t cmd) { }

	void BrowserStatusText(HTML_StatusText_t cmd) { }

	void BrowserSetCursor(HTML_SetCursor_t cmd) { }

	void BrowserFileLoadDialog(HTML_FileOpenDialog_t cmd) { }

	void BrowserShowToolTip(HTML_ShowToolTip_t cmd) { }

	void BrowserUpdateToolTip(HTML_UpdateToolTip_t cmd) { }

	void BrowserHideToolTip(HTML_HideToolTip_t cmd) { }

	void BrowserSearchResults(HTML_SearchResults_t cmd) { }

	void BrowserClose(HTML_CloseBrowser_t cmd) { }

	void BrowserHorizontalScrollBarSizeResponse(HTML_HorizontalScroll_t cmd) { }

	void BrowserVerticalScrollBarSizeResponse(HTML_VerticalScroll_t cmd) { }

	void BrowserLinkAtPositionResponse(HTML_LinkAtPosition_t cmd) { }

	void BrowserJSAlert(HTML_JSAlert_t cmd) { }

	void BrowserJSConfirm(HTML_JSConfirm_t cmd) { }

	void DismissJSDialog(int result) { }

	void BrowserCanGoBackandForward(HTML_CanGoBackAndForward_t cmd) { }

	void GetLinkAtPosition(int x, int y) { }

	void UpdateSizeAndScrollBars() {
		BrowserResize();
		InvalidateLayout();
	}

#if DEBUG
	[ConCommand("sdn_createhtml")]
	static void CreateHTML(in TokenizedCommand args) {
		ISurface surface = Singleton<ISurface>();
		surface.GetScreenSize(out int screenWide, out int screenTall);

		int wide = screenWide / 2;
		int tall = screenTall / 2;

		Frame frame = new(null, "TestHTMLFrame");
		frame.SetParent(surface.GetEmbeddedPanel());
		frame.SetTitle("HTML Test", true);
		frame.SetSize(wide, tall);
		frame.MoveToCenterOfScreen();
		frame.SetSizeable(true);

		const int inset = 8;
		const int titleInset = 32;
		HTML html = new(frame, "TestHTML");
		html.SetBounds(inset, titleInset, wide - inset * 2, tall - titleInset - inset);
		html.SetAutoResize(PinCorner.TopLeft, AutoResize.DownAndRight, inset, titleInset, -inset, -inset);
		html.SetVisible(true);
		html.OpenURL("https://store.steampowered.com/", "", false);

		frame.Activate();
	}
#endif
}
