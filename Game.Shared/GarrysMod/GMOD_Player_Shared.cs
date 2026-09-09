#if CLIENT_DLL || GAME_DLL

using Game.Shared;

using System;
using System.Collections.Generic;
using System.Text;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif
public partial class
#if CLIENT_DLL
C_GMOD_Player
#else
GMOD_Player
#endif
{
	public override float GetPlayerMaxSpeed() {
		float speed = Local.WalkSpeed;
		if ((Buttons & InButtons.Walk) != 0)
			speed = Local.SlowWalkSpeed;
		else if ((Buttons & InButtons.Speed) != 0)
			speed = Local.SprintSpeed;

		float maxSpeed = sv_maxspeed.GetFloat();
		if (speed > 0.0f && speed < maxSpeed)
			maxSpeed = speed;

		return maxSpeed;
	}
}

#endif
