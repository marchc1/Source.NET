using Source.Common;
using Source.Common.GUI;

namespace Source.GUI.Controls;

public struct ColorChange
{
	public nint TextStreamIndex;
	public Color Color;
}
public class TextImage : Image
{
	readonly public ILocalize Localize = Singleton<ILocalize>();

	string? Text;
	IFont? Font;
	IFont? FallbackFont;
	ulong UnlocalizedTextSymbol;
	int DrawWidth;
	nint EllipsesPosition;
	bool RecalculateTruncation;
	bool Wrap;
	bool UseFallbackFont;
	bool RenderUsingFallbackFont;
	bool AllCaps;
	bool WrapCenter;

	List<nint> LineBreaks = [];
	List<int> LineXIndent = [];
	List<ColorChange> ColorChangeStream = [];

	public void SetDrawWidth(int width) {
		DrawWidth = width;
		RecalculateTruncation = true;
	}

	public TextImage(ReadOnlySpan<char> text) : base() {
		Text = null;
		Font = null;
		FallbackFont = null;
		UnlocalizedTextSymbol = unchecked((ulong)-1);
		DrawWidth = 0;
		EllipsesPosition = 0;
		Wrap = false;
		UseFallbackFont = false;
		RenderUsingFallbackFont = false;
		AllCaps = false;
		WrapCenter = false;
		LineBreaks.Clear();
		LineXIndent.Clear();

		SetText(text);
	}


	public virtual void SetFont(IFont? font) {
		Font = font;
		RecalculateTruncation = true;
	}


	public void SetText(ReadOnlySpan<char> text) {
		if (text.IsEmpty)
			text = "";

		if (!text.IsEmpty && text.Length > 0 && text[0] == '#') {
			UnlocalizedTextSymbol = Localize.FindIndex(text[1..]);
			if (UnlocalizedTextSymbol != ulong.MaxValue) {
				SetText(Localize.GetValueByIndex(UnlocalizedTextSymbol));
				return;
			}
		}

		SetText(text, false);
	}

	private void SetText(ReadOnlySpan<char> text, bool clearUnlocalizedSymbol) {
		if (clearUnlocalizedSymbol) {
			UnlocalizedTextSymbol = unchecked((ulong)-1);
		}

		if (text.IsEmpty)
			text = "";

		Text = new(text);
		LineBreaks.Clear();
		LineXIndent.Clear();
		RecalculateTruncation = true;
	}

	public void GetText(Span<char> buffer) {
		// todo
	}

	public IFont? GetFont() {
		if (RenderUsingFallbackFont)
			return FallbackFont;
		return Font;
	}

	public override void SetSize(int wide, int tall) {
		base.SetSize(wide, tall);
		DrawWidth = wide;
		RecalculateTruncation = true;
	}

	public override void GetContentSize(out int wide, out int tall) {
		GetTextSize(out wide, out tall);
	}

	private void GetTextSize(out int wide, out int tall) {
		wide = 0;
		tall = 0;
		int maxWide = 0;
		ReadOnlySpan<char> text = Text;

		IFont? font = Font;
		if (font == null)
			return;

		if (Wrap || WrapCenter)
			RecalculateNewLinePositions();

		int fontHeight = Surface.GetFontTall(GetFont());
		tall = fontHeight;

		nint textLen = text.Length;
		for (nint i = 0; i < textLen; i++) {
			char ch = text[(int)i];

			if (ch == '&')
				continue;

			if (AllCaps)
				ch = char.ToUpper(ch);

			Surface.GetCharABCwide(font, ch, out int a, out int b, out int c);
			wide += (a + b + c);

			if (ch == '\n') {
				tall += fontHeight;
				if (wide > maxWide)
					maxWide = wide;

				wide = 0;
			}

			if (Wrap || WrapCenter) {
				for (nint j = 0; j < LineBreaks.Count; j++) {
					if (i == LineBreaks[(int)j]) {
						tall += fontHeight;
						if (wide > maxWide)
							maxWide = wide;

						wide = 0;
					}
				}
			}

		}

		if (wide < maxWide)
			wide = maxWide;
	}

	public void ResizeImageToContentMaxWidth(int maxWidth) {
		DrawWidth = maxWidth;
		if (RecalculateTruncation) {
			if (Wrap || WrapCenter)
				RecalculateNewLinePositions();

			RecalculateEllipsesPosition();
		}
		ResizeImageToContent();
	}

	public void ResizeImageToContent() {
		GetContentSize(out int wide, out int tall);
		SetSize(wide, tall);
	}

	public override void Paint() {
		GetSize(out int wide, out int tall);
		IFont? font = GetFont();

		if (Text == null || font == null)
			return;

		if (RecalculateTruncation) {
			if (Wrap || WrapCenter)
				RecalculateNewLinePositions();

			RecalculateEllipsesPosition();
		}

		DrawSetTextColor(GetColor());
		DrawSetTextFont(font);

		int lineHeight = Surface.GetFontTall(font);
		float x = 0.0f;
		int y = 0;
		int indent = 0;
		int nextColorChange = 0;

		GetPos(out int px, out int py);
		int currentLineBreak = 0;

		if (WrapCenter && LineXIndent.Count > 0)
			x = LineXIndent[0];

		for (nint i = 0, len = Text.Length; i < len; i++) {
			char ch = Text[(int)i];
			if (AllCaps)
				ch = char.ToUpperInvariant(ch);

			if (ColorChangeStream.Count > nextColorChange)
				if (ColorChangeStream[nextColorChange].TextStreamIndex == i)
					DrawSetTextColor(ColorChangeStream[nextColorChange++].Color);

			if (ch == '\r' || ch <= 8)
				continue;
			else if (ch == '\n') {
				indent++;
				if (WrapCenter && indent < LineXIndent.Count)
					x = LineXIndent[indent];
				else
					x = 0;
				y += lineHeight;
			}
			else if (ch == '&') {
				if (i + 1 < len && Text[(int)(i + 1)] == '&')
					i++;
				else {
					continue;
				}
			}

			if (EllipsesPosition > 0 && i == EllipsesPosition) {
				for (int i2 = 0; i2 < 3; i2++) {
					Surface.DrawSetTextPos((int)x + px, y + py);
					Surface.DrawChar('.');
					x += Surface.GetCharacterWidth(font, '.');
				}
				break;
			}

			if (currentLineBreak != LineBreaks.Count) {
				if (i == LineBreaks[currentLineBreak]) {
					indent++;
					if (WrapCenter && indent < LineXIndent.Count)
						x = LineXIndent[indent];
					else
						x = 0;

					y += lineHeight;
					currentLineBreak++;
				}
			}
			Surface.DrawSetTextPos((int)x + px, y + py);
			Surface.DrawChar(ch);
			x += Surface.GetCharacterWidth(font, ch);
		}
	}

	private void RecalculateEllipsesPosition() {
		RecalculateTruncation = false;
		EllipsesPosition = 0;

		if (Wrap || WrapCenter)
			return;

		if (Text!.Contains('\n'))
			return;

		if (DrawWidth == 0)
			GetSize(out DrawWidth, out _);

		for (int check = 0; check < (UseFallbackFont ? 2 : 1); ++check) {
			IFont? font = GetFont();
			if (check == 1 && FallbackFont != null) {
				EllipsesPosition = 0;
				font = FallbackFont;
				RenderUsingFallbackFont = true;
			}

			int ellipsesWidth = 3 * Surface.GetCharacterWidth(font, '.');
			int x = 0;

			for (nint i = 0, textLen = Text.Length; i < textLen; i++) {
				char ch = Text[(int)i];
				if (AllCaps)
					ch = char.ToUpperInvariant(ch);


				if (ch == '\r' || ch <= 8)
					continue;
				else if (ch == '&') {
					if (i + 1 < textLen && Text[(int)(i + 1)] == '&')
						i++;
					else {
						continue;
					}
				}

				int charLen = Surface.GetCharacterWidth(font, ch);
				if (i == 0) {
					x += charLen;
					continue;
				}

				if (x + charLen + ellipsesWidth > DrawWidth) {
					int remainingLength = charLen;
					for (nint ri = i + 1; ri < textLen; ri++) {
						remainingLength += Surface.GetCharacterWidth(font, Text[(int)ri]);
					}

					if (x + remainingLength > DrawWidth) {
						EllipsesPosition = i;
						break;
					}
				}

				x += (int)charLen;
			}

			if (EllipsesPosition == 0)
				break;
		}
	}

	public void RecalculateNewLinePositions() {
		IFont? font = GetFont();

		int charWidth;
		int x = 0;

		nint wordStartIndex = 0;
		int wordLength = 0;
		bool hasWord = false;
		bool justStartedNewLine = true;
		bool wordStartedOnNewLine = true;

		int startChar = 0;

		LineBreaks.Clear();
		LineXIndent.Clear();
		if (Text == null)
			return;
		if (Text.Length > 0 && (Text![startChar] == '\r' || Text![startChar] == '\n')) {
			startChar++;
		}

		for (nint i = 0, len = Text.Length; i < len; i++) {
			char ch = Text[(int)i];
			if ((ch == '&' || ch == 0x01 || ch == 0x02 || ch == 0x03) && (i + 1) < len) {
				i++;
				ch = Text[(int)i];
			}

			if (AllCaps)
				ch = char.ToUpper(ch);

			if (!char.IsWhiteSpace(ch)) {
				if (!hasWord) {
					wordStartIndex = i;
					hasWord = true;
					wordStartedOnNewLine = justStartedNewLine;
					wordLength = 0;
				}
			}
			else {
				hasWord = false;
			}

			charWidth = Surface.GetCharacterWidth(font, ch);
			if (!char.IsControl(ch))
				justStartedNewLine = false;

			if ((x + charWidth) > DrawWidth || ch == '\r' || ch == '\n') {
				justStartedNewLine = true;
				hasWord = false;

				if (ch == '\r' || ch == '\n') {

				}
				else if (wordStartedOnNewLine) {
					LineBreaks.Add(i);
				}
				else {
					LineBreaks.Add(wordStartIndex);

					i = wordStartIndex - 1;
				}

				wordLength = 0;
				x = 0;
				continue;
			}

			x += charWidth;
			wordLength += charWidth;
		}

		RecalculateCenterWrapIndents();
	}

	private void RecalculateCenterWrapIndents() {
		LineXIndent.Clear();

		if (!WrapCenter)
			return;

		if (Text == null || GetFont() == null)
			return;

		IFont font = GetFont()!;
		GetPos(out int px, out int py);

		int currentLineBreak = 0;
		int iCurLineW = 0;

		for (nint i = 0, len = Text.Length; i < len; i++) {
			char ch = Text[(int)i];

			if (AllCaps)
				ch = char.ToUpper(ch);

			if (ch == '\r') {
				continue;
			}
			else if (ch == '\n') {
				LineXIndent.Add((int)((DrawWidth - iCurLineW) * 0.5f));

				iCurLineW = 0;
				continue;
			}
			else if (ch == '&') {
				if (i + 1 < len && Text[(int)(i + 1)] == '&')
					i++;
				else {
					continue;
				}
			}


			if (currentLineBreak != LineBreaks.Count) {
				if (i == LineBreaks[currentLineBreak]) {
					LineXIndent.Add((int)((DrawWidth - iCurLineW) * 0.5f));

					iCurLineW = 0;
					currentLineBreak++;
				}
			}

			iCurLineW += Surface.GetCharacterWidth(font, ch);
		}

		LineXIndent.Add((DrawWidth - iCurLineW) / 2);
	}

	internal void SetUseFallbackFont(bool useFallbackFont, IFont? fallbackItemFont) {
		UseFallbackFont = useFallbackFont;
		FallbackFont = fallbackItemFont;
	}

	internal void SetAllCaps(bool allCaps) {
		AllCaps = allCaps;
	}

	internal void SetCenterWrap(bool wrap) {
		WrapCenter = wrap;
	}

	internal void SetWrap(bool wrap) {
		Wrap = wrap;
	}
}
