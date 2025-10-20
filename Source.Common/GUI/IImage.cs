namespace Source.Common.GUI;

public interface IImage
{
	void Paint();
	void SetPos(int x, int y);
	void GetContentSize(out int wide, out int tall);
	void GetSize(out int wide, out int tall);
	void SetSize(int wide, int tall);
	void SetColor(Color color);
}

public enum IImageRotation
{
	Unrotated = 0,
	Clockwise_90,
	Anticlockwise_90,
	Flipped
}