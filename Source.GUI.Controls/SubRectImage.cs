using Source.Common.GUI;

namespace Source.GUI.Controls;

class SubRectImage : Image
{
	TextureID ID;
	int[] Sub = new int[4];
	string Filename;
	int[] Pos = new int[2];
	int Wide;
	int Tall;
	Color Color;
	bool Uploaded;
	bool Valid;
	bool Filtered;

	public SubRectImage(ReadOnlySpan<char> filename, bool hardwareFiltered, int subx, int suby, int subw, int subh) {
		SetSize(subw, subh);
		Sub[0] = subx;
		Sub[1] = suby;
		Sub[2] = subw;
		Sub[3] = subh;
		Filtered = hardwareFiltered;
		Filename = "vgui/" + filename.ToString();
		ID = TextureID.INVALID;
		Uploaded = false;
		Color = new(255, 255, 255, 255);
		Pos[0] = Pos[1] = 0;
		Valid = true;
		Wide = subw;
		Tall = subh;
		ForceUpload();
	}

	public override void GetSize(out int wide, out int tall) {
		wide = Wide;
		tall = Tall;
	}

	public override void GetContentSize(out int wide, out int tall) {
		wide = 0;
		tall = 0;

		if (!Valid)
			return;

		if (ID != TextureID.INVALID)
			Surface.DrawGetTextureSize(ID, out wide, out tall);
	}

	public override void SetSize(int x, int y) {
		Wide = x;
		Tall = y;
	}

	public override void SetPos(int x, int y) {
		Pos[0] = x;
		Pos[1] = y;
	}

	public override void SetColor(Color col) => Color = col;

	ReadOnlySpan<char> GetName() => Filename;

	public override void Paint() {
		if (!Valid)
			return;

		if (ID == TextureID.INVALID)
			ID = Surface.CreateNewTextureID();

		if (!Uploaded)
			ForceUpload();

		Surface.DrawSetColor(Color.R, Color.G, Color.B, Color.A);
		Surface.DrawSetTexture(ID);

		if (Wide == 0 || Tall == 0)
			return;

		GetContentSize(out int cwide, out int ctall);
		if (cwide == 0 || ctall == 0)
			return;

		float[] s = new float[2];
		float[] t = new float[2];

		s[0] = Sub[0] / cwide;
		t[0] = Sub[1] / ctall;
		s[1] = (Sub[0] + Sub[2]) / cwide;
		t[1] = (Sub[1] + Sub[3]) / ctall;

		Surface.DrawTexturedSubRect(Pos[0], Pos[1], Pos[0] + Wide, Pos[1] + Tall, s[0], t[0], s[1], t[1]);
	}

	void ForceUpload() {
		if (!Valid || Uploaded)
			return;

		if (ID == TextureID.INVALID)
			ID = Surface.CreateNewTextureID();

		Surface.DrawSetTextureFile(ID, Filename, Filtered ? 1 : 0, false);
		Uploaded = true;
		Valid = Surface.IsTextureIDValid(ID);
	}

	public TextureID GetID() => ID;
	public bool IsValid() => Valid;
}