using Source.Common.Formats.Keyvalues;

namespace Source.Common.GarrysMod;

public interface GModScreenspaceEffects
{
	void Init();
	void Shutdown();
	void SetParameters(KeyValues unk1);
	void Render(int unk1, int unk2, int unk3, int unk4);
	void Enable(bool unk1);
	bool IsEnabled();
}
