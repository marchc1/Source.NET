using Source.Common.GarrysMod.Lua;

namespace Source.Common.GarrysMod;

public interface LuaCurlHTTPRequest
{
	void Run();
	void OnThreadFinished();
	bool IsFinished();
	void DoFinish(ILuaBase lua);
	void DestroyForced();
}
