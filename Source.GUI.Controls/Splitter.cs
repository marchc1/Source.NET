using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

enum SplitterMode
{
	Horizontal = 0,
	Vertical
}

class SplitterHandle : Panel
{
	SplitterMode Mode;
	int Index;
	bool Dragging;

	public SplitterHandle(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void ApplySchemeSettings(IScheme pScheme) { }

	public override void OnMousePressed(ButtonCode code) { }

	public override void OnMouseReleased(ButtonCode code) { }

	public override void OnCursorMoved(int x, int y) { }

	public override void OnMouseDoublePressed(ButtonCode code) { }
}

class SplitterChildPanel : EditablePanel
{
	public SplitterChildPanel(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	// bool HasUserConfigSettings() {}
}

class Splitter : EditablePanel
{
	struct SplitterInfo
	{
		public SplitterChildPanel Panel;
		public SplitterHandle Handle;
		float Pos;
		bool Locked;
		int LockedSize;
	}
	List<SplitterInfo> Splitters;
	SplitterMode Mode;

	public Splitter(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	void RecreateSplitters(int count) { }

	void SetSplitterColor(Color c) { }

	void EnableBorders(bool enable) { }

	// int GetSplitterCount() { }

	// int GetSubPanelCount() { }

	public override void ApplySettings(KeyValues resourceData) { }

	// int GetPosRange() { }

	void LockChildSize(int childIndex, int size) { }

	void UnlockChildSize(int childIndex) { }

	public override void OnSizeChanged(int newWide, int newTall) { }

	// int GetSplitterPosition(int index) { }

	void SetSplitterPosition(int index, int pos) { }

	// int ComputeLockedSize(int startingIndex) { }

	void EvenlyRespaceSplitters() { }

	void RespaceSplitters(float flFractions) { }

	// public override void ApplyUserConfigSettings(KeyValues userConfig) { }

	// public override void GetUserConfigSettings(KeyValues userConfig) { }

	public override void PerformLayout() { }

	public override void GetSettings(KeyValues outResourceData) { }
}