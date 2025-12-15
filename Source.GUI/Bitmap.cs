using Source;
using Source.Common.GUI;
using Source.Common.MaterialSystem;

class Bitmap : IImage
{
	readonly public IMatSystemSurface Surface = Singleton<IMatSystemSurface>();

	TextureID ID;
	bool Uploaded;
	bool Valid;
	string Filename;
	int[] Pos = new int[2];
	Color Color;
	bool Filtered;
	int Wide;
	int Tall;
	bool Procedural;
	TokenCache FrameCache;
	IImageRotation Rotation;

	public Bitmap(ReadOnlySpan<char> filename, bool hardwareFiltered) {
		Filtered = hardwareFiltered;
		Filename = filename.ToString();

		Procedural = false;

		if (filename.IndexOf(".pic") == 0)
			Procedural = true;

		ID = 0;
		Uploaded = false;
		Color = new(255, 255, 255, 255);
		Pos[0] = Pos[1] = 0;
		Valid = true;
		Wide = 0;
		Tall = 0;
		FrameCache = new();
		Rotation = 0;

		ForceUpload();
	}

	public void GetSize(out int wide, out int tall) {
		if (0 == Wide && 0 == Tall)
			Surface.DrawGetTextureSize(in ID, out Wide, out Tall);

		wide = Wide;
		tall = Tall;
	}

	public void GetContentSize(out int wide, out int tall) => GetSize(out wide, out tall);

	public void SetSize(int x, int y) {
		Wide = x;
		Tall = y;
	}

	public void SetPos(int x, int y) {
		Pos[0] = x;
		Pos[1] = y;
	}

	public void SetColor(Color col) => Color = col;

	ReadOnlySpan<char> GetName() => Filename;

	public void Paint() {
		if (!Valid)
			return;

		if (ID == 0)
			ID = Surface.CreateNewTextureID();

		if (!Uploaded)
			ForceUpload();

		Surface.DrawSetColor(Color[0], Color[1], Color[2], Color[3]);
		Surface.DrawSetTexture(ID);

		if (Wide == 0)
			GetSize(out Wide, out Tall);

		if (Rotation == IImageRotation.Unrotated)
			Surface.DrawTexturedRect(Pos[0], Pos[1], Pos[0] + Wide, Pos[1] + Tall);
		else {
			SurfaceVertex[] verts = new SurfaceVertex[4];
			verts[0].Position.X = 0;
			verts[0].Position.Y = 0;
			verts[1].Position.X = Wide;
			verts[1].Position.Y = 0;
			verts[2].Position.X = Wide;
			verts[2].Position.Y = Tall;
			verts[3].Position.X = 0;
			verts[3].Position.Y = Tall;

			switch (Rotation) {
				case IImageRotation.Clockwise_90:
					verts[0].TexCoord.X = 1;
					verts[0].TexCoord.Y = 0;
					verts[1].TexCoord.X = 1;
					verts[1].TexCoord.Y = 1;
					verts[2].TexCoord.X = 0;
					verts[2].TexCoord.Y = 1;
					verts[3].TexCoord.X = 0;
					verts[3].TexCoord.Y = 0;
					break;

				case IImageRotation.Anticlockwise_90:
					verts[0].TexCoord.X = 0;
					verts[0].TexCoord.Y = 1;
					verts[1].TexCoord.X = 0;
					verts[1].TexCoord.Y = 0;
					verts[2].TexCoord.X = 1;
					verts[2].TexCoord.Y = 0;
					verts[3].TexCoord.X = 1;
					verts[3].TexCoord.Y = 1;
					break;

				case IImageRotation.Flipped:
					verts[0].TexCoord.X = 1;
					verts[0].TexCoord.Y = 1;
					verts[1].TexCoord.X = 0;
					verts[1].TexCoord.Y = 1;
					verts[2].TexCoord.X = 0;
					verts[2].TexCoord.Y = 0;
					verts[3].TexCoord.X = 1;
					verts[3].TexCoord.Y = 0;
					break;

				default:
				case IImageRotation.Unrotated:
					break;
			}

			Surface.DrawTexturedPolygon(verts);
		}
	}

	void ForceUpload() {
		if (!Valid || Uploaded)
			return;

		if (ID == 0)
			ID = Surface.CreateNewTextureID(Procedural);

		if (!Procedural)
			Surface.DrawSetTextureFile(in ID, Filename, Filtered ? 1 : 0, false);

		Uploaded = true;
		Valid = Surface.IsTextureIDValid(in ID);
	}

	TextureID GetID() => ID;

	bool Evict() {
		if (ID != 0) {
			// Surface.DestroyTextureID(in ID); todo
			ID = 0;
			Uploaded = false;
			return true;
		}

		return false;
	}

	int GetNumFrames() {
		if (!Valid)
			return 0;

		return Surface.GetTextureNumFrames(in ID);
	}

	void SetFrame(int frame) {
		if (!Valid)
			return;

		Surface.DrawSetTextureFrame(in ID, frame, ref FrameCache);
	}
}