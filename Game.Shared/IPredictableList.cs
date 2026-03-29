#if CLIENT_DLL || GAME_DLL
namespace Game.Shared;

public interface IPredictableList {
	BaseEntity? GetPredictable(int slot);
	int GetPredictableCount();
}
#endif
