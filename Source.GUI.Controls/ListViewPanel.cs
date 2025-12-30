using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class ListViewItem : Label
{
	Color FgColor1;
	Color FgColor2;
	Color BgColor;
	Color ArmedFgColor2;
	Color ArmedBgColor;
	Color SelectionBG2Color;
	KeyValues? Data;
	ListViewPanel ListViewPanel;
	bool Selected;

	public ListViewItem(Panel? parent) : base(parent, null, "") {
		ListViewPanel = (ListViewPanel)parent!;
		Data = null;
		Selected = false;
	}

	public void SetData(KeyValues data) => Data = data.MakeCopy();
	public override void OnMousePressed(ButtonCode code) => ListViewPanel.OnItemMousePressed(this, code);
	public override void OnMouseDoublePressed(ButtonCode code) => ListViewPanel.OnItemMouseDoublePressed(this, code);
	public KeyValues? GetData() => Data;

	public void SetSelected(bool selected) {
		if (Selected == selected)
			return;

		Selected = selected;

		if (selected)
			RequestFocus();

		UpdateImage();
		InvalidateLayout();
		Repaint();
	}

	public override void PerformLayout() {
		TextImage textImage = GetTextImage()!;
		if (Selected) {
			IPanel? focus = Input.GetFocus();
			if (HasFocus() || (focus != null && focus.HasParent(this)))
				textImage.SetColor(ArmedFgColor2);
			else
				textImage.SetColor(FgColor2);
		}
		else
			textImage.SetColor(GetFgColor());

		base.PerformLayout();
		Repaint();
	}


	public override void PaintBackground() {
		GetSize(out int wide, out int tall);

		if (Selected) {
			IPanel? focus = Input.GetFocus();
			if (HasFocus() || (focus != null && focus.HasParent(this)))
				Surface.DrawSetColor(ArmedBgColor);
			else
				Surface.DrawSetColor(SelectionBG2Color);
		}
		else
			Surface.DrawSetColor(GetBgColor());

		Surface.DrawFilledRect(0, 0, wide, tall);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		ArmedFgColor2 = GetSchemeColor("ListPanel.SelectedTextColor", scheme);
		ArmedBgColor = GetSchemeColor("ListPanel.SelectedBgColor", scheme);
		FgColor1 = GetSchemeColor("ListPanel.TextColor", scheme);
		FgColor2 = GetSchemeColor("ListPanel.SelectedTextColor", scheme);
		BgColor = GetSchemeColor("ListPanel.BgColor", scheme);
		BgColor = GetSchemeColor("ListPanel.TextBgColor", BgColor, scheme);
		SelectionBG2Color = GetSchemeColor("ListPanel.SelectedOutOfFocusBgColor", scheme);

		SetBgColor(BgColor);
		SetFgColor(FgColor1);

		UpdateImage();
	}

	public void UpdateImage() {
		if (ListViewPanel.ImageList != null) {
			int imageIndex = 0;

			if (Selected)
				imageIndex = Data!.GetInt("imageSelected", 0);

			if (imageIndex == 0)
				imageIndex = Data!.GetInt("image", 0);

			if (ListViewPanel.ImageList.IsValidIndex(imageIndex))
				SetImageAtIndex(0, ListViewPanel.ImageList.GetImage(imageIndex)!, 0);
			else
				SetImageAtIndex(0, ListViewPanel.ImageList.GetImage(1)!, 0);

			SizeToContents();
			InvalidateLayout();
		}
	}
}

public class ListViewPanel : Panel
{
	public delegate bool ListViewSortFunc_t(KeyValues kv1, KeyValues kv2);

	ScrollBar VBar;
	List<ListViewItem> DataItems = [];
	List<int> SortedItems = [];
	ListViewSortFunc_t? SortFunc;
	int RowHeight;
	IFont? Font;
	Color LabelFgColor;
	Color SelectionFgColor;
	List<int> SelectedItems = [];
	int LastSelectedItemID;
	int ShiftStartItemID;
	bool NeedsSort;
	bool DeleteImageListWhenDone;
	public ImageList? ImageList;

	static bool DefaultSortFunc(KeyValues kv1, KeyValues kv2) {
		ReadOnlySpan<char> s1 = kv1.GetString("text");
		ReadOnlySpan<char> s2 = kv2.GetString("text");
		return s1.CompareTo(s2, StringComparison.Ordinal) < 0;
	}


	public ListViewPanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		RowHeight = 20;
		NeedsSort = false;
		Font = null;
		ImageList = null;
		DeleteImageListWhenDone = false;
		SortFunc = DefaultSortFunc;
		ShiftStartItemID = -1;

		VBar = new(this, "HorizScrollBar", true);
		VBar.AddActionSignalTarget(this);
		VBar.SetVisible(false);
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		DeleteAllItems();
	}

	public int AddItem(KeyValues data, bool scrollToItem, bool SortOnAdd) {
		ListViewItem newItem = new(this);
		newItem.SetData(data);
		if (Font != null)
			newItem.SetFont(Font);

		int itemId = DataItems.Count;
		DataItems.Add(newItem);
		ApplyItemChanges(itemId);
		SortedItems.Add(itemId);

		if (SortOnAdd)
			NeedsSort = true;

		InvalidateLayout();

		if (scrollToItem)
			ScrollToItem(itemId);

		return itemId;
	}

	public void ScrollToItem(int itemID) {
		if (!VBar.IsVisible())
			return;

		int val = VBar.GetValue();

		GetSize(out int wide, out int tall);

		int maxWidth = GetItemsMaxWidth();
		int maxColVisible = wide / maxWidth;
		int itemsPerCol = GetItemsPerColumn();

		int itemIndex = SortedItems.IndexOf(itemID);
		int desiredCol = itemIndex / itemsPerCol;
		if (desiredCol < val || desiredCol >= (val + maxColVisible))
			VBar.SetValue(desiredCol);

		InvalidateLayout();
	}

	public int GetItemCount() => DataItems.Count;

	public KeyValues? GetItem(int itemID) {
		if (itemID < 0 || itemID >= DataItems.Count)
			return null;

		return DataItems[itemID].GetData();
	}

	public int GetItemIDFromPos(int pos) {
		if (pos < 0 || pos >= DataItems.Count)
			return -1;

		return SortedItems[pos];
	}

	public void ApplyItemChanges(int itemID) {
		if (itemID < 0 || itemID >= DataItems.Count)
			return;

		KeyValues kv = DataItems[itemID].GetData()!;
		ListViewItem label = DataItems[itemID];

		label.SetText(kv.GetString("text"));
		label.SetTextImageIndex(1);
		label.SetImagePreOffset(1, 5);

		TextImage TextImage = label.GetTextImage()!;
		TextImage.ResizeImageToContent();

		label.UpdateImage();
		label.SizeToContents();
		label.InvalidateLayout();
	}

	public void RemoveItem(int itemID) {
		if (itemID < 0 || itemID >= DataItems.Count)
			return;

		DataItems[itemID].MarkForDeletion();

		DataItems.RemoveAt(itemID);
		SortedItems.Remove(itemID);
		SelectedItems.Remove(itemID);

		InvalidateLayout();
	}

	public void DeleteAllItems() {
		for (int i = 0; i < DataItems.Count; i++)
			DataItems[i].MarkForDeletion();

		DataItems.Clear();
		SortedItems.Clear();
		SelectedItems.Clear();
	}

	public int InvalidItemID() => -1;
	public bool IsValidItemID(int itemID) => itemID >= 0 && itemID < DataItems.Count;

	public void SetSortFunc(ListViewSortFunc_t func) {
		SortFunc = func;
		SortList();
	}

	public void SortList() {
		SortedItems.Clear();

		for (int i = 0; i < DataItems.Count; i++) {
			if (SortFunc != null) {
				int insertionPoint;
				for (insertionPoint = 0; insertionPoint < SortedItems.Count; insertionPoint++) {
					if (!SortFunc(DataItems[i].GetData()!, DataItems[SortedItems[insertionPoint]].GetData()!))
						break;
				}
			}
			else
				SortedItems.Add(i);
		}
	}

	public void SetImageList(ImageList imageList, bool deleteImageListWhenDone) {
		DeleteImageListWhenDone = deleteImageListWhenDone;
		ImageList = imageList;

		foreach (var item in DataItems)
			item.UpdateImage();
	}

	public void SetFont(IFont? font) {
		Assert(font);
		if (font == null)
			return;

		Font = font;
		RowHeight = Surface.GetFontTall(font) + 1;

		foreach (var item in DataItems) {
			item.SetFont(font);
			TextImage textImage = item.GetTextImage()!;
			textImage.ResizeImageToContent();
			item.SizeToContents();
		}
	}

	public int GetSelectedItemsCount() => SelectedItems.Count;

	public int GetSelectedItem(int index) {
		if (index < 0 || index >= SelectedItems.Count)
			return -1;

		return SelectedItems[index];
	}

	public void ClearSelectedItems() {
		for (int i = 0; i < SelectedItems.Count; i++) {
			int itemID = SelectedItems[i];
			if (itemID >= 0 && itemID < DataItems.Count)
				DataItems[itemID].SetSelected(false);
		}
		SelectedItems.Clear();
	}

	static readonly KeyValues KV_ListViewItemSelected = new("ListViewItemSelected");
	public void AddSelectedItem(int itemID) {
		if (SelectedItems.Find(itemID) == -1) {
			SelectedItems.Add(itemID);
			DataItems[itemID].SetSelected(true);
			LastSelectedItemID = itemID;
			ShiftStartItemID = itemID;
			PostActionSignal(KV_ListViewItemSelected);
		}
	}

	public void SetSingleSelectedItem(int itemID) {
		ClearSelectedItems();
		AddSelectedItem(itemID);
	}

	public override void OnMouseWheeled(int delta) {
		int val = VBar.GetValue();
		val -= delta;
		VBar.SetValue(val);
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		base.OnSizeChanged(newWide, newTall);
		InvalidateLayout();
		Repaint();
	}

	public int GetItemsMaxWidth() {
		int maxWidth = 0;
		foreach (var item in DataItems) {
			item.GetSize(out int labelWide, out _);
			if (labelWide > maxWidth)
				maxWidth = labelWide + 25;
		}
		return maxWidth;
	}

	const int WINDOW_BORDER_WIDTH = 2;
	public override void PerformLayout() {
		if (NeedsSort)
			SortList();

		if (DataItems.Count == 0)
			return;

		GetSize(out int wide, out int tall);

		int maxWidth = GetItemsMaxWidth();
		if (maxWidth < 24)
			maxWidth = 24;
		int maxColVisible = wide / maxWidth;

		VBar.SetVisible(false);
		int itemsPerCol = GetItemsPerColumn();
		if (itemsPerCol < 1)
			itemsPerCol = 1;
		int cols = (GetItemCount() + itemsPerCol - 1) / itemsPerCol;

		int startItem = 0;
		if (cols > maxColVisible) {
			VBar.SetVisible(true);

			itemsPerCol = GetItemsPerColumn();
			cols = (GetItemCount() + itemsPerCol - 1) / itemsPerCol;

			VBar.SetEnabled(true);
			VBar.SetRangeWindow(maxColVisible);
			VBar.SetRange(0, cols);
			VBar.SetButtonPressedScrollValue(1);

			VBar.SetPos(0, tall - VBar.GetTall() + WINDOW_BORDER_WIDTH);
			VBar.SetSize(wide - (WINDOW_BORDER_WIDTH * 2), VBar.GetTall());
			VBar.InvalidateLayout();

			int val = VBar.GetValue();
			startItem += val * itemsPerCol;
		}
		else
			VBar.SetVisible(false);

		int lastItemVisible = startItem + ((maxColVisible + 1) * itemsPerCol) - 1;
		int itemsThisCol = 0;
		int x = 0;
		int y = 0;
		int i = 0;

		for (; i < SortedItems.Count; i++) {
			if (i >= startItem && i <= lastItemVisible) {
				DataItems[SortedItems[i]].SetVisible(false);
				DataItems[SortedItems[i]].SetPos(x, y);
				itemsThisCol++;
				if (itemsThisCol == itemsPerCol) {
					y = 0;
					x += maxWidth;
					itemsThisCol = 0;
				}
				else
					y += RowHeight;
			}
			else
				DataItems[SortedItems[i]].SetVisible(false);
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetBgColor(GetSchemeColor("ListPanel.BgColor", scheme));
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));

		LabelFgColor = GetSchemeColor("ListPanel.TextColor", scheme);
		SelectionFgColor = GetSchemeColor("ListPanel.SelectedTextColor", LabelFgColor, scheme);

		Font = scheme.GetFont("Default", IsProportional());
		SetFont(Font);
	}

	public override void OnMousePressed(ButtonCode code) {
		if (code == ButtonCode.MouseLeft || code == ButtonCode.MouseRight) {
			ClearSelectedItems();
			RequestFocus();

			if (code == ButtonCode.MouseRight)
				PostActionSignal(new KeyValues("OpenContextMenu", "itemID", -1));
		}
	}

	public void OnShiftSelect(int itemID) {
		if (ShiftStartItemID < 0 || ShiftStartItemID >= DataItems.Count)
			ShiftStartItemID = 0;

		int lowerPos = -1, upperPos = -1;
		int i = 0;
		for (; i < SortedItems.Count; i++) {
			if (SortedItems[i] == itemID) {
				lowerPos = i;
				upperPos = SortedItems.IndexOf(ShiftStartItemID);
				break;
			}
			else if (SortedItems[i] == ShiftStartItemID) {
				lowerPos = SortedItems.IndexOf(itemID);
				upperPos = i;
				break;
			}
		}

		Assert(lowerPos <= upperPos);

		if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl))
			ClearSelectedItems();

		for (i = lowerPos; i <= upperPos; i++) {
			DataItems[SortedItems[i]].SetSelected(true);
			SelectedItems.Add(SortedItems[i]);
			LastSelectedItemID = itemID;
		}
	}

	public void OnItemMousePressed(ListViewItem item, ButtonCode code) {
		int itemID = DataItems.IndexOf(item);
		if (itemID == -1)
			return;

		if (code == ButtonCode.MouseRight) {
			if (SelectedItems.Find(itemID) == -1) {
				ClearSelectedItems();
				AddSelectedItem(itemID);
			}

			PostActionSignal(new KeyValues("OpenContextMenu", "itemID", itemID));
		}
		else {
			if (Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift))
				OnShiftSelect(itemID);
			else if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl)) {
				if (SelectedItems.Find(itemID) != -1) {
					SelectedItems.Remove(itemID);
					item.SetSelected(false);
					ShiftStartItemID = itemID;
					LastSelectedItemID = itemID;
					DataItems[itemID].RequestFocus();
				}
				else
					AddSelectedItem(itemID);
			}
			else {
				ClearSelectedItems();
				AddSelectedItem(itemID);
			}
		}
	}

	public void OnItemMouseDoublePressed(ListViewItem item, ButtonCode code) {
		if (code == ButtonCode.MouseLeft)
			OnKeyCodeTyped(ButtonCode.KeyEnter);
	}

	public void FinishKeyPress(int itemID) {
		if (Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift))
			OnShiftSelect(itemID);
		else if (Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl)) {
			DataItems[itemID].RequestFocus();
			LastSelectedItemID = itemID;
		}
		else
			SetSingleSelectedItem(itemID);

		ScrollToItem(itemID);
	}
	public override void OnKeyCodeTyped(ButtonCode code) {
		if (DataItems.Count == 0)
			return;

		switch (code) {
			case ButtonCode.KeyHome: {
					if (SortedItems.Count > 0)
						FinishKeyPress(SortedItems[0]);
					break;
				}
			case ButtonCode.KeyEnd: {
					FinishKeyPress(SortedItems[^1]);
					break;
				}
			case ButtonCode.KeyUp: {
					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) - 1;
					if (itemPos < 0)
						itemPos = 0;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			case ButtonCode.KeyDown: {
					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) + 1;
					if (itemPos >= DataItems.Count)
						itemPos = DataItems.Count - 1;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			case ButtonCode.KeyLeft: {
					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) - GetItemsPerColumn();
					if (itemPos < 0)
						itemPos = 0;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			case ButtonCode.KeyRight: {
					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) + GetItemsPerColumn();
					if (itemPos >= SortedItems.Count)
						itemPos = SortedItems.Count - 1;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			case ButtonCode.KeyPageUp: {
					GetSize(out int wide, out _);

					int maxWidth = GetItemsMaxWidth();
					if (maxWidth == 0)
						maxWidth = wide;

					int maxColVisible = wide / maxWidth;
					int delta = maxColVisible * GetItemsPerColumn();

					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) - delta;
					if (itemPos < 0)
						itemPos = 0;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			case ButtonCode.KeyPageDown: {
					GetSize(out int wide, out int tall);

					int maxWidth = GetItemsMaxWidth();
					if (maxWidth == 0)
						maxWidth = wide;

					int maxColVisible = wide / maxWidth;
					int delta = maxColVisible * GetItemsPerColumn();

					int itemPos = SortedItems.FindIndex(x => x == LastSelectedItemID) + delta;
					if (itemPos >= SortedItems.Count)
						itemPos = SortedItems.Count - 1;

					FinishKeyPress(SortedItems[itemPos]);
					break;
				}
			default: {
					base.OnKeyCodeTyped(code);
					break;
				}
		}
	}

	public override void OnKeyTyped(char unichar) {
		if (!char.IsControl(unichar)) {
			Span<char> uniString = [unichar, '\0'];
			ReadOnlySpan<char> buff = stackalloc char[2];
			// convertunicodetoansi

			int i;
			int itemPos = SortedItems.IndexOf(LastSelectedItemID);
			if (itemPos > 0 && itemPos < SortedItems.Count - 1) {
				itemPos++;

				for (i = itemPos; i < SortedItems.Count; i++) {
					KeyValues? kv = DataItems[SortedItems[i]].GetData();
					ReadOnlySpan<char> text = kv!.GetString("text");
					if (MemoryExtensions.CompareTo(text, buff, StringComparison.Ordinal) == 0) {
						SetSingleSelectedItem(SortedItems[i]);
						ScrollToItem(SortedItems[i]);
						return;
					}
				}
			}

			for (i = 0; i < SortedItems.Count; i++) {
				if (i == itemPos)
					break;

				KeyValues? kv = DataItems[SortedItems[i]].GetData();
				ReadOnlySpan<char> text = kv!.GetString("text");
				if (MemoryExtensions.CompareTo(text, buff, StringComparison.Ordinal) == 0) {
					SetSingleSelectedItem(SortedItems[i]);
					ScrollToItem(SortedItems[i]);
					return;
				}
			}
		}
		else
			base.OnKeyTyped(unichar);
	}

	public void OnSliderMoved() {
		InvalidateLayout();
		Repaint();
	}

	public int GetItemsPerColumn() {
		GetSize(out _, out int tall);
		if (VBar.IsVisible())
			tall -= VBar.GetTall();

		return tall / RowHeight; // should round down
	}
}