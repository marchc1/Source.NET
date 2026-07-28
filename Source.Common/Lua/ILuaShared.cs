namespace Source.Common.Lua;

public interface ILuaShared
{
	void Init(IServiceProvider services, bool isDedicated);
	ILuaInterface CreateLuaInterface(byte realm);
}
