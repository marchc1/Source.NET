namespace Source.GUI.Controls;

class SubRectImage : Image
{
	public SubRectImage(ReadOnlySpan<char> filename, bool hardwareFiltered, int subx, int suby, int subw, int subh) {

	}

	void GetSize(int wide, int tall) { }

	void GetContentSize(int wide, int tall) { }

	public override void SetSize(int x, int y) { }

	public override void SetPos(int x, int y) { }

	public override void SetColor(Color col) { }

	// ReadOnlySpan<char> GetName() { }

	public override void Paint() { }

	void ForceUpload() { }

	// ITexture GetID() { }

	// bool IsValid() { }
}