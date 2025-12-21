global using static Game.Client.WeaponsResource;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public class WeaponsResource
{
	public static readonly WeaponsResource gWR = new();

	internal void LoadWeaponSprites(WEAPON_FILE_INFO_HANDLE tmp) {
		throw new NotImplementedException();
	}
}
