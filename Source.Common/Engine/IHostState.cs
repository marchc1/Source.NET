using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common.Engine;

public interface IHostState
{
	public void Init();
	public void Frame(double time);
	public void RunGameInit();
	public void NewGame(ReadOnlySpan<char> mapName, bool rememberLocation, bool background);
	public void LoadGame(ReadOnlySpan<char> mapName, bool rememberLocation);
	public void ChangeLevelSP(ReadOnlySpan<char> mapName, ReadOnlySpan<char> landmark);
	public void ChangeLevelMP(ReadOnlySpan<char> mapName, ReadOnlySpan<char> landmark);
	public void GameShutdown();
	public void Shutdown();
	public void Restart();
	public bool IsShuttingDown();
	public void OnClientConnected();
	public void OnClientDisconnected();
	public void SetSpawnPoint(in Vector3 pos, in QAngle angles);
}
