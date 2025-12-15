using Source.Common.GUI;
using Source.GUI.Controls;

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

	}

	void SetDisplayDomainSize(float size) { }

	void SetMinDomainSize(float size) { }

	void SetMaxDomainSize(float size) { }

	void SetUseFixedRange(float lowRange, float highRange) { }

	void SetUseDynamicRange(float rangeList, int numRanges) { }

	void GetDisplayedRange(float lowRange, float highRange) { }

	void AddItem(float sampleEnd, float sampleValue) { }

	// int GetVisibleItemCount() { }

	public override void PerformLayout() { }

	public override void Paint() { }

	public override void ApplySchemeSettings(IScheme scheme) { }
}