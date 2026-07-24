using Source.Common.Engine;

namespace Source.Common.GarrysMod;
public interface IMenuSystem
{
	int Init(IServiceProvider services, IGet get, IGarrysMod gmod, GlobalVarsBase vars);
	void Shutdown() ;
	void SetupNetworkString(INetworkStringTableContainer networkStringTableContainer);
	void Think() ;
	void StartLua() ;
	void ServerDetails( ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2, ReadOnlySpan<char> unk3, int unk4, ReadOnlySpan<char> unk5) ;
	void OnLuaError(ref LuaError err, ref IAddonSystem.Information addonInfo) ;
	void SendProblemToMenu( ReadOnlySpan<char> id, int severity, ReadOnlySpan<char> parms  );
	bool IsServerBlacklisted( ReadOnlySpan<char> address, ReadOnlySpan<char> hostname, ReadOnlySpan<char> description, ReadOnlySpan<char> gamemode, ReadOnlySpan<char> map ) ;
}
