using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

class CheckButtonList : EditablePanel
{
	struct CheckItem
	{
		public CheckButton CheckButton;
		public KeyValues UserData;
	}

	List<CheckItem> CheckItems;
	ScrollBar ScrollBar;

	public CheckButtonList(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	// int AddItem(ReadOnlySpan<char> itemText, bool startsSelected, KeyValues userData) { }

	void RemoveAll() { }

	// int GetCheckedItemCount() { }

	public override void PerformLayout() { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	// bool IsItemIDValid(int itemID) { }

	// int GetHighestItemID() { }

	// KeyValues GetItemData(int itemID) { }

	// int GetItemCount() { }

	// bool IsItemChecked(int itemID) { }

	void SetItemCheckable(int itemID, bool state) { }

	void OnCheckButtonChecked(KeyValues _params) { }

	void OnScrollBarSliderMoved() { }

	public override void OnMouseWheeled(int delta) { }
}