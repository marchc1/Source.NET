using Source.Common.GUI;

namespace Source.GUI.Controls;

class GraphPanel : Panel
{
	struct Sample
	{
		public float SampleEnd;
		public float Value;
	}
	LinkedList<Sample> Samples = [];
	float DomainSize;
	float MaxDomainSize;
	float MinDomainSize;
	bool MaxDomainSizeSet;
	float LowRange;
	float HighRange;
	bool UseDynamicRange;
	List<float> RangeList = [];
	int GraphBarWidth;
	int GraphBarGapWidth;

	public GraphPanel(Panel parent, ReadOnlySpan<char> name) : base(parent, name) {
		DomainSize = 100.0f;
		LowRange = 0.0f;
		HighRange = 1.0f;
		UseDynamicRange = true;
		MinDomainSize = 0.0f;
		MaxDomainSize = 0.0f;
		MaxDomainSizeSet = false;
		GraphBarWidth = 2;
		GraphBarGapWidth = 2;
	}

	void SetDisplayDomainSize(float size) {
		DomainSize = size;

		if (!MaxDomainSizeSet)
			SetMaxDomainSize(size);
	}

	void SetMinDomainSize(float size) => MinDomainSize = size;

	void SetMaxDomainSize(float size) {
		MaxDomainSize = size;
		MaxDomainSizeSet = true;
	}

	void SetUseFixedRange(float lowRange, float highRange) {
		UseDynamicRange = false;
		LowRange = lowRange;
		HighRange = highRange;
	}

	void SetUseDynamicRange(float rangeList, int numRanges) {
		UseDynamicRange = true;
		RangeList.Clear();
		for (int i = 0; i < numRanges; i++)
			RangeList.Add(rangeList);
	}

	void GetDisplayedRange(out float lowRange, out float highRange) {
		lowRange = LowRange;
		highRange = HighRange;
	}

	public void AddItem(float sampleEnd, float sampleValue) {
		if (Samples.Count > 0 && Samples.Last!.Value.Value == sampleValue) {
			var last = Samples.Last;
			last.Value = new Sample {
				Value = last.Value.Value,
				SampleEnd = sampleEnd
			};
		}
		else
			Samples.AddLast(new Sample {
				Value = sampleValue,
				SampleEnd = sampleEnd
			});

		if (MaxDomainSizeSet) {
			float freePoint = sampleEnd - MaxDomainSize;
			while (Samples.Count > 0 && Samples.First!.Value.SampleEnd < freePoint)
				Samples.RemoveFirst();
		}

		InvalidateLayout();
		Repaint();
	}


	int GetVisibleItemCount() => GetWide() / (GraphBarWidth + GraphBarGapWidth);

	public override void Paint() {
		if (Samples.Count == 0)
			return;

		var sampleNode = Samples.Last;
		int x = GetWide() - (GraphBarWidth + GraphBarGapWidth);

		float sampleSize = DomainSize / GetVisibleItemCount();

		float resampleStart = sampleNode!.Value.SampleEnd - sampleSize;
		resampleStart -= (float)(resampleStart % sampleSize);

		float barSizeMultiplier = GetTall() / (HighRange - LowRange);

		Surface.DrawSetColor(GetFgColor());

		float minValue = Samples.First!.Value.Value;
		float maxValue = Samples.First.Value.Value;

		while (x > 0 && sampleNode != null) {
			x -= GraphBarWidth + GraphBarGapWidth;

			float value = 0f;
			float maxSampleValue = 0f;
			int samplesTouched = 0;

			var prevNode = sampleNode.Previous;
			while (prevNode != null) {
				float v = sampleNode.Value.Value;

				value += v;
				samplesTouched++;

				if (v < minValue)
					minValue = v;
				if (v > maxValue)
					maxValue = v;
				if (v > maxSampleValue)
					maxSampleValue = v;

				if (resampleStart < prevNode.Value.SampleEnd) {
					sampleNode = prevNode;
					prevNode = sampleNode.Previous;
				}
				else {
					resampleStart -= sampleSize;
					break;
				}
			}

			int size = (int)(maxSampleValue * barSizeMultiplier);
			Surface.DrawFilledRect(x, GetTall() - size, x + GraphBarWidth, GetTall());
		}

		if (UseDynamicRange) {
			minValue = 0;

			for (int i = 0; i < RangeList.Count; i++) {
				if (RangeList[i] > maxValue) {
					maxValue = RangeList[i];
					break;
				}
			}

			LowRange = minValue;
			HighRange = maxValue;
		}
	}


	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetFgColor(GetSchemeColor("GraphPanel.FgColor", scheme));
		SetBgColor(GetSchemeColor("GraphPanel.BgColor", scheme));
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));
	}
}