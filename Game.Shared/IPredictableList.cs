#if CLIENT_DLL || GAME_DLL
namespace Game.Shared;

public interface IPredictableList {
	SharedBaseEntity? GetPrdictable(int slot);
	int GetPredictableCount();
}
#endif
