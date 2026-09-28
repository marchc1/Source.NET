namespace Game.Shared;

public struct InputData
{
#if CLIENT_DLL || GAME_DLL
	public BaseEntity? Activator;
	public BaseEntity? Caller;
	public Variant_t Value;
	public int OutputID;
#endif
}
