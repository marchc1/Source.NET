using Source.Common.Formats.Keyvalues;

namespace Source.Common.GarrysMod;

public interface GMODScreenspaceEffects
{
	void Init();
	void Shutdown();
	void SetParameters(KeyValues parms);
	void Render(int r, int g, int b, int a);
	void Enable(bool enabled);
	bool IsEnabled();
}
