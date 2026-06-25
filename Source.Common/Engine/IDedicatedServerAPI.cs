namespace Source.Common.Engine;

public interface IDedicatedServerAPI : IEngineAPI
{
	bool ModInit(in StartupInfo modInfo);
	void ModShutdown();
	bool RunFrame();
	void AddConsoleText(ReadOnlySpan<char> text);
	void UpdateStatus(out float fps, out int active, out int maxPlayers, Span<char> map);
	void UpdateHostname(Span<char> hostname);
}
