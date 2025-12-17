#if CLIENT_DLL || GAME_DLL
namespace Game.Shared;

public interface IPredictableList {
	SharedBaseEntity? GetPredictable(int slot);
	int GetPredictableCount();
}
#endif
