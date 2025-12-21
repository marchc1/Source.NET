using Game.Shared;

using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared.GarrysMod;

public static class GModWeaponParse
{
	static GModWeaponParse() {
		WeaponParse.CreateWeaponInfo = CreateWeaponInfo;
	}

	private static FileWeaponInfo CreateWeaponInfo() {
		return new HL2MPSWeaponInfo();
	}
}

public class HL2MPSWeaponInfo : FileWeaponInfo
{
	public override void Parse(KeyValues keyValuesData, ReadOnlySpan<char> weaponName) {
		base.Parse(keyValuesData, weaponName);
		PlayerDamage = keyValuesData.GetInt("damage", 0);
	}

	public int PlayerDamage;
}
