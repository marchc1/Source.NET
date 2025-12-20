using Game.Client.HUD;

using Source.Common.Formats.Keyvalues;

public class FileWeaponInfo
{
	bool ParsedScript;
	bool LoadedHudElements;
	string ClassName;
	string PrintName;
	string ViewModel;
	string WorldModel;
	string AnimationPrefix;
	int Slot;
	int Position;
	int MaxClip1;
	int MaxClip2;
	int DefaultClip1;
	int DefaultClip2;
	int Weight;
	int RumbleEffect;
	bool AutoSWitchTo;
	bool AutoSwitchFrom;
	int Flags;
	string Ammo1;
	string Ammo2;
	string[] ShootSounds = [];
	int AmmoType;
	int Ammo2Type;
	bool bMeleeWeapon;
	bool BuiltRightHanded;
	bool AllowFlipping;
	int SpriteCount;
	HudTexture? IconActive;
	HudTexture? IconInactive;
	HudTexture? IconAmmo;
	HudTexture? IconAmmo2;
	HudTexture? IconCrosshair;
	HudTexture? IconAutoaim;
	HudTexture? IconZoomedCrosshair;
	HudTexture? IconZoomedAutoaim;
	HudTexture? IconSmall;

	FileWeaponInfo() { }

	void Parse(KeyValues keyValuesData, ReadOnlySpan<char> weaponName) { }
}
