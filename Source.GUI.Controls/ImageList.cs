using Source.Common.GUI;

namespace Source.GUI.Controls;

class BlankImage : Image
{
	public override void Paint() { }
	public override void SetPos(int x, int y) { }
	public override void GetContentSize(out int wide, out int tall) {
		wide = 0;
		tall = 0;
	}
	public override void GetSize(out int wide, out int tall) {
		wide = 0;
		tall = 0;
	}
	public override void SetSize(int wide, int tall) { }
	public override void SetColor(Color color) { }
	// public override bool Evict() => false;
	// public override int GetNumFrames() => 0;
	// public override void SetFrame(int nFrame) { }
	// public override HTexture GetID() => 0;
	// public override void SetRotation(int iRotation) { }
}

public class ImageList(bool deleteImagesWhenDone)
{
	List<IImage?> Images = [];
	bool DeleteImagesWhenDone = deleteImagesWhenDone;

	// FIXME #37
	//  ~ImageList() {
	//  	if (DeleteImagesWhenDone) {}
	//  }

	public int AddImage(IImage image) {
		Images.Add(image);
		return Images.Count - 1;
	}

	public void SetImageAtIndex(int index, IImage image) {
		while (Images.Count <= index)
			Images.Add(null);

		Images[index] = image;
	}

	public int GetImageCount() => Images.Count;
	public IImage? GetImage(int index) => Images[index];
	public bool IsValidIndex(int index) => index >= 0 && index < Images.Count;
}
