using Source.Common.MaterialSystem;

namespace Source.Common.GarrysMod;

public interface IIntroScreen
{
	void Start();
	void End();
	void Update(ReadOnlySpan<char> unk1, bool unk2);
	void DoDraw(ref MatRenderContextPtr ptr, ReadOnlySpan<char> unk1, int unk2, int unk3, float unk4);
}
