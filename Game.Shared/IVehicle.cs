#if CLIENT_DLL || GAME_DLL
using Source.Common.Mathematics;

#if CLIENT_DLL
using Game.Client;
#else
using Game.Server;
#endif

using System.Numerics;

namespace Game.Shared;

public interface IVehicle
{
	BaseCombatCharacter? GetPassenger(PassengerRole role = PassengerRole.Driver);
	PassengerRole GetPassengerRole(BaseCombatCharacter? passenger);
	void GetVehicleViewPosition(PassengerRole role, out Vector3 origin, out QAngle angles, out float fov);
	bool IsPassengerUsingStandardWeapons(PassengerRole role = PassengerRole.Driver);
	// void SetupMove(BasePlayer player, ref UserCmd ucmd, IMoveHelper helper, ref MoveData move);
	// void ProcessMovement(BasePlayer player, ref MoveData moveData);
	// void FinishMove(BasePlayer? player, ref UserCmd ucmd, ref MoveData move);
	void ItemPostFrame(BasePlayer? player);
}
#endif
