using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

class BudgetHistoryPanel : Panel
{
	static ConVar budget_show_history = new("budget_show_history", "1", FCvar.Archive, "turn history graph off and on. . good to turn off on low end");
	static ConVar budget_history_numsamplesvisible = new("budget_history_numsamplesvisible", "100", FCvar.Archive, "number of samples to draw in the budget history window.  The lower the better as far as rendering overhead of the budget panel");

	BaseBudgetPanel BudgetPanel;
	double[] Data = [];
	int Groups;
	int SamplesPerGroup;
	int SampleOffset;
	float RangeMin;
	float RangeMax;
	public BudgetHistoryPanel(BaseBudgetPanel parent, ReadOnlySpan<char> name) : base(parent, name) {
		BudgetPanel = parent;
		SamplesPerGroup = 0;

		SetProportional(false);
		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		SetVisible(true);
		SetPaintBackgroundEnabled(false);
		SetBgColor(new(0, 0, 0, 255));
		SetMinimumSize(0, 0);
	}

	public override void Paint() {
		if (SamplesPerGroup == 0)
			return;

		if (!budget_show_history.GetBool())
			return;

		int width = GetWide();
		int height = GetTall();

		int startId = SampleOffset - width;
		while (startId < 0)
			startId += SamplesPerGroup;

		int endId = startId + width;
		int numSamplesVisible = budget_history_numsamplesvisible.GetInt();
		int xOffset = 0;
		if (endId - startId > numSamplesVisible) {
			xOffset = endId - numSamplesVisible - startId;
			startId = endId - numSamplesVisible;
		}

		int rectCount = endId - startId;
		var rects = new IntRect[rectCount];
		var currentHeight = new float[rectCount];

		float oneOverRange = 1.0f / (RangeMax - RangeMin);

		for (int group = 0; group < Groups; group++) {
			for (int i = startId; i < endId; i++) {
				int sampleOffset = i % SamplesPerGroup;
				int left = i - startId + xOffset;
				int right = left + 1;

				ref float curHeight = ref currentHeight[i - startId];
				int bottom = (int)((curHeight - RangeMin) * oneOverRange * height);

				curHeight += (float)Data[sampleOffset + SamplesPerGroup * group];
				int top = (int)((curHeight - RangeMin) * oneOverRange * height);

				bottom = height - bottom - 1;
				top = height - top - 1;

				ref IntRect rect = ref rects[i - startId];
				rect.X0 = left;
				rect.X1 = right;
				rect.Y0 = top;
				rect.Y1 = bottom;
			}

			var color = BudgetPanel.ConfigData.BudgetGroupInfo[group].Color;
			Surface.DrawSetColor(color.R, color.G, color.B, color.A);
			Surface.DrawFilledRectArray(rects, rectCount);
		}

		foreach (var value in BudgetPanel.ConfigData.HistoryLabelValues)
			DrawBudgetLine(value);
	}


	void DrawBudgetLine(float val) {
		GetSize(out int width, out int height);
		double y = (val - RangeMin) * (1.0f / (RangeMax - RangeMin)) * height;
		int bottom = (int)(height - y - 1 + .5);
		int top = (int)(height - y - 1 - .5);
		Surface.DrawSetColor(0, 0, 0, 255);
		Surface.DrawFilledRect(0, top - 1, width, bottom + 1);
		Surface.DrawSetColor(255, 255, 255, 255);
		Surface.DrawFilledRect(0, top, width, bottom);
	}

	public void SetData(double[] data, int groups, int samplesPerGroup, int sampleOffset) {
		Data = data;
		Groups = groups;
		SamplesPerGroup = samplesPerGroup;
		SampleOffset = sampleOffset;
	}

	public void SetRange(float min, float max) {
		RangeMin = min;
		RangeMax = max;
	}
}