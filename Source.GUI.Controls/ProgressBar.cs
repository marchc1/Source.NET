
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

public enum ProgressDir
{
	East,
	West,
	North,
	South
}

public class ProgressBar : Panel
{
	public static Panel Create_ProgressBar() => new ProgressBar(null, null);

	protected ProgressDir ProgressDirection;
	protected double Progress;
	int SegmentGap;
	int SegmentWide;
	int BarInset;
	int BarMargin;
	string? DialogVar;

	public ProgressBar(Panel? parent, string? name) : base(parent, name) {
		Progress = 0;
		DialogVar = null;
		SetSegmentInfo(4, 8);
		SetBarInset(4);
		SetMargin(0);
		ProgressDirection = ProgressDir.East;
	}

	public void SetSegmentInfo(int gap, int width) {
		SegmentGap = gap;
		SegmentWide = width;
	}
	public int GetDrawnSegmentCount() {
		GetSize(out int wide, out _);
		int segmentTotal = wide / (SegmentGap + SegmentWide);
		return (int)(segmentTotal * Progress);
	}

	public double GetProgress() => Progress;

	public void SetProgress(double progress) {
		if(progress != Progress) {
			Progress = Math.Clamp(progress, 0, 1);
			Repaint();
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("ProgressBar.FgColor", scheme));
		SetBgColor(GetSchemeColor("ProgressBar.BgColor", scheme));
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));
	}

	public int GetBarInset() => BarInset;
	public void SetBarInset(int pixels) => BarInset = pixels;
	public int GetMargin() => BarMargin;
	public void SetMargin(int pixels) => BarMargin = pixels;

	public override void ApplySettings(KeyValues resourceData) {
		Progress = resourceData.GetFloat("progress", 0.0f);

		ReadOnlySpan<char> dialogVar = resourceData.GetString("variable", "");
		if (!dialogVar.IsEmpty && dialogVar.Length > 0) {
			DialogVar = new(dialogVar);
		}

		base.ApplySettings(resourceData);
	}

	public override void Paint() {
		GetSize(out int wide, out int tall);

		int segmentTotal = 0, segmentsDrawn = 0;
		int x = 0, y = 0;

		switch (ProgressDirection) {
			case ProgressDir.West:
				wide -= 2 * BarMargin;
				x = wide - BarMargin;
				y = BarInset;
				segmentTotal = wide / (SegmentGap + SegmentWide);
				segmentsDrawn = (int)(segmentTotal * Progress);
				break;

			case ProgressDir.East:
				wide -= 2 * BarMargin;
				x = BarMargin;
				y = BarInset;
				segmentTotal = wide / (SegmentGap + SegmentWide);
				segmentsDrawn = (int)(segmentTotal * Progress);
				break;

			case ProgressDir.North:
				tall -= 2 * BarMargin;
				x = BarInset;
				y = tall - BarMargin;
				segmentTotal = tall / (SegmentGap + SegmentWide);
				segmentsDrawn = (int)(segmentTotal * Progress);
				break;

			case ProgressDir.South:
				tall -= 2 * BarMargin;
				x = BarInset;
				y = BarMargin;
				segmentTotal = tall / (SegmentGap + SegmentWide);
				segmentsDrawn = (int)(segmentTotal * Progress);
				break;
		}

		Surface.DrawSetColor(GetFgColor());
		for (int i = 0; i < segmentsDrawn; i++) {
			PaintSegment(ref x, ref y, tall, wide);
		}
	}

	private void PaintSegment(ref int x, ref int y, int tall, int wide) {
		switch (ProgressDirection) {
			case ProgressDir.East:
				x += SegmentGap;
				Surface.DrawFilledRect(x, y, x + SegmentWide, y + tall - (y * 2));
				x += SegmentWide;
				break;

			case ProgressDir.West:
				x -= SegmentGap + SegmentWide;
				Surface.DrawFilledRect(x, y, x + SegmentWide, y + tall - (y * 2));
				break;

			case ProgressDir.North:
				y -= SegmentGap + SegmentWide;
				Surface.DrawFilledRect(x, y, x + wide - (x * 2), y + SegmentWide);
				break;

			case ProgressDir.South:
				y += SegmentGap;
				Surface.DrawFilledRect(x, y, x + wide - (x * 2), y + SegmentWide);
				y += SegmentWide;
				break;
		}
	}
}


public class ContinuousProgressBar : ProgressBar
{
	protected double PreviousProgress;
	protected Color ColorGain;
	protected Color ColorLoss;

	public ContinuousProgressBar(Panel? parent, string name) : base(parent, name) {

	}
}

