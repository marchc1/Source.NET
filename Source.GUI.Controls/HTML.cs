namespace Source.GUI.Controls;

class HTMLInterior : Panel
{
	public HTMLInterior(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

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

	public HTML(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	// 	void OnBrowserReady(HTML_BrowserReady pBrowserReady, bool bIOFailure) { }

	// 	void OnSetCursorVGUI(int cursor) { }

	// 	void ApplySchemeSettings(IScheme pScheme) { }

	// 	void Paint() { }

	// 	void PerformLayout() { }

	// 	void OnMove() { }

	// 	void OpenURL(const char URL, const char postData, bool force){}

	// void PostURL(const char URL, const char pchPostData, bool force) { }

	// 	bool StopLoading() { }

	// 	bool Refresh() { }

	// 	void GoBack() { }

	// 	void GoForward() { }

	// 	bool BCanGoBack() { }

	// 	bool BCanGoFoward() { }

	// 	void OnSizeChanged(int wide, int tall) { }

	// 	void RunJavascript( const char pchScript) { }

	// 	void OnMousePressed(MouseCode code) { }

	// 	void OnMouseReleased(MouseCode code) { }

	// 	void OnCursorMoved(int x, int y) { }

	// 	void OnMouseDoublePressed(MouseCode code) { }

	// 	void OnKeyTyped(wchar unichar) { }

	// 	void ShowFindDialog() { }

	// 	void HideFindDialog() { }

	// 	bool FindDialogVisible() { }

	// 	void OnKeyCodeTyped(KeyCode code) { }

	// 	void OnKeyCodeReleased(KeyCode code) { }

	// 	void OnMouseWheeled(int delta) { }

	// 	void AddCustomURLHandler(const char customProtocolName, vgui::Panel target) { }

	// 	void BrowserResize() { }

	// 	void OnSliderMoved() { }

	// 	bool IsScrolledToBottom() { }

	// 	bool IsScrollbarVisible() { }

	// 	void SetScrollbarsEnabled(bool state) { }

	// 	void SetContextMenuEnabled(bool state) { }

	// 	void SetViewSourceEnabled(bool state) { }

	// 	void NewWindowsOnly(bool state) { }

	// 	void PostChildPaint() { }

	// 	void AddHeader( const char pchHeader, const char pchValue) { }

	// 	void OnSetFocus() { }

	// 	void OnKillFocus() { }

	// 	void OnCommand( const char pchCommand) { }

	// 	void OnFileSelected( const char pchSelectedFile) { }

	// 	void OnFileSelectionCancelled() { }

	// 	void Find( const char pchSubStr) { }

	// 	void FindPrevious() { }

	// 	void FindNext() { }

	// 	void StopFind() { }

	// 	void OnEditNewLine(Panel pPanel) { }

	// 	void OnTextChanged(Panel pPanel) { }

	// 	void BrowserNeedsPaint(HTML_NeedsPaint pCallback) { }

	// 	bool OnStartRequest( const char url, const char target, const char pchPostData, bool bIsRedirect) { }

	// 	void BrowserStartRequest(HTML_StartRequest pCmd) { }

	// 	void BrowserURLChanged(HTML_URLChanged pCmd) { }

	// 	void BrowserFinishedRequest(HTML_FinishedRequest pCmd) { }

	// 	void BrowserOpenNewTab(HTML_OpenLinkInNewTab pCmd) { }

	// 	void BrowserPopupHTMLWindow(HTML_NewWindow pCmd) { }

	// 	void BrowserSetHTMLTitle(HTML_ChangedTitle pCmd) { }

	// 	void BrowserStatusText(HTML_StatusText pCmd) { }

	// 	void BrowserSetCursor(HTML_SetCursor pCmd) { }

	// 	void BrowserFileLoadDialog(HTML_FileOpenDialog pCmd) { }

	// 	void BrowserShowToolTip(HTML_ShowToolTip pCmd) { }

	// 	void BrowserUpdateToolTip(HTML_UpdateToolTip pCmd) { }

	// 	void BrowserHideToolTip(HTML_HideToolTip pCmd) { }

	// 	void BrowserSearchResults(HTML_SearchResults pCmd) { }

	// 	void BrowserClose(HTML_CloseBrowser pCmd) { }

	// 	void BrowserHorizontalScrollBarSizeResponse(HTML_HorizontalScroll pCmd) { }

	// 	void BrowserVerticalScrollBarSizeResponse(HTML_VerticalScroll pCmd) { }

	// 	void BrowserLinkAtPositionResponse(HTML_LinkAtPosition pCmd) { }

	// 	void BrowserJSAlert(HTML_JSAlert pCmd) { }

	// 	void BrowserJSConfirm(HTML_JSConfirm pCmd) { }

	// 	void DismissJSDialog(int bResult) { }

	// 	void BrowserCanGoBackandForward(HTML_CanGoBackAndForward pCmd) { }

	// 	void GetLinkAtPosition(int x, int y) { }

	// 	void UpdateSizeAndScrollBars() { }
}
