namespace Source.Common.Lua;

public interface ILuaShared
{
	void Init();
	ILuaInterface CreateLuaInterface(byte realm);
}
