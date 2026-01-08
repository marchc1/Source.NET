using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class Slider : Panel
{
	bool Dragging;
	int NobPosMin;
	int NobPosMax;
	int NobDragStartPosX;
	int NobDragStartPosY;
	int DragStartPosX;
	int DragStartPosY;
	int RangeMin;
	int RangeMax;
	int SubRangeMin;
	int SubRangeMax;
	int Value;
	int ButtonOffset;
	IBorder? SliderBorder;
	IBorder? InsetBorder;
	float NobSize;
	TextImage? LeftCaption;
	TextImage? RightCaption;
	public Color TickColor;
	Color TrackColor;
	Color DisabledTextColor1;
	Color DisabledTextColor2;
	int NumTicks;
	bool _IsDragOnRepositionNob;
	bool UseSubRange;
	bool Inverted;
	public Slider(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		_IsDragOnRepositionNob = false;
		Dragging = false;
		Value = 0;
		RangeMin = 0;
		RangeMax = 0;
		ButtonOffset = 0;
		SliderBorder = null;
		InsetBorder = null;
		NumTicks = 10;
		LeftCaption = null;
		RightCaption = null;

		SubRangeMin = 0;
		SubRangeMax = 0;
		UseSubRange = false;
		Inverted = false;

		SetThumbWidth(8);
		RecomputeNobPosFromValue();
		AddActionSignalTarget(this);
		SetBlockDragChaining(true);
	}

	public void SetSliderThumbSubRange(bool enable, int min = 0, int max = 100) {
		UseSubRange = enable;
		SubRangeMin = min;
		SubRangeMax = max;
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		base.OnSizeChanged(newWide, newTall);
		RecomputeNobPosFromValue();
	}

	public void SetValue(int value, bool triggerChangeMessage = true) {
		int oldValue = Value;

		if (RangeMin < RangeMax) {
			if (value < RangeMin)
				value = RangeMin;
			if (value > RangeMax)
				value = RangeMax;
		}
		else {
			if (value < RangeMax)
				value = RangeMax;
			if (value > RangeMin)
				value = RangeMin;
		}

		Value = value;
		RecomputeNobPosFromValue();

		if (Value != oldValue && triggerChangeMessage)
			SendSliderMovedMessage();
	}

	public int GetValue() => Value;

	public override void PerformLayout() {
		base.PerformLayout();
		RecomputeNobPosFromValue();

		if (LeftCaption != null)
			LeftCaption.ResizeImageToContent();

		if (RightCaption != null)
			RightCaption.ResizeImageToContent();
	}

	public void RecomputeNobPosFromValue() {
		GetTrackRect(out int x, out _, out int wide, out _);

		float usevalue = Value;
		int userange = RangeMin;
		if (UseSubRange) {
			userange = SubRangeMin;
			usevalue = Math.Clamp(Value, SubRangeMin, SubRangeMax);
		}

		float fwide = wide;
		float frange = UseSubRange ? (SubRangeMax - SubRangeMin) : (RangeMax - RangeMin);
		float fvalue = usevalue - userange;
		float fper = (frange != 0.0f) ? (fvalue / frange) : 0.0f;

		if (Inverted)
			fper = 1.0f - fper;

		float freepixels = fwide - NobSize;
		float leftpixel = x;
		float firstpixel = leftpixel + freepixels * fper + 0.5f;

		NobPosMin = (int)firstpixel;
		NobPosMax = (int)(firstpixel + NobSize);

		int rightEdge = x + wide;
		if (NobPosMax > rightEdge) {
			NobPosMin = rightEdge - (int)NobSize;
			NobPosMax = rightEdge;
		}

		Repaint();
	}

	public void RecomputeValueFromNobPos() {
		int value = EstimateValueAtPos(NobPosMin);
		SetValue(value);
	}

	public int EstimateValueAtPos(int localMouseX) {
		GetTrackRect(out int x, out _, out int w, out _);

		int[] useRange = [RangeMin, RangeMax];
		if (UseSubRange)
			useRange = [SubRangeMin, SubRangeMax];

		float wide = w;
		float nob = localMouseX - x;
		float freepixels = wide - NobSize;
		float value = (freepixels != 0.0f) ? (nob / freepixels) : 0.0f;

		return (int)(useRange[0] + (useRange[1] - useRange[0]) * (value - 0.0f) / (1.0f - 0.0f));
	}
	public void SetInverted(bool state) => Inverted = state;

	private void SendSliderMovedMessage() {
		KeyValues msg = new("SliderMoved", "position", Value);
		msg.SetPtr("panel", this);
		PostActionSignal(msg);
	}

	private void SendSliderDragStartMessage() {
		KeyValues msg = new("SliderDragStart", "position", Value);
		msg.SetPtr("panel", this);
		PostActionSignal(msg);
	}

	private void SendSliderDragEndMessage() {
		KeyValues msg = new("SliderDragEnd", "position", Value);
		msg.SetPtr("panel", this);
		PostActionSignal(msg);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("Slider.NobColor", scheme));

		TickColor = scheme.GetColor("Slider.TextColor", GetFgColor());
		TrackColor = scheme.GetColor("Slider.TrackColor", GetFgColor());

		DisabledTextColor1 = scheme.GetColor("Slider.DisabledTextColor1", GetFgColor());
		DisabledTextColor2 = scheme.GetColor("Slider.DisabledTextColor2", GetFgColor());

		SliderBorder = scheme.GetBorder("ButtonBorder");
		InsetBorder = scheme.GetBorder("ButtonDepressedBorder");

		LeftCaption?.SetFont(scheme.GetFont("DefaultVerySmall", IsProportional()));
		RightCaption?.SetFont(scheme.GetFont("DefaultVerySmall", IsProportional()));
	}

	public override void GetSettings(KeyValues outResourceData) {
		base.GetSettings(outResourceData);

		Span<char> buf = stackalloc char[256];

		if (LeftCaption != null) {
			LeftCaption.GetText(buf);//GetUnlocalizedText
			outResourceData.SetString("leftText", buf);
		}

		if (RightCaption != null) {
			RightCaption.GetText(buf);//GetUnlocalizedText
			outResourceData.SetString("rightText", buf);
		}
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> left = resourceData.GetString("leftText", null);
		ReadOnlySpan<char> right = resourceData.GetString("rightText", null);

		int thumbWidth = resourceData.GetInt("thumbwidth", 0);
		if (thumbWidth != 0)
			SetThumbWidth(thumbWidth);

		SetTickCaptions(left, right);

		int numTicks = resourceData.GetInt("numTicks", -1);
		if (numTicks >= 0)
			SetNumTicks(numTicks);

		KeyValues rangeMinKV = resourceData.FindKey("rangeMin", false)!;
		KeyValues rangeMaxKV = resourceData.FindKey("rangeMax", false)!;

		bool doClamp = false;
		if (rangeMinKV != null) {
			RangeMin = resourceData.GetInt("rangeMin");
			doClamp = true;
		}

		if (rangeMaxKV != null) {
			RangeMax = resourceData.GetInt("rangeMax");
			doClamp = true;
		}

		if (doClamp)
			ClampRange();
	}

	public void GetTrackRect(out int x, out int y, out int w, out int h) {
		GetPaintSize(out int wide, out _);
		x = 0;
		y = 8;
		w = wide - (int)NobSize;
		h = 4;
	}

	public override void Paint() {
		DrawTicks();
		DrawTickLabels();
		DrawNob();
	}

	private void DrawTicks() {
		GetTrackRect(out _, out int y, out int wide, out _);

		float fwide = wide;
		float freepixels = fwide - NobSize;
		float leftpixel = NobSize / 2.0f;
		float pixelspertick = freepixels / NumTicks;

		y += (int)NobSize;
		int tickHeight = 5;

		if (IsEnabled()) {
			Surface.DrawSetColor(TickColor);
			for (int i = 0; i <= NumTicks; i++) {
				int xpos = (int)(leftpixel + i * pixelspertick);
				Surface.DrawFilledRect(xpos, y, xpos + 1, y + tickHeight);
			}
		}
		else {
			Surface.DrawSetColor(DisabledTextColor1);
			for (int i = 0; i <= NumTicks; i++) {
				int xpos = (int)(leftpixel + i * pixelspertick);
				Surface.DrawFilledRect(xpos + 1, y + 1, xpos + 2, y + tickHeight + 1);
			}

			Surface.DrawSetColor(DisabledTextColor2);
			for (int i = 0; i <= NumTicks; i++) {
				int xpos = (int)(leftpixel + i * pixelspertick);
				Surface.DrawFilledRect(xpos, y, xpos + 1, y + tickHeight);
			}
		}
	}

	private void DrawTickLabels() {
		GetTrackRect(out _, out int y, out int wide, out _);

		y += (int)NobSize + 4;

		if (IsEnabled())
			Surface.DrawSetColor(TickColor);
		else
			Surface.DrawSetColor(DisabledTextColor1);

		if (LeftCaption != null) {
			LeftCaption.SetPos(0, y);
			if (IsEnabled())
				LeftCaption.SetColor(TickColor);
			else
				LeftCaption.SetColor(DisabledTextColor1);
			LeftCaption.Paint();
		}

		if (RightCaption != null) {
			RightCaption.GetSize(out int rwide, out _);
			RightCaption.SetPos(wide - rwide, y);
			if (IsEnabled())
				RightCaption.SetColor(TickColor);
			else
				RightCaption.SetColor(DisabledTextColor1);
			RightCaption.Paint();
		}
	}

	private void DrawNob() {
		GetTrackRect(out _, out int y, out _, out int tall);
		Color col = GetFgColor();

		Surface.DrawSetColor(col);
		int nobHeight = 16;

		Surface.DrawFilledRect(NobPosMin, y + tall / 2 - nobHeight / 2, NobPosMax, y + tall / 2 + nobHeight / 2);

		SliderBorder?.Paint(NobPosMin, y + tall / 2 - nobHeight / 2, NobPosMax, y + tall / 2 + nobHeight / 2);
	}

	public void SetTickCaptions(ReadOnlySpan<char> left, ReadOnlySpan<char> right) {
		if (left.Length > 0)
			if (LeftCaption != null)
				LeftCaption.SetText(left);
			else
				LeftCaption = new TextImage(left);

		if (right.Length > 0)
			if (RightCaption != null)
				RightCaption.SetText(right);
			else
				RightCaption = new TextImage(right);

		InvalidateLayout();
	}

	public override void PaintBackground() {
		base.PaintBackground();

		GetTrackRect(out int x, out int y, out int wide, out int tall);

		Surface.DrawSetColor(TrackColor);
		Surface.DrawFilledRect(x, y, x + wide, y + tall);
		InsetBorder?.Paint(x, y, x + wide, y + tall);
	}

	public void SetRange(int min, int max) {
		RangeMin = min;
		RangeMax = max;
		ClampRange();
	}

	public void ClampRange() {
		if (RangeMin < RangeMax) {
			if (Value < RangeMin)
				SetValue(RangeMin, false);
			else if (Value > RangeMax)
				SetValue(RangeMax, false);
		}
		else {
			if (Value < RangeMax)
				SetValue(RangeMax, false);
			else if (Value > RangeMin)
				SetValue(RangeMin, false);
		}
	}

	public void GetRange(out int min, out int max) {
		min = RangeMin;
		max = RangeMax;
	}

	public override void OnCursorMoved(int x, int y) {
		if (!Dragging)
			return;

		Input.GetCursorPos(out x, out y);
		ScreenToLocal(ref x, ref y);

		GetTrackRect(out int tx, out int ty, out int wide, out int tall);

		NobPosMin = NobDragStartPosX + (x - DragStartPosX);
		NobPosMax = NobDragStartPosY + (x - DragStartPosX);

		int rightEdge = tx + wide;
		int unclamped = NobPosMin;

		if (NobPosMax > rightEdge) {
			NobPosMin = rightEdge - (NobPosMax - NobPosMin);
			NobPosMax = rightEdge;
		}

		if (NobPosMin < tx) {
			int offset = tx - NobPosMin;
			NobPosMax = NobPosMax - offset;
			NobPosMin = 0;
		}

		int value = EstimateValueAtPos(unclamped);
		SetValue(value, false);

		Repaint();
		SendSliderMovedMessage();
	}

	public void SetDragOnRepositionNob(bool state) => _IsDragOnRepositionNob = state;

	public bool IsDragOnRepositionNob() => _IsDragOnRepositionNob;

	public bool IsDragged() => Dragging;

	public override void OnMousePressed(ButtonCode code) {
		if (!IsEnabled())
			return;

		Input.GetCursorPos(out int x, out int y);
		ScreenToLocal(ref x, ref y);
		RequestFocus();

		bool startDragging = false, PostDragStartSignal = false;

		if (x >= NobPosMin && x <= NobPosMax) {
			startDragging = true;
			PostDragStartSignal = true;
		}
		else {
			GetRange(out int min, out int max);
			if (UseSubRange) {
				min = SubRangeMin;
				max = SubRangeMax;
			}

			GetTrackRect(out int tx, out int ty, out int wide, out int tall);
			if (wide > 0) {
				float frange = max - min;
				float clickFrac = Math.Clamp((float)(x - tx) / (wide - 1), 0.0f, 1.0f);
				float value = min + clickFrac * frange;
				startDragging = IsDragOnRepositionNob();

				if (startDragging) {
					Dragging = true;
					SendSliderDragStartMessage();
				}

				SetValue((int)(value + 0.5f));
			}
		}

		if (startDragging) {
			Dragging = true;
			Input.SetMouseCapture(this);
			NobDragStartPosX = NobPosMin;
			NobDragStartPosY = NobPosMax;
			DragStartPosX = x;
			DragStartPosY = y;

			if (PostDragStartSignal)
				SendSliderDragStartMessage();
		}
	}

	public override void OnMouseDoublePressed(ButtonCode code) => OnMousePressed(code);

	public override void OnKeyCodeTyped(ButtonCode code) {
		switch (code) {
			case ButtonCode.KeyLeft:
			case ButtonCode.KeyDown:
				SetValue(Value - 1);
				break;
			case ButtonCode.KeyRight:
			case ButtonCode.KeyUp:
				SetValue(Value + 1);
				break;
			case ButtonCode.KeyPageDown:
				GetRange(out int min, out int max);
				float range = max - min;
				float pertick = range / NumTicks;
				SetValue((int)(Value - pertick));
				break;
			case ButtonCode.KeyPageUp:
				GetRange(out min, out max);
				range = max - min;
				pertick = range / NumTicks;
				SetValue((int)(Value + pertick));
				break;
			case ButtonCode.KeyHome:
				GetRange(out min, out _);
				SetValue(min);
				break;
			case ButtonCode.KeyEnd:
				GetRange(out _, out max);
				SetValue(max);
				break;
			default:
				base.OnKeyCodeTyped(code);
				break;
		}
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (Dragging) {
			Dragging = false;
			Input.SetMouseCapture(null);
		}

		if (IsEnabled())
			SendSliderDragEndMessage();
	}

	public void GetNobPos(out int min, out int max) {
		min = NobPosMin;
		max = NobPosMax;
	}

	public void SetButtonOffset(int offset) => ButtonOffset = offset;
	public void SetThumbWidth(int width) => NobSize = width;
	public void SetNumTicks(int numTicks) => NumTicks = numTicks;
}
