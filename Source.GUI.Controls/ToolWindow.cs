using Source.Common.Input;

namespace Source.GUI.Controls;

// So that an app can have a "custom" tool window class created during window drag/drop operations on the property sheet
abstract class IToolWindowFactory
{
	public abstract ToolWindow InstanceToolWindow(
		Panel parent,
		bool contextLabel,
		Panel firstPage,
		string title,
		bool contextMenu
	);
}

class ToolWindow : Frame
{
	static readonly List<ToolWindow> ToolWindows = [];

	PropertySheet PropertySheet;
	IToolWindowFactory? Factory;

	public ToolWindow(Panel parent, bool contextLabel, IToolWindowFactory? factory = null, Panel? page = null, string? title = null, bool contextMenu = false, bool inGlobalList = true) : base(parent, "ToolWindow") {
		Factory = factory;

		if (inGlobalList)
			ToolWindows.Add(this);

		PropertySheet = new(this, "ToolWindowSheet", true);
		PropertySheet.ShowContextButtons(contextLabel);
		PropertySheet.AddPage(page, title, null, contextMenu);
		PropertySheet.AddActionSignalTarget(this);
		PropertySheet.SetSmallTabs(true);
		PropertySheet.SetKBNavigationEnabled(false);

		SetSmallCaption(true);

		SetMenuButtonResponsive(false);
		SetMinimizeButtonVisible(false);
		SetCloseButtonVisible(true);
		SetMoveable(true);
		SetSizeable(true);

		SetClipToParent(false);
		SetVisible(true);

		SetDeleteSelfOnClose(true);

		SetTitle("", false);
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		PropertySheet.RemoveAllPages();
		ToolWindows.Remove(this);
	}

	int GetToolWindowCount() => ToolWindows.Count;
	ToolWindow GetToolWindow(int index) => ToolWindows[index];
	bool IsDraggableTabContainer() => PropertySheet.IsDraggableTab();
	PropertySheet GetPropertySheet() => PropertySheet;
	Panel? GetActivePage() => PropertySheet.GetActivePage();
	void SetActivePage(Panel page) => PropertySheet.SetActivePage(page);
	void AddPage(Panel page, ReadOnlySpan<char> title, bool contextMenu) => PropertySheet.AddPage(page, title, null, contextMenu);

	void RemovePage(Panel page) {
		PropertySheet.RemovePage(page);
		if (PropertySheet.GetNumPages() == 0)
			MarkForDeletion();
	}

	public override void PerformLayout() {
		base.PerformLayout();

		GetClientArea(out int x, out int y, out int w, out int h);
		PropertySheet.SetBounds(x, y, w, h);
		PropertySheet.InvalidateLayout();
		Repaint();
	}

	public override void ActivateBuildMode() {
		EditablePanel? panel = (EditablePanel?)GetActivePage();
		if (panel == null)
			return;

		panel.ActivateBuildMode();
	}

	public override void RequestFocus(int direction) => PropertySheet.RequestFocus(direction);
	void SetToolWindowFactory(IToolWindowFactory factory) => Factory = factory;
	IToolWindowFactory? GetToolWindowFactory() => Factory;

	void Grow(int edge = 0, int from_x = -1, int from_y = -1) {
		int status_h = 24;
		int menubar_h = 27;

		Surface.GetScreenSize(out int sw, out int sh);

		GetBounds(out int old_x, out int old_y, out int old_w, out int old_h);

		int new_x, new_y, new_w, new_h;
		new_x = old_x;
		new_y = old_y;
		new_w = old_w;
		new_h = old_h;

		int c = GetToolWindowCount();

		if ((edge == 0) || (edge == 1)) {
			if (from_y >= 0) {
				old_h -= from_y - old_y;
				old_y = from_y;
			}

			new_h = old_h + (old_y - menubar_h);
			new_y = menubar_h;

			for (int i = 0; i < c; ++i) {
				ToolWindow tw = GetToolWindow(i);
				Assert(tw);
				if ((tw == null) || (tw == this))
					continue;

				tw.GetBounds(out int x, out int y, out int w, out int h);

				if ((((old_x > x) && (old_x < x + w))
					|| ((old_x + old_w > x) && (old_x + old_w < x + w))
					|| ((old_x <= x) && old_x + old_w >= x + w))
					&& ((old_y >= y + h) && (new_y < y + h))) {
					new_h = old_h + (old_y - (y + h));
					new_y = y + h;
				}
			}

			old_h = new_h;
			old_y = new_y;
		}

		if ((edge == 0) || (edge == 2)) {
			if (from_x >= 0) {
				old_w = from_x - old_x;
			}

			new_w = sw - old_x;

			for (int i = 0; i < c; ++i) {
				ToolWindow tw = GetToolWindow(i);
				Assert(tw);
				if ((tw == null) || (tw == this))
					continue;

				tw.GetBounds(out int x, out int y, out int w, out int h);

				if ((((old_y > y) && (old_y < y + h))
					|| ((old_y + old_h > y) && (old_y + old_h < y + h))
					|| ((old_y <= y) && old_y + old_h >= y + h))
					&& ((old_x + old_w <= x) && (new_w > x - old_x))) {
					new_w = x - old_x;
				}
			}

			old_w = new_w;
		}

		if ((edge == 0) || (edge == 3)) {
			if (from_y >= 0)
				old_h = from_y - old_y;

			new_h = sh - old_y - status_h;

			for (int i = 0; i < c; ++i) {
				ToolWindow tw = GetToolWindow(i);
				Assert(tw);
				if ((tw == null) || (tw == this))
					continue;

				tw.GetBounds(out int x, out int y, out int w, out int h);

				if ((((old_x > x) && (old_x < x + w))
					|| ((old_x + old_w > x) && (old_x + old_w < x + w))
					|| ((old_x <= x) && old_x + old_w >= x + w))
					&& ((old_y + old_h <= y) && (new_h > y - old_y))) {
					new_h = y - old_y;
				}
			}

			old_h = new_h;
		}

		// grow left
		if ((edge == 0) || (edge == 4)) {
			if (from_x >= 0) {
				old_w -= from_x - old_x;
				old_x = from_x;
			}

			new_w = old_w + old_x;
			new_x = 0;

			for (int i = 0; i < c; ++i) {
				ToolWindow tw = GetToolWindow(i);
				Assert(tw);
				if ((tw == null) || (tw == this))
					continue;

				tw.GetBounds(out int x, out int y, out int w, out int h);

				if ((((old_y > y) && (old_y < y + h))
					|| ((old_y + old_h > y) && (old_y + old_h < y + h))
					|| ((old_y <= y) && old_y + old_h >= y + h))
					&& ((old_x >= x + w) && (new_x < x + w))) {
					new_w = old_w + (old_x - (x + w));
					new_x = x + w;
				}
			}

			old_w = new_w;
			old_x = new_x;
		}

		SetBounds(new_x, new_y, new_w, new_h);
	}

	void GrowFromClick() {
		Input.GetCursorPos(out int mx, out int my);

		int esz, csz, brsz, ch;
		esz = GetDraggerSize();
		csz = GetCornerSize();
		brsz = GetBottomRightSize();
		ch = GetCaptionHeight();

		GetBounds(out int x, out int y, out int w, out int h);

		// upper right
		if ((mx > x + w - csz - 1) && (my < y + csz)) {
			Grow(1);
			Grow(2);
		}
		// lower right (the big one)
		else if ((mx > x + w - brsz - 1) && (my > y + h - brsz - 1)) {
			Grow(2);
			Grow(3);
		}
		// lower left
		else if ((mx < x + csz) && (my > y + h - csz - 1)) {
			Grow(3);
			Grow(4);
		}
		// upper left
		else if ((mx < x + csz) && (my < y + csz)) {
			Grow(4);
			Grow(1);
		}
		// top edge
		else if (my < y + esz)
			Grow(1);
		// right edge
		else if (mx > x + w - esz - 1)
			Grow(2);
		// bottom edge
		else if (my > y + h - esz - 1)
			Grow(3);
		// left edge
		else if (mx < x + esz)
			Grow(4);
		// otherwise (if over the grab bar), grow all edges (from the clicked point)
		else if (my < y + ch)
			Grow(0, mx, my);
	}

	public override void OnMouseDoublePressed(ButtonCode code) => GrowFromClick();

	public override void OnMousePressed(ButtonCode code) {
		if (code == ButtonCode.MouseMiddle)
			GrowFromClick();
		else
			base.OnMousePressed(code);
	}
}