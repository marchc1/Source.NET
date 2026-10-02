using Source.Common.MaterialSystem;

namespace Source.Common.GarrysMod;

public interface IIntroScreen
{
	void Start();
	void End();
	void Update(ReadOnlySpan<char> status, bool step);
	void DoDraw(ref MatRenderContextPtr renderContext, ReadOnlySpan<char> status, int width, int height, float progress);
}
