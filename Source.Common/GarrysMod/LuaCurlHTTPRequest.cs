namespace Source.Common.GarrysMod;

public interface LuaCurlHTTPRequest {
	void Run();
	void OnThreadFinished();
	bool IsFinished();
	void DoFinish(Lua.ILuaBase luaBase);
	void DestroyForced();
}
