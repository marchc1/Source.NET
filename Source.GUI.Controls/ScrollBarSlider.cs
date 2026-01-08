using Source.Common.GUI;
using Source.Common.Input;
using Source.Common.Formats.Keyvalues;

namespace Source.GUI.Controls;

public class ScrollBarSlider : Panel
{
	bool Vertical;
	bool Dragging;
	readonly int[] NobPos = new int[2];
	readonly int[] NobDragStartPos = new int[2];
	readonly int[] DragStartPos = new int[2];
	readonly int[] Range = new int[2];
	int Value;
	int RangeWindow;
	int ButtonOffset;
	IBorder? ScrollBarSliderBorder;

	public ScrollBarSlider(Panel? parent, ReadOnlySpan<char> panelName, bool vertical) : base(parent, panelName) {
		Vertical = vertical;
		Dragging = false;
		Value = 0;
		Range[0] = 0;
		Range[1] = 0;
		RangeWindow = 0;
		ButtonOffset = 0;
		ScrollBarSliderBorder = null;
		RecomputeNobPosFromValue();
		SetBlockDragChaining(true);
	}

	public void SetButtonOffset(int buttonOffset) => ButtonOffset = buttonOffset;

	public bool HasFullRange() {
		GetPaintSize(out int wide, out int tall);

		float frangewindow = RangeWindow;

		float CheckAgainst;
		if (Vertical)
			CheckAgainst = tall;
		else
			CheckAgainst = wide;

		if (frangewindow > 0)
			if (frangewindow <= (CheckAgainst + ButtonOffset))
				return true;

		return false;
	}

	internal void SendScrollBarSliderMovedMessage() {
		PostActionSignal(new KeyValues("ScrollBarSliderMoved", "position", Value));
	}

	public bool IsSliderVisible() {
		int itemRange = Range[1] - Range[0];

		if (itemRange <= 0)
			return false;

		if (itemRange <= RangeWindow)
			return false;

		return true;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("ScrollBarSlider.FgColor", scheme));
		SetBgColor(GetSchemeColor("ScrollBarSlider.BgColor", scheme));

		ScrollBarSliderBorder = scheme.GetBorder("ScrollBarSliderBorder") ?? scheme.GetBorder("ButtonBorder");
	}

	public override void Paint() {
		GetPaintSize(out int wide, out int tall);

		if (!IsSliderVisible())
			return;

		Color col = GetFgColor();
		Surface.DrawSetColor(col);

		if (Vertical) {
			if (GetPaintBackgroundType() == PaintBackgroundType.Box)
				DrawBox(1, NobPos[0], wide - 2, NobPos[1] - NobPos[0], col, 1.0f);
			else
				Surface.DrawFilledRect(1, NobPos[0], wide - 2, NobPos[1]);

			ScrollBarSliderBorder?.Paint(0, NobPos[0], wide, NobPos[1]);
		}
		else {
			Surface.DrawFilledRect(NobPos[0], 1, NobPos[1], tall - 2);
			ScrollBarSliderBorder?.Paint(NobPos[0] - 1, 1, NobPos[1], tall);
		}
	}

	internal int GetRangeWindow() => RangeWindow;

	internal void GetNobPos(out int min, out int max) {
		min = NobPos[0];
		max = NobPos[1];
	}

	public int GetValue() {
		return Value;
	}

	public bool IsVertical() => Vertical;

	public void SetValue(int val) {
		int OldValue = Value;

		if (val > Range[1] - RangeWindow)
			val = Range[1] - RangeWindow;

		if (val < Range[0])
			val = Range[0];

		Value = val;
		RecomputeNobPosFromValue();

		if (OldValue != Value)
			SendScrollBarSliderMovedMessage();
	}

	public void SetRange(int min, int max) {
		Range[0] = min;
		Range[1] = max;
	}

	public void GetRange(out int min, out int max) {
		min = Range[0];
		max = Range[1];
	}

	public override void PerformLayout() {
		RecomputeNobPosFromValue();
		base.PerformLayout();
	}

	internal void RecomputeNobPosFromValue() {
		GetPaintSize(out int wide, out int tall);

		float fwide = (float)wide - 1;
		float ftall = (float)tall - 1;
		float frange = Range[1] - Range[0];
		float fvalue = Value - Range[0];
		float fper = (frange != RangeWindow) ? (fvalue / (frange - RangeWindow)) : 0;

		if (RangeWindow > 0) {
			if (frange <= 0)
				frange = 1;

			float width, length;
			if (Vertical) {
				width = fwide;
				length = ftall;
			}
			else {
				width = ftall;
				length = fwide;
			}

			float proportion = RangeWindow / frange;
			float fnobsize = length * proportion;
			if (fnobsize < width) fnobsize = width;

			float freepixels = length - fnobsize;

			float firstpixel = freepixels * fper;

			NobPos[0] = (int)firstpixel;
			NobPos[1] = (int)(firstpixel + fnobsize);

			if (NobPos[1] > length) {
				NobPos[0] = NobPos[1] - (int)fnobsize;
				NobPos[1] = (int)length;
			}
		}

		Repaint();
	}

	internal void RecomputeValueFromNobPos() {
		GetPaintSize(out int wide, out int tall);

		float fwide = (float)wide - 1;
		float ftall = (float)tall - 1;
		float frange = Range[1] - Range[0];
		float fnob = NobPos[0];
		float frangewindow = RangeWindow;

		if (frangewindow > 0) {
			if (frange <= 0)
				frange = 1;

			float width, length;
			if (Vertical) {
				width = fwide;
				length = ftall;
			}
			else {
				width = ftall;
				length = fwide;
			}

			float proportion = frangewindow / frange;
			float fnobsize = length * proportion;

			if (fnobsize < width) fnobsize = width;

			float fvalue;
			if (length - fnobsize == 0)
				fvalue = 0.0f;
			else
				fvalue = (frange - frangewindow) * (fnob / (length - fnobsize));

			if ((fvalue + RangeWindow - Range[1]) > (0.01f * frangewindow))
				Value = Range[1] - RangeWindow;
			else
				Value = (int)(fvalue + Range[0] + 0.5f);

			Value = (Value < (Range[1] - RangeWindow)) ? Value : Range[1] - RangeWindow;

			if (Value < Range[0])
				Value = Range[0];
		}
	}

	public override void OnCursorMoved(int x, int y) {
		if (!Dragging)
			return;

		Input.GetCursorPos(out x, out y);
		ScreenToLocal(ref x, ref y);

		GetPaintSize(out int wide, out int tall);

		if (Vertical) {
			NobPos[0] = NobDragStartPos[0] + (y - DragStartPos[1]);
			NobPos[1] = NobDragStartPos[1] + (y - DragStartPos[1]);

			if (NobPos[1] > tall) {
				NobPos[0] = tall - (NobPos[1] - NobPos[0]);
				NobPos[1] = tall;
				SetValue(Range[1] - RangeWindow);
			}
		}
		else {
			NobPos[0] = NobDragStartPos[0] + (x - DragStartPos[0]);
			NobPos[1] = NobDragStartPos[1] + (x - DragStartPos[0]);

			if (NobPos[1] > wide) {
				NobPos[0] = wide - (NobPos[1] - NobPos[0]);
				NobPos[1] = wide;
				SetValue(Range[1] - RangeWindow);
			}
		}

		if (NobPos[0] < 0) {
			NobPos[1] = NobPos[1] - NobPos[0];
			NobPos[0] = 0;
			SetValue(0);
		}

		InvalidateLayout();
		RecomputeValueFromNobPos();
		SendScrollBarSliderMovedMessage();
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		OnMousePressed(code);
	}

	public override void OnMouseReleased(ButtonCode code) {
		Dragging = false;
		Input.SetMouseCapture(null);
	}

	public override void OnMousePressed(ButtonCode code) {
		Input.GetCursorPos(out int x, out int y);
		ScreenToLocal(ref x, ref y);

		if (Vertical) {
			if ((y >= NobPos[0]) && (y < NobPos[1])) {
				Dragging = true;
				Input.SetMouseCapture(this);
				NobDragStartPos[0] = NobPos[0];
				NobDragStartPos[1] = NobPos[1];
				DragStartPos[0] = x;
				DragStartPos[1] = y;
			}
			else if (y < NobPos[0]) {
				int val = GetValue();
				val -= RangeWindow;
				SetValue(val);
			}
			else if (y >= NobPos[1]) {
				int val = GetValue();
				val += RangeWindow;
				SetValue(val);
			}
		}
		else {
			if ((x >= NobPos[0]) && (x < NobPos[1])) {
				Dragging = true;
				Input.SetMouseCapture(this);
				NobDragStartPos[0] = NobPos[0];
				NobDragStartPos[1] = NobPos[1];
				DragStartPos[0] = x;
				DragStartPos[1] = y;
			}
			else if (x < NobPos[0]) {
				int val = GetValue();
				val -= RangeWindow;
				SetValue(val);
			}
			else if (x >= NobPos[1]) {
				int val = GetValue();
				val += RangeWindow;
				SetValue(val);
			}
		}
	}

	public void SetRangeWindow(int range) => RangeWindow = range;
}
