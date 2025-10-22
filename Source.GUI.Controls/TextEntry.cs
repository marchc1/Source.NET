using CommunityToolkit.HighPerformance;

using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class TextEntry : Panel
{
	public static Panel Create_TextEntry() => new TextEntry(null, null);
	public TextEntry(Panel? parent, string? name) : base(parent, name) {
		SetTriplePressAllowed(true);

		CursorBlinkRate = 400;
		MaxCharCount = -1;
		Select[0] = Select[1] = -1;

		ResetCursorBlink();
		SetCursor(CursorCode.IBeam);
		SetEditable(true);
		RecalculateBreaksIndex = 0;
		ShouldSelectAllOnFirstFocus = false;
		ShouldSelectAllOnFocusAlways = false;

		GotoTextEnd();
		SetAllowKeyBindingChainToParent(true);
	}

	public override void OnKeyFocusTicked() {
		base.OnKeyFocusTicked();
		long time = System.GetTimeMillis();
		if (time > CursorNextBlinkTime) {
			CursorBlink = !CursorBlink;
			CursorNextBlinkTime = time + CursorBlinkRate;
			Repaint();
		}
	}
	const int DRAW_OFFSET_X = 3;
	const int DRAW_OFFSET_Y = 1;
	public int GetYStart() => Multiline ? DRAW_OFFSET_Y : (GetTall() / 2) - (Surface.GetFontTall(Font) / 2);

	public override void PaintBackground() {
		base.PaintBackground();

		Color col = IsEnabled() ? GetBgColor() : DisabledBgColor;
		Color saveBgColor = col;

		GetSize(out int wide, out int tall);

		int x = DRAW_OFFSET_X + PixelsIndent, y = GetYStart();

		LangInset = 0;

		int langlen = 0;
		if (AllowNonAsciiCharacters) {
			// TODO: IME related things
		}

		IFont? useFont = Font;

		Surface.DrawSetTextFont(useFont);
		if (IsEnabled())
			col = GetFgColor();
		else
			col = DisabledFgColor;

		Surface.DrawSetTextColor(col);
		PixelsIndent = 0;

		int lineBreakIndexIndex = 0;
		int startIndex = GetStartDrawIndex(ref lineBreakIndexIndex);
		int remembery = y;

		int oldEnd = TextStream.Count;
		int oldCursorPos = CursorPos;
		int nCompStart = -1;
		int nCompEnd = -1;

		bool composing = false; // todo
		bool invertcomposition = Input.GetShouldInvertCompositionString();

		bool highlight_composition = (nCompStart != -1 && nCompEnd != -1) ? true : false;

		if ((!Multiline) && (!HorizScrollingAllowed)) {
			int endIndex = TextStream.Count;
			if ((!HasFocus() && IsEditable()) || (!IsEditable())) {
				int i1 = -1;

				bool addEllipses = NeedsEllipses(useFont, ref i1);
				if (addEllipses && !IsEditable() && UseFallbackFont && FallbackFont != null) {
					useFont = FallbackFont;
					Surface.DrawSetTextFont(useFont);
					addEllipses = NeedsEllipses(useFont, ref i1);
				}
				if (addEllipses) {
					int elipsisWidth = 3 * getCharWidth(useFont, '.');
					while (elipsisWidth > 0 && i1 >= 0) {
						elipsisWidth -= getCharWidth(useFont, TextStream[i1]);
						i1--;
					}
					endIndex = i1 + 1;
				}

				if (TextStream.Count - endIndex < 3 && TextStream.Count - endIndex > 0)
					endIndex = TextStream.Count - 3;
			}

			int i;
			for (i = startIndex; i < endIndex; i++) {
				char ch = TextStream[i];
				if (HideText)
					ch = '*';

				bool iscompositionchar = false;

				if (highlight_composition) {
					iscompositionchar = (i >= nCompStart && i < nCompEnd) ? true : false;
					if (iscompositionchar) {
						Surface.DrawSetColor(col);

						int w = getCharWidth(useFont, ch);

						if (invertcomposition) {
							Surface.DrawSetTextColor(saveBgColor);
							Surface.DrawSetColor(col);

							Surface.DrawFilledRect(x, 0, x + w, tall);
							Surface.DrawSetColor(saveBgColor);
						}

						Surface.DrawFilledRect(x, tall - 2, x + w, tall - 1);
					}
				}

				x += DrawChar(ch, useFont, i, x, y);

				Surface.DrawSetTextColor(col);

			}
			if (endIndex < TextStream.Count) {
				x += DrawChar('.', useFont, i++, x, y);
				x += DrawChar('.', useFont, i++, x, y);
				x += DrawChar('.', useFont, i++, x, y);
			}
		}
		else {
			for (int i = startIndex; i < TextStream.Count; i++) {
				char ch = TextStream[i];
				if (HideText)
					ch = '*';

				if (Multiline && LineBreaks[lineBreakIndexIndex] == i) {
					AddAnotherLine(ref x, ref y);
					lineBreakIndexIndex++;
				}

				bool iscompositionchar = false;

				if (highlight_composition) {
					iscompositionchar = (i >= nCompStart && i < nCompEnd) ? true : false;
					if (iscompositionchar) {
						Surface.DrawSetColor(col);

						int w = getCharWidth(useFont, ch);

						if (invertcomposition) {
							Surface.DrawSetTextColor(saveBgColor);
							Surface.DrawFilledRect(x, 0, x + w, tall);
							Surface.DrawSetColor(saveBgColor);
						}

						Surface.DrawFilledRect(x, tall - 2, x + w, tall - 1);
					}
				}

				x += DrawChar(ch, useFont, i, x, y);

				Surface.DrawSetTextColor(col);
			}
		}

		Surface.DrawSetColor(50, 50, 50, 255);

		if (IsEnabled() && IsEditable() && HasFocus()) {
			Surface.DrawSetColor(0, 0, 0, 255);

			DrawCursor(x, y);

			if (composing) {
				LocalToScreen(ref x, ref y);
				Input.SetCandidateWindowPos(x, y);
			}
		}

		int newEnd = TextStream.Count;
		int remove = newEnd - oldEnd;
		if (remove > 0)
			TextStream.RemoveRange(oldCursorPos, remove);

		CursorPos = oldCursorPos;

		if (HasFocus() && AllowNonAsciiCharacters && langlen > 0) {
			wide += LangInset;

			if (DrawLanguageIDAtLeft)
				x = 0;
			else
				x = wide - LangInset;


			Surface.DrawSetColor(col);

			Surface.DrawFilledRect(x, 2, x + LangInset - 2, tall - 2);

			saveBgColor[3] = 255;
			Surface.DrawSetTextColor(saveBgColor);

			x += 1;

			Surface.DrawSetTextFont(SmallFont);
			for (int i = 0; i < langlen; ++i) {
				// x += DrawChar(shortcode[i], SmallFont, i, x, remembery);
			}
		}
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		FlushLineBreaks(false);

		if (newWide > DrawWidth)
			ScrollLeftForResize();

		base.OnSizeChanged(newWide, newTall);
		DrawWidth = newWide;
		InvalidateLayout();
	}

	private void ScrollLeftForResize() {
		if (Multiline)
			return;

		if (!HorizScrollingAllowed)
			return;

		while (CurrentStartIndex > 0) {
			CurrentStartIndex--;
			int val = CurrentStartIndex;

			if (IsCursorOffRightSideOfWindow(CursorPos)) {
				CurrentStartIndex++;
				break;
			}

			if (val != CurrentStartIndex)
				break;
		}

		LayoutVerticalScrollBarSlider();
	}

	public int GetDrawWidth() => DrawWidth;
	public void SetDrawWidth(int width) => DrawWidth = width;

	private bool NeedsEllipses(IFont? font, ref int index) {
		index = -1;
		int wide = DRAW_OFFSET_X;
		for (int i = 0; i < TextStream.Count; ++i) {
			wide += getCharWidth(font, TextStream[i]);
			if (wide > DrawWidth) {
				index = i;
				return true;
			}
		}
		return false;
	}

	public void CursorToPixelSpace(int cursorPos, out int x, out int y) {
		int yStart = GetYStart();

		x = DRAW_OFFSET_X;
		y = yStart;
		PixelsIndent = 0;
		int lineBreakIndexIndex = 0;

		for (int i = GetStartDrawIndex(ref lineBreakIndexIndex); i < TextStream.Count; i++) {
			char ch = TextStream[i];
			if (HideText)
				ch = '*';

			if (cursorPos == i)
				break;


			if (LineBreaks.Count > 0 && lineBreakIndexIndex < LineBreaks.Count && LineBreaks[lineBreakIndexIndex] == i) {
				AddAnotherLine(ref x, ref y);
				lineBreakIndexIndex++;
			}

			x += getCharWidth(Font, ch);
		}

		if (DrawLanguageIDAtLeft)
			x += LangInset;

	}

	private void AddAnotherLine(ref int x, ref int y) {
		x = DRAW_OFFSET_X + PixelsIndent;
		y += Surface.GetFontTall(Font) + DRAW_OFFSET_Y;
	}

	private bool DrawCursor(int x, int y) {
		if (!CursorBlink) {
			CursorToPixelSpace(CursorPos, out int cx, out int cy);
			Surface.DrawSetColor(CursorColor);
			int fontTall = Surface.GetFontTall(Font);
			Surface.DrawFilledRect(cx, cy, cx + 1, cy + fontTall);
			return true;
		}

		return false;
	}

	int GetCursorLine() {
		int cursorLine;
		for (cursorLine = 0; cursorLine < LineBreaks.Count; cursorLine++)
			if (CursorPos < LineBreaks[cursorLine])
				break;

		if (PutCursorAtEnd)
			if (CursorPos != TextStream.Count)
				cursorLine--;


		return cursorLine;
	}

	private int GetStartDrawIndex(ref int lineBreakIndexIndex) {
		int startIndex = 0;

		int numLines = LineBreaks.Count;
		int startLine = 0;

		if (VertScrollBar != null && !MouseDragSelection)
			startLine = VertScrollBar.GetValue();
		else {
			IFont? font = Font;
			int displayLines = GetTall() / (Surface.GetFontTall(font) + DRAW_OFFSET_Y);
			if (displayLines < 1) {
				displayLines = 1;
			}
			if (numLines > displayLines) {
				int cursorLine = GetCursorLine();

				startLine = CurrentStartLine;

				if (cursorLine < CurrentStartLine) {
					startLine = cursorLine;
					if (VertScrollBar != null) {
						MoveScrollBar(1);
						startLine = VertScrollBar.GetValue();
					}
				}
				else if (cursorLine > (CurrentStartLine + displayLines - 1)) {
					startLine = cursorLine - displayLines + 1;
					if (VertScrollBar != null) {
						MoveScrollBar(-1);
						startLine = VertScrollBar.GetValue();
					}
				}
			}
			else if (!Multiline) {
				bool done = false;
				while (!done) {
					done = true;
					int x = DRAW_OFFSET_X;
					for (int i = CurrentStartIndex; i < TextStream.Count; i++) {
						done = false;
						char ch = TextStream[i];
						if (HideText)
							ch = '*';

						if (CursorPos == i)
							break;

						x += getCharWidth(font, ch);
					}

					if (x >= GetWide()) {
						CurrentStartIndex++;
						continue;
					}

					if (x <= 0) {
						if (CurrentStartIndex > 0)
							CurrentStartIndex--;
					}

					break;
				}
			}
		}

		if (startLine > 0) {
			lineBreakIndexIndex = startLine;
			if (startLine != 0 && startLine < LineBreaks.Count) {
				startIndex = LineBreaks[startLine - 1];
			}
		}

		if (!HorizScrollingAllowed)
			return 0;

		CurrentStartLine = startLine;
		if (Multiline)
			return startIndex;
		else
			return CurrentStartIndex;

	}

	private void MoveScrollBar(int delta) {
		if (VertScrollBar != null) {
			int val = VertScrollBar.GetValue();
			val -= (delta * 3);
			VertScrollBar.SetValue(val);
		}
	}

	int getCharWidth(IFont? font, char ch) {
		if (!char.IsControl(ch)) {
			Surface.GetCharABCwide(font, ch, out int a, out int b, out int c);
			return a + b + c;
		}
		return 0;
	}
	int DrawChar(char ch, IFont? font, int index, int x, int y) {
		int charWide = getCharWidth(font, ch);
		int fontTall = Surface.GetFontTall(font);
		if (!char.IsControl(ch)) {
			int selection0 = -1, selection1 = -1;
			if (GetSelectedRange(out selection0, out selection1) && index >= selection0 && index < selection1) {
				IPanel? focus = Input.GetFocus();
				Color bgColor;
				bool hasFocus = HasFocus();
				bool childOfFocus = focus != null && focus.HasParent(this);

				if (hasFocus || childOfFocus)
					bgColor = SelectionColor;
				else
					bgColor = DefaultSelectionBG2Color;

				Surface.DrawSetColor(bgColor);
				Surface.DrawFilledRect(x, y, x + charWide, y + 1 + fontTall);
				Surface.DrawSetTextColor(SelectionTextColor);
			}
			if (index == selection1)
				Surface.DrawSetTextColor(GetFgColor());

			Surface.DrawSetTextPos(x, y);
			Surface.DrawChar(ch);

			return charWide;
		}

		return 0;
	}

	List<char> TextStream = [];
	List<char> UndoTextStream = [];
	List<int> LineBreaks = [];

	int CursorPos;
	bool CursorIsAtEnd;
	bool PutCursorAtEnd;
	int UndoCursorPos;
	bool CursorBlink;
	bool HideText;
	bool Editable;
	bool MouseSelection;
	bool MouseDragSelection;
	long CursorNextBlinkTime;
	int CursorBlinkRate;
	int PixelsIndent;
	int CharCount;
	int MaxCharCount;
	bool DataChanged;
	bool Multiline;
	bool VerticalScrollbar;
	ScrollBar? VertScrollBar;
	int CurrentStartLine;
	int CurrentStartIndex;
	bool HorizScrollingAllowed;
	bool CatchEnterKey;
	bool Wrap;
	bool SendNewLines;
	int DrawWidth;
	int[] Select = new int[2];
	Menu? EditMenu;
	int RecalculateBreaksIndex;
	bool ShouldSelectAllOnFirstFocus;
	bool ShouldSelectAllOnFocusAlways;
	Color CursorColor;
	Color DisabledFgColor, DisabledBgColor;
	Color SelectionTextColor, SelectionColor, DefaultSelectionBG2Color, FocusEdgeColor;
	IFont? Font, SmallFont;
	bool AllowNonAsciiCharacters;
	bool AutoProgressOnHittingCharLimit;
	bool AllowNumericInputOnly;
	int LangInset;
	bool DrawLanguageIDAtLeft;
	bool UseFallbackFont;
	IFont? FallbackFont;

	public void SetWrap(bool state) {
		Wrap = state;
	}

	public void SendNewLine(bool state) {
		SendNewLines = state;
	}

	public override void OnCursorMoved(int x, int y)
	{
		if (!MouseSelection)
			return;

		ScreenToLocal(ref x, ref y);
		CursorPos = PixelToCursorSpace(x, y);

		if (CursorPos == 0)
			PutCursorAtEnd = false;

		if (CursorPos == CurrentStartIndex)
		{
			if (CursorPos > 0)
				CursorPos--;

			ScrollLeft();
			CursorPos = CurrentStartIndex;
		}

		if (CursorPos != Select[1])
		{
			Select[1] = CursorPos;
			Repaint();
		}
	}

	public override void OnMousePressed(ButtonCode code) {
		if (code == ButtonCode.MouseLeft) {
			bool keepChecking = SelectCheck(true);
			if (!keepChecking) {
				base.OnMousePressed(code);
				return;
			}

			Input.GetCursorPos(out int x, out int y);
			ScreenToLocal(ref x, ref y);

			CursorIsAtEnd = PutCursorAtEnd;
			CursorPos = PixelToCursorSpace(x, y);
			if (CursorPos == 0)
				PutCursorAtEnd = false;

			Input.SetMouseCapture(this);
			MouseSelection = true;

			if (Select[0] < 0)
				Select[0] = CursorPos;

			Select[1] = CursorPos;

			ResetCursorBlink();
			RequestFocus();
			Repaint();
		}
		else if (code == ButtonCode.MouseRight) {
			//CreateEditMenu();
			//Assert(EditMenu);

			//OpenEditMenu();
		}
	}

	public override void OnMouseReleased(ButtonCode code) {
		MouseSelection = false;
		Input.SetMouseCapture(null);
		if (GetSelectedRange(out int cx0, out int cx1)) {
			if (cx1 - cx0 == 0)
				Select[0] = -1;
		}
	}

	public override void OnMouseTriplePressed(ButtonCode code) {
		base.OnMouseTriplePressed(code);

		if (code == ButtonCode.MouseLeft)
		{
			GotoTextEnd();
			SelectAllText(false);
		}
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		if (code == ButtonCode.MouseLeft)
		{
			OnMousePressed(code);
			int selectSpotStart, selectSpotEnd;
			GotoWordLeft();
			selectSpotStart = CursorPos;
			GotoWordRight();
			selectSpotEnd = CursorPos;

			if (CursorPos > 0)
			{
				if (char.IsWhiteSpace(TextStream[CursorPos - 1]))
				{
					selectSpotEnd--;
					CursorPos--;
				}

				Select[0] = selectSpotStart;
				Select[1] = selectSpotEnd;
				MouseSelection = true;
			}
		}
	}

	public void SetCatchEnterKey(bool state) {
		CatchEnterKey = state;
	}

	public void SetVerticalScrollbar(bool state) {
		VerticalScrollbar = state;

		if (VerticalScrollbar) {
			if (VertScrollBar == null) {
				VertScrollBar = new(this, "ScrollBar", true);
				VertScrollBar.AddActionSignalTarget(this);
			}

			VertScrollBar.SetVisible(true);
		}
		else if (VertScrollBar != null)
			VertScrollBar.SetVisible(false);

		InvalidateLayout();
	}

	public override void OnKeyCodePressed(ButtonCode code) {
		if (code == ButtonCode.KeyEnter) {
			if (!CatchEnterKey) {
				base.OnKeyCodePressed(code);
				return;
			}
		}

		switch (code) {
			case ButtonCode.KeyF1:
			case ButtonCode.KeyF2:
			case ButtonCode.KeyF3:
			case ButtonCode.KeyF4:
			case ButtonCode.KeyF5:
			case ButtonCode.KeyF6:
			case ButtonCode.KeyF7:
			case ButtonCode.KeyF8:
			case ButtonCode.KeyF9:
			case ButtonCode.KeyF10:
			case ButtonCode.KeyF11:
			case ButtonCode.KeyF12:
			case ButtonCode.KeyEscape:
			case ButtonCode.KeyApp:
				base.OnKeyCodePressed(code);
				return;
		}

		if (code.IsMouseCode()) {
			base.OnKeyCodePressed(code);
			return;
		}
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		CursorIsAtEnd = PutCursorAtEnd;
		PutCursorAtEnd = false;

		bool shift = Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift);
		bool ctrl = Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl);
		bool alt = Input.IsKeyDown(ButtonCode.KeyLAlt) || Input.IsKeyDown(ButtonCode.KeyRAlt);
		bool winkey = Input.IsKeyDown(ButtonCode.KeyLWin) || Input.IsKeyDown(ButtonCode.KeyRWin);
		bool fallThrough = false;

		if ((ctrl || (winkey && IsOSX())) && !alt) {
			switch (code) {
				case ButtonCode.KeyA:
					SelectAllText(false);
					CursorPos = Select[1];
					break;

				case ButtonCode.KeyInsert:
				case ButtonCode.KeyC:
					CopySelected();
					break;

				case ButtonCode.KeyV:
					DeleteSelected();
					Paste();
					break;

				case ButtonCode.KeyX:
					CopySelected();
					DeleteSelected();
					break;

				case ButtonCode.KeyZ:
					Undo();
					break;

				case ButtonCode.KeyRight:
					GotoWordRight();
					break;

				case ButtonCode.KeyLeft:
					GotoWordLeft();
					break;

				case ButtonCode.KeyEnter:
					if (Multiline) {
						DeleteSelected();
						SaveUndoState();
						InsertChar('\n');
					}

					if (SendNewLines)
						PostActionSignal(TextNewLineActionSignal);

					break;

				case ButtonCode.KeyHome:
					GotoTextStart();
					break;

				case ButtonCode.KeyEnd:
					GotoTextEnd();
					break;

				default:
					fallThrough = true;
					break;

			}
		}
		else if (alt) {
			if (!AllowNonAsciiCharacters || (code != ButtonCode.KeyBackquote))
				fallThrough = true;
		}
		else {
			switch (code) {
				case ButtonCode.KeyTab:
				case ButtonCode.KeyLShift:
				case ButtonCode.KeyRShift:
				case ButtonCode.KeyEscape:
					fallThrough = true;

					break;
				case ButtonCode.KeyInsert:
					if (shift) {
						DeleteSelected();
						Paste();
					}
					else
						fallThrough = true;
					break;
				case ButtonCode.KeyDelete:
					if (shift) {
						CopySelected();
						DeleteSelected();
					}
					else {
						Delete();
					}
					break;
				case ButtonCode.KeyLeft: {
						GotoLeft();
					}
					break;
				case ButtonCode.KeyRight: {
						GotoRight();
					}
					break;
				case ButtonCode.KeyUp:
					if (Multiline)
						GotoUp();
					else
						fallThrough = true;
					break;
				case ButtonCode.KeyDown:
					if (Multiline)
						GotoDown();
					else
						fallThrough = true;
					break;
				case ButtonCode.KeyHome:
					if (Multiline) {
						GotoFirstOfLine();
					}
					else {
						GotoTextStart();
					}
					break;

				case ButtonCode.KeyEnd:
					GotoEndOfLine();
					break;

				case ButtonCode.KeyBackspace:
					if (GetSelectedRange(out int x0, out int x1))
						DeleteSelected();
					else
						Backspace();

					break;
				case ButtonCode.KeyEnter:
					if (Multiline && CatchEnterKey) {
						DeleteSelected();
						SaveUndoState();
						InsertChar('\n');
					}
					else
						fallThrough = true;

					if (SendNewLines)
						PostActionSignal(TextNewLineActionSignal);

					break;


				// TODO: PageUp
				// TODO: PageDown

				case ButtonCode.KeyF1:
				case ButtonCode.KeyF2:
				case ButtonCode.KeyF3:
				case ButtonCode.KeyF4:
				case ButtonCode.KeyF5:
				case ButtonCode.KeyF6:
				case ButtonCode.KeyF7:
				case ButtonCode.KeyF8:
				case ButtonCode.KeyF9:
				case ButtonCode.KeyF10:
				case ButtonCode.KeyF11:
				case ButtonCode.KeyF12: {
						fallThrough = true;
						break;
					}

				default:
					return;
			}
		}

		Select[1] = CursorPos;

		if (DataChanged)
			FireActionSignal();

		if (fallThrough) {
			PutCursorAtEnd = CursorIsAtEnd;
			base.OnKeyCodeTyped(code);
		}
	}

	public override void OnKeyTyped(char ch) {
		CursorIsAtEnd = PutCursorAtEnd;
		PutCursorAtEnd = false;

		bool fallThrough = false;

		if (char.IsControl(ch) || ch == 9)
			return;

		if (!IsEditable()) {
			base.OnKeyTyped(ch);
			return;
		}

		if (ch != 0) {
			DeleteSelected();
			SaveUndoState();
			InsertChar(ch);
		}

		Select[1] = CursorPos;

		if (DataChanged)
			FireActionSignal();

		if (fallThrough) {
			PutCursorAtEnd = CursorIsAtEnd;
			base.OnKeyTyped(ch);
		}
	}

	public void GotoTextEnd() {
		SelectCheck();
		CursorPos = TextStream.Count;
		PutCursorAtEnd = true;
		ScrollRight();

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void GotoWordLeft() {
		SelectCheck();

		if (CursorPos < 1)
			return;

		while (--CursorPos >= 0)
			if (!char.IsWhiteSpace(TextStream[CursorPos]))
				break;


		while (--CursorPos >= 0)
			if (char.IsWhiteSpace(TextStream[CursorPos]))
				break;

		CursorPos++;

		ScrollLeft();

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void ScrollLeft() {
		if (Multiline)
			return;

		if (!HorizScrollingAllowed)
			return;

		if (CursorPos < CurrentStartIndex) {
			if (CursorPos < 0)
				CursorPos = 0;

			CurrentStartIndex = CursorPos;
		}

		LayoutVerticalScrollBarSlider();
	}

	private void GotoWordRight() {
		SelectCheck();

		while (++CursorPos < TextStream.Count)
			if (char.IsWhiteSpace(TextStream[CursorPos]))
				break;

		while (++CursorPos < TextStream.Count)
			if (!char.IsWhiteSpace(TextStream[CursorPos]))
				break;

		if (CursorPos > TextStream.Count)
			CursorPos = TextStream.Count;

		ScrollRight();

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void ScrollRight()
	{
		if (!HorizScrollingAllowed)
			return;

		if (Multiline) { }
		else if (IsCursorOffRightSideOfWindow(CursorPos))
		{
			CurrentStartIndex++;
			ScrollRight();
		}

		LayoutVerticalScrollBarSlider();
	}

	private void Undo() { // FIXME: why does this only work once?
		CursorPos = UndoCursorPos;

		// I have a bad feeling about this...
		TextStream.Clear();
		TextStream.AddRange(UndoTextStream);

		InvalidateLayout();
		Repaint();
		SelectNone();
	}

	public void SelectAllText(bool resetCursorPos) {
		if (TextStream.Count == 0)
			Select[0] = -1;
		else
			Select[0] = 0;

		Select[1] = TextStream.Count;

		if (resetCursorPos)
			CursorPos = Select[1];
	}

	private void InsertChar(char ch) {
		if (ch == '\r')
			return;

		if (!Multiline && ch == '\n')
			return;

		if (ch == '\t')
			return;

		if (AllowNumericInputOnly) {
			if (!char.IsDigit(ch) && (ch != '.')) {
				Surface.PlaySound("Resource\\warning.wav");
				return;
			}
		}

		if (!AllowNonAsciiCharacters) {
			if (ch > 127)
				return;
		}

		if (MaxCharCount > -1 && TextStream.Count >= MaxCharCount) {
			if (MaxCharCount > 0 && Multiline && Wrap) {
				while (TextStream.Count > MaxCharCount) {
					if (RecalculateBreaksIndex == 0)
						RecalculateLineBreaks();

					if (LineBreaks[0] > TextStream.Count) {
						RecalculateBreaksIndex = -1;
						RecalculateLineBreaks();
					}

					if (LineBreaks[0] + 1 < TextStream.Count) {
						TextStream.RemoveRange(0, LineBreaks[0]);

						if (CursorPos > TextStream.Count)
							CursorPos = TextStream.Count;
						else {
							CursorPos -= LineBreaks[0] + 1;
							if (CursorPos < 0) {
								CursorPos = 0;
							}
						}

						if (Select[0] > -1) {
							Select[0] -= LineBreaks[0] + 1;

							if (Select[0] <= 0)
								Select[0] = -1;

							Select[1] -= LineBreaks[0] + 1;
							if (Select[1] <= 0)
								Select[1] = -1;
						}

						for (int i = TextStream.Count - 1; i >= 0; i--)
							SetCharAt(TextStream[i], i + 1);


						RecalculateBreaksIndex = -1;
						RecalculateLineBreaks();

					}
				}

			}
			else {
				Surface.PlaySound("Resource\\warning.wav");
				return;
			}
		}


		if (Wrap) {
			SetCharAt(ch, TextStream.Count);
			CursorPos = TextStream.Count;
		}
		else {
			for (int i = TextStream.Count - 1; i >= CursorPos; i--)
				SetCharAt(TextStream[i], i + 1);

			SetCharAt(ch, CursorPos);
			CursorPos++;
		}

		if (ch == '\n')
			RecalculateLineBreaks();

		if (AutoProgressOnHittingCharLimit && TextStream.Count == MaxCharCount)
			RequestFocusNext();

		ScrollRight();

		DataChanged = true;

		CalcBreakIndex();
		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	public const int BUFFER_SIZE = 999999;

	private void SetCharAt(char ch, int index) {
		if ((ch == '\n') || (ch == '\0'))
			FlushLineBreaks(true);

		if (index < 0)
			return;

		if (index >= TextStream.Count)
			while (TextStream.Count <= index + 1)
				TextStream.Add('\0');

		TextStream[index] = ch;
		DataChanged = true;
	}

	private bool IsCursorOffRightSideOfWindow(int CursorPos) {
		int wx = GetWide() - 1;
		CursorToPixelSpace(CursorPos, out int cx, out int cy);
		if (wx <= 0) return false;

		return (cx >= wx);
	}

	private bool IsCursorOffLeftSideOfWindow(int CursorPos) {
		CursorToPixelSpace(CursorPos, out int cx, out int cy);
		return (cx < 0);
	}

	private void CalcBreakIndex() {
		// if (CursorPos == TextStream.Count) {
		// 	RecalculateBreaksIndex = LineBreaks.Count - 2;
		// 	return;
		// }

		// RecalculateBreaksIndex = 0;
		// while (CursorPos > LineBreaks[RecalculateBreaksIndex]) // todo: fix out of range
		// 	++RecalculateBreaksIndex;

		// --RecalculateBreaksIndex;
	}

	private void LayoutVerticalScrollBarSlider() {

	}

	private void RecalculateLineBreaks() {
		if (!Multiline || HideText)
			return;

		if (TextStream.Count == 0)
			return;

		IFont font = Font!;

		int wide = GetWide() - 2;

		if (VertScrollBar != null)
			wide -= VertScrollBar.GetWide();

		int charWidth;
		int x = DRAW_OFFSET_X, y = DRAW_OFFSET_Y;
		int wordStartIndex = 0;
		int wordLength = 0;
		bool hasWord = false;
		bool justStartedNewLine = true;
		bool wordStartedOnNewLine = true;

		int startChar = 0;
		if (RecalculateBreaksIndex <= 0) {
			LineBreaks.Clear();
		}
		else {
			for (int i2 = RecalculateBreaksIndex + 1; i2 < LineBreaks.Count; ++i2) {
				LineBreaks.RemoveAt(i2);
				i2--;
				startChar = LineBreaks[RecalculateBreaksIndex];
			}
		}

		if (TextStream[startChar] == '\r' || TextStream[startChar] == '\n')
			startChar++;

		int i;
		for (i = startChar; i < TextStream.Count; ++i) {
			char ch = TextStream[i];

			if (!char.IsWhiteSpace(ch)) {
				if (hasWord) {

				}
				else {
					wordStartIndex = i;
					hasWord = true;
					wordStartedOnNewLine = justStartedNewLine;
					wordLength = 0;
				}
			}
			else
				hasWord = false;

			charWidth = getCharWidth(font, ch);
			if (char.IsControl(ch))
				justStartedNewLine = false;

			if ((x + charWidth) > wide || ch == '\r' || ch == '\n') {
				AddAnotherLine(ref x, ref y);

				justStartedNewLine = true;
				hasWord = false;

				if (ch == '\r' || ch == '\n')
					LineBreaks.Add(i);
				else if (wordStartedOnNewLine)
					LineBreaks.Add(wordStartIndex);
				else {
					LineBreaks.Add(wordStartIndex);
					i = wordStartIndex;
				}

				wordLength = 0;
			}

			x += charWidth;
			wordLength += charWidth;
		}

		CharCount = i - 1;
		LineBreaks.Add(BUFFER_SIZE);
		LayoutVerticalScrollBarSlider();
	}

	private void SaveUndoState() {
		UndoCursorPos = CursorPos;
		UndoTextStream.Clear();
		UndoTextStream.AddRange(TextStream);
	}

	private void Backspace() {
		if (!IsEditable())
			return;

		if (CursorPos == 0)
			return;

		if (TextStream.Count() == 0)
			return;

		SaveUndoState();

		for (int i = CursorPos; i < TextStream.Count; ++i)
			SetCharAt(TextStream[i], i - 1);

		TextStream.RemoveAt(TextStream.Count - 1);

		if (CursorPos == CurrentStartIndex)
			if (CurrentStartIndex - 6 >= 0)
				CurrentStartIndex -= 6;
			else
				CurrentStartIndex = 0;

		CursorPos--;

		DataChanged = true;

		FlushLineBreaks(true);

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void GotoEndOfLine() {
		SelectCheck();
		CursorPos = GetCurrentLineEnd();
		PutCursorAtEnd = true;

		ScrollRight();

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private int GetCurrentLineEnd() {
		int i;
		if (IsLineBreak(CursorPos)) {
			for (i = 0; i < LineBreaks.Count - 1; ++i)
				if (CursorPos == LineBreaks[i])
					break;

			if (!CursorIsAtEnd) {
				if (i == LineBreaks.Count - 2)
					return TextStream.Count;
				else
					return LineBreaks[i + 1];
			}
			else
				return CursorPos;
		}

		for (i = 0; i < LineBreaks.Count - 1; i++)
			if (CursorPos < LineBreaks[i])
				return LineBreaks[i];


		return TextStream.Count;
	}

	private bool IsLineBreak(int index) {
		for (int i = 0; i < LineBreaks.Count; ++i)
			if (index == LineBreaks[i])
				return true;

		return false;
	}

	private void GotoTextStart() {
		SelectCheck();
		CursorPos = 0;
		PutCursorAtEnd = false;
		CurrentStartIndex = 0;

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void GotoFirstOfLine() {
		SelectCheck();
		CursorPos = GetCurrentLineStart();
		PutCursorAtEnd = false;

		CurrentStartIndex = CursorPos;

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private int GetCurrentLineStart() {
		if (!Multiline)
			return CurrentStartIndex;

		int i;
		if (IsLineBreak(CursorPos)) {
			for (i = 0; i < LineBreaks.Count; ++i) {
				if (CursorPos == LineBreaks[i])
					break;
			}
			if (CursorIsAtEnd) {
				if (i > 0)
					return LineBreaks[i - 1];

				return LineBreaks[0];
			}
			else
				return CursorPos;
		}

		for (i = 0; i < LineBreaks.Count; ++i) {
			if (CursorPos < LineBreaks[i]) {
				if (i == 0)
					return 0;
				else
					return LineBreaks[i - 1];
			}
		}

		return 0;
	}

	private void GotoDown() {
		SelectCheck();

		if (CursorIsAtEnd) {
			CursorPos--;
			if (CursorPos < 0)
				CursorPos = 0;
		}

		CursorToPixelSpace(CursorPos, out int cx, out int cy);

		MoveCursor(GetCursorLine() + 1, cx);
		if (!PutCursorAtEnd && CursorIsAtEnd) {
			CursorPos++;
			if (CursorPos > TextStream.Count)
				CursorPos = TextStream.Count;
		}
		LayoutVerticalScrollBarSlider();
	}

	private void MoveCursor(int line, int pixelsAcross) {
		if (line < 0)
			line = 0;
		if (line >= LineBreaks.Count)
			line = LineBreaks.Count - 1;

		int yStart = GetYStart();

		int x = DRAW_OFFSET_X, y = yStart;
		int lineBreakIndexIndex = 0;
		PixelsIndent = 0;
		int i;
		for (i = 0; i < TextStream.Count; i++) {
			char ch = TextStream[i];

			if (HideText) {
				ch = '*';
			}

			if (LineBreaks[lineBreakIndexIndex] == i) {
				if (lineBreakIndexIndex == line) {
					PutCursorAtEnd = true;
					CursorPos = i;
					break;
				}

				AddAnotherLine(ref x, ref y);
				lineBreakIndexIndex++;
			}

			int charWidth = getCharWidth(Font, ch);

			if (line == lineBreakIndexIndex) {
				if ((x + (charWidth / 2)) > pixelsAcross) {
					CursorPos = i;
					break;
				}
			}

			x += charWidth;
		}

		if (i == TextStream.Count)
			GotoTextEnd();

		LayoutVerticalScrollBarSlider();
		ResetCursorBlink();
		Repaint();
	}

	private void Delete() {
		if (!IsEditable())
			return;

		if (TextStream.Count == 0)
			return;

		if (!GetSelectedRange(out int x0, out int x1)) {
			x0 = CursorPos;
			x1 = x0 + 1;

			if (CursorPos >= TextStream.Count)
				return;
		}

		SaveUndoState();

		int dif = x1 - x0;
		for (int i = 0; i < dif; i++)
			TextStream.RemoveAt(x0);

		ResetCursorBlink();

		SelectNone();

		CursorPos = x0;

		DataChanged = true;

		FlushLineBreaks(true);

		CalcBreakIndex();

		LayoutVerticalScrollBarSlider();
	}

	private void CopySelected() {
		if (HideText)
			return;

		if (GetSelectedRange(out int x0, out int x1)) {
			List<char> buf = [];
			for (int i = x0; i < x1; i++) {
				if (TextStream[i] == '\n')
					buf.Add('\r');

				buf.Add(TextStream[i]);
			}
			buf.Add('\0');
			System.SetClipboardText(buf.AsSpan());
		}

		RequestFocus();

		if (DataChanged)
			FireActionSignal();
	}

	private void Paste() {
		if (!IsEditable())
			return;

		List<char> buf = [];
		int bufferSize = (int)System.GetClipboardTextCount();
		if (!AutoProgressOnHittingCharLimit)
			bufferSize = MaxCharCount > 0 ? MaxCharCount + 1 : bufferSize;

		buf.EnsureCapacity(bufferSize);
		for (int i = 0; i < bufferSize; i++)
			buf.Add('\0');

		int len = (int)System.GetClipboardText(0, buf.AsSpan());
		if (len < 1)
			return;

		SaveUndoState();
		bool haveMovedFocusAwayFromCurrentEntry = false;

		for (int i = 0; i < len && buf[i] != 0; i++) {
			if (AutoProgressOnHittingCharLimit) {
				if (TextStream.Count == MaxCharCount) {
					RequestFocusNext();
					Span<char> remainingText = buf.AsSpan()[i..];
					System.SetClipboardText(remainingText[..(len - i - 1)]);
					if (GetParent() != null && GetCurrentKeyFocus() != this) {
						haveMovedFocusAwayFromCurrentEntry = true;
						GetCurrentKeyFocus()?.SendMessage(new KeyValues("DoPaste"), this);
					}
					break;
				}
			}

			InsertChar(buf[i]);
		}

		if (AutoProgressOnHittingCharLimit)
			System.SetClipboardText(buf.AsSpan()[..bufferSize]);

		DataChanged = true;
		FireActionSignal();

		if (!haveMovedFocusAwayFromCurrentEntry)
			RequestFocus();
	}

	/// <summary>
	/// Sets <see cref="RecalculateBreaksIndex"/> to 0, clears all line breaks, and if <paramref name="addBufferSize"/> is true, will add <see cref="BUFFER_SIZE"/> to LineBreaks automatically
	/// </summary>
	/// <param name="addBufferSize"></param>
	private void FlushLineBreaks(bool addBufferSize) {
		RecalculateBreaksIndex = 0;
		LineBreaks.Clear();
		if (addBufferSize)
			LineBreaks.Add(BUFFER_SIZE);
	}

	private void DeleteSelected() {
		if (!IsEditable())
			return;

		if (TextStream.Count == 0)
			return;

		if (!GetSelectedRange(out int x0, out int x1))
			return;

		SaveUndoState();

		for (int i = 0, dif = x1 - x0; i < dif; ++i)
			TextStream.RemoveAt(x0);

		SelectNone();
		ResetCursorBlink();

		CursorPos = x0;

		DataChanged = true;

		FlushLineBreaks(true);
		CalcBreakIndex();

		LayoutVerticalScrollBarSlider();
	}

	private void GotoUp() {
		SelectCheck();

		if (CursorIsAtEnd) {
			if ((GetCursorLine() - 1) == 0) {
				PutCursorAtEnd = true;
				return;
			}
			else
				CursorPos--;
		}

		CursorToPixelSpace(CursorPos, out int cx, out int cy);
		MoveCursor(GetCursorLine() - 1, cx);
	}

	private void GotoRight() {
		SelectCheck();

		if (IsLineBreak(CursorPos)) {
			if (CursorIsAtEnd) {
				PutCursorAtEnd = false;
			}
			else {
				if (CursorPos < TextStream.Count) {
					CursorPos++;
				}
			}
		}
		else {
			if (CursorPos < TextStream.Count) {
				CursorPos++;
			}

			if (IsLineBreak(CursorPos)) {
				if (!CursorIsAtEnd)
					PutCursorAtEnd = true;
			}
		}

		ScrollRight();

		ResetCursorBlink();
		Repaint();
	}

	private void GotoLeft() {
		SelectCheck();

		if (IsLineBreak(CursorPos))
			if (!CursorIsAtEnd)
				PutCursorAtEnd = true;

		if (!PutCursorAtEnd && CursorPos > 0)
			CursorPos--;

		ScrollLeft();

		ResetCursorBlink();
		Repaint();
	}

	static readonly KeyValues TextNewLineActionSignal = new("TextNewLine");
	static readonly KeyValues TextChangedActionSignal = new("TextChanged");
	public void FireActionSignal() {
		PostActionSignal(TextChangedActionSignal);
		DataChanged = false;   // reset the data changed flag
		InvalidateLayout();
	}

	private void ResetCursorBlink() {
		CursorBlink = false;
		CursorNextBlinkTime = System.GetTimeMillis() + CursorBlinkRate;
	}

	public void SetTextHidden(bool hideText) {
		HideText = hideText;
		Repaint();
	}

	public override IPanel? GetDragPanel() {
		if (Input.IsMouseDown(ButtonCode.MouseLeft)) {
			Input.GetCursorPos(out int x, out int y);
			ScreenToLocal(ref x, ref y);
			int cursor = PixelToCursorSpace(x, y);

			bool check = GetSelectedRange(out int cx0, out int cx1);

			if (check && cursor >= cx0 && cursor < cx1)
				return base.GetDragPanel();

			return null;
		}

		return base.GetDragPanel();
	}

	public int PixelToCursorSpace(int cx, int cy) {
		GetSize(out int w, out int h);
		cx = Math.Clamp(cx, 0, w + 100);
		cy = Math.Clamp(cy, 0, h);

		PutCursorAtEnd = false;

		int fontTall = Surface.GetFontTall(Font);

		int yStart = GetYStart();
		int x = DRAW_OFFSET_X, y = yStart;
		PixelsIndent = 0;
		int lineBreakIndexIndex = 0;

		int startIndex = GetStartDrawIndex(ref lineBreakIndexIndex);
		bool onRightLine = false;
		int i;
		for (i = startIndex; i < TextStream.Count; i++) {
			char ch = TextStream[i];
			if (HideText)
				ch = '*';

			if (LineBreaks.Count > 0 && LineBreaks[lineBreakIndexIndex] == i) {
				AddAnotherLine(ref x, ref y);
				lineBreakIndexIndex++;

				if (onRightLine) {
					PutCursorAtEnd = true;
					return i;
				}
			}

			if (cy < yStart) {
				onRightLine = true;
				PutCursorAtEnd = true;
			}
			else if (cy >= y && (cy < (y + fontTall + DRAW_OFFSET_Y)))
				onRightLine = true;


			int wide = getCharWidth(Font, ch);

			if (onRightLine) {
				if (cx > GetWide()) { }
				else if (cx < (DRAW_OFFSET_X + PixelsIndent) || cy < yStart)

					return i;

				if (cx >= x && cx < (x + wide)) {
					if (cx < (x + (wide * 0.5)))
						return i;
					else
						return i + 1;
				}
			}
			x += wide;
		}

		return i;
	}

	public bool GetSelectedRange(out int cx0, out int cx1) {
		if (Select[0] == -1) {
			cx0 = cx1 = -1; // -1 may be better here
			return false;
		}

		cx0 = Select[0];
		cx1 = Select[1];

		if (cx1 < cx0)
			(cx1, cx0) = (cx0, cx1);

		return true;
	}


	private bool SelectCheck(bool fromMouse = false) {
		bool ret = true;
		if (!HasFocus() || !(Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift))) {
			bool deselect = true;
			if (fromMouse && GetDragPanel() != null) {
				Input.GetCursorPos(out int x, out int y);
				ScreenToLocal(ref x, ref y);
				int cursor = PixelToCursorSpace(x, y);

				bool check = GetSelectedRange(out int cx0, out int cx1);

				if (check && cursor >= cx0 && cursor < cx1) {
					deselect = false;
					ret = false;
				}
			}

			if (deselect) {
				Select[0] = -1;
			}
		}
		else if (Select[0] == -1) {
			Select[0] = CursorPos;
		}
		return ret;
	}

	public void SelectNone() {
		Select[0] = -1;
		Repaint();
	}
	public void SelectNoText() {
		Select[0] = -1;
		Select[1] = 0;
	}

	public void SetToFullWidth() {
		if (Multiline)
			return;

		PerformLayout();
		int wide = 2 * DRAW_OFFSET_X;

		for (int i = 0; i < TextStream.Count; ++i)
			wide += getCharWidth(Font, TextStream[i]);

		int tall = (Surface.GetFontTall(Font) + DRAW_OFFSET_Y) + DRAW_OFFSET_Y + 2;

		SetSize(wide, tall);
		PerformLayout();
	}

	public void SetEditable(bool state) {
		if (state)
			SetDropEnabled(true, 1);
		else
			SetDropEnabled(false);

		Editable = state;
	}

	public bool IsEditable() => Editable;
	public void SetMaximumCharCount(int chars) => MaxCharCount = chars;
	public void SetAllowNonAsciiCharacters(bool state) => AllowNonAsciiCharacters = state;
	public void SetAllowNumericInputOnly(bool state) => AllowNumericInputOnly = state;
	public void SelectAllOnFirstFocus(bool state) => ShouldSelectAllOnFirstFocus = state;
	public void SelectAllOnFocusAlways(bool state) {
		ShouldSelectAllOnFirstFocus = state;
		ShouldSelectAllOnFocusAlways = state;
	}
	public override void OnSetFocus() {
		if (ShouldSelectAllOnFirstFocus) {
			Select[1] = TextStream.Count;
			Select[0] = Select[1] > 0 ? 0 : -1;
			CursorPos = Select[1];
			if (!ShouldSelectAllOnFocusAlways)
				ShouldSelectAllOnFirstFocus = false;
		}
		else if (Input.IsKeyDown(ButtonCode.KeyTab) || Input.WasKeyReleased(ButtonCode.KeyTab)) {
			GotoTextEnd();
			SelectNone();
		}

		base.OnSetFocus();
	}

	public void OnSetText(ReadOnlySpan<char> text) {
		SetText(text);
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		Font = GetScheme()!.GetFont(resourceData.GetString("font", "Default"), IsProportional());
		SetFont(Font);

		SetTextHidden(resourceData.GetInt("textHidden", 0) != 0);
		SetEditable(resourceData.GetInt("editable", 1) != 0);
		SetMaximumCharCount(resourceData.GetInt("maxchars", -1));
		SetAllowNumericInputOnly(resourceData.GetInt("NumericInputOnly", 0) != 0);
		SetAllowNonAsciiCharacters(resourceData.GetInt("unicode", 0) != 0);
		SelectAllOnFirstFocus(resourceData.GetInt("selectallonfirstfocus", 0) != 0);
	}
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("TextEntry.TextColor", scheme));
		SetBgColor(GetSchemeColor("TextEntry.BgColor", scheme));

		CursorColor = GetSchemeColor("TextEntry.CursorColor", scheme);
		DisabledFgColor = GetSchemeColor("TextEntry.DisabledTextColor", scheme);
		DisabledBgColor = GetSchemeColor("TextEntry.DisabledBgColor", scheme);

		SelectionTextColor = GetSchemeColor("TextEntry.SelectedTextColor", GetFgColor(), scheme);
		SelectionColor = GetSchemeColor("TextEntry.SelectedBgColor", scheme);
		DefaultSelectionBG2Color = GetSchemeColor("TextEntry.OutOfFocusSelectedBgColor", scheme);
		FocusEdgeColor = GetSchemeColor("TextEntry.FocusEdgeColor", new(0, 0, 0, 0), scheme);

		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));

		Font ??= scheme.GetFont("Default", IsProportional());
		SmallFont ??= scheme.GetFont("DefaultVerySmall", IsProportional());

		SetFont(Font);
	}

	public virtual void SetFont(IFont? font) {
		Font = font;
		InvalidateLayout();
		Repaint();
	}

	public int GetText(Span<char> outBuffer) {
		int i, c;
		for (i = 0, c = Math.Min(outBuffer.Length, TextStream.Count); i < c; i++) {
			char ch = TextStream[i];
			if (ch == '\0')
				break;
			outBuffer[i] = ch;
		}
		return i;
	}

	public void SetText(ReadOnlySpan<char> text) {
		if (text.IsEmpty)
			text = "";

		if (text.Length > 0 && text[0] == '#') {
			ReadOnlySpan<char> localized = Localize.Find(text);
			if (!localized.IsEmpty) {
				SetText(localized);
				return;
			}
		}

		TextStream.Clear();
		TextStream.EnsureCapacity(text.Length);
		int missed_count = 0;
		for (int i = 0; i < text.Length; i++) {
			if (text[i] == '\r') {
				missed_count++;
				continue;
			}
			TextStream.Add(text[i]);
			SetCharAt(text[i], i - missed_count);
		}

		GotoTextStart();
		SelectNone();

		DataChanged = false;

		FlushLineBreaks(true);
		InvalidateLayout();
	}

	public virtual void SetUseFallbackFont(bool state, IFont fallback) {
		FallbackFont = fallback;
		UseFallbackFont = state;
	}
}
