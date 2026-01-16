using Source.Common.Formats.Keyvalues;
using Source.Common.Input;
using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class TagInfoLabel : URLLabel
{
	public TagInfoLabel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName, "", "") {

	}

	public TagInfoLabel(Panel? parent, ReadOnlySpan<char> panelName, ReadOnlySpan<char> text, ReadOnlySpan<char> url) : base(parent, panelName, text, url) {

	}

	public override void OnMousePressed(ButtonCode code) { }

	void DoOpenCustomServerInfoURL() { }
}

class TagMenuButton : MenuButton
{
	public TagMenuButton(Panel parent, ReadOnlySpan<char> panelName, ReadOnlySpan<char> text) : base(parent, panelName, text) {

	}

	public override void OnShowMenu(Menu menu) { }
}

class CustomServerInfoURLQuery : QueryBox
{
	public CustomServerInfoURLQuery(ReadOnlySpan<char> title, ReadOnlySpan<char> queryText, Panel parent) : base(title, queryText, parent) => SetOKButtonText("#ServerBrowser_CustomServerURLButton");
}

class CustomGames : InternetGames
{
	const int MAX_TAG_CHARACTERS = 128;

	TagInfoLabel TagInfoURL;
	TagMenuButton AddTagList;
	Menu TagListMenu;
	TextEntry TagFilter;

	public CustomGames(Panel parent) : base(parent, "CustomGames", PageType.InternetServer) {

	}

	void UpdateDerivedLayouts() { }

	public override void OnLoadFilter(KeyValues filter) { }

	public override bool CheckTagFilter(gameserveritem_t server) {
		throw new NotImplementedException();
	}

	public override bool CheckWorkshopFilter(gameserveritem_t server) {
		throw new NotImplementedException();
	}

	public override void OnSaveFilter(KeyValues filter) { }

	public override void SetRefreshing(bool state) { }

	public override void ServerResponded(int server, gameserveritem_t serverItem) { }

	void RecalculateCommonTags() { }

	void OnTagMenuButtonOpened() { }

	void OnAddTag(KeyValues _params) { }

	void AddTagToFilterList(ReadOnlySpan<char> tag) { }
}