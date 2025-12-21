global using static Game.Client.WeaponsResource;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public class WeaponsResource
{
	public static readonly WeaponsResource gWR = new();

	public void LoadAllWeaponSprites() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		for (int i = 0; i < MAX_WEAPONS; i++)
			if (player.GetWeapon(i) != null)
				LoadWeaponSprites(player.GetWeapon(i)!.GetWeaponFileInfoHandle());
	}

	internal void LoadWeaponSprites(WEAPON_FILE_INFO_HANDLE tmp) {
		throw new NotImplementedException();
	}
}
