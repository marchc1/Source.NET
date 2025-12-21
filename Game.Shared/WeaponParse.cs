global using WEAPON_FILE_INFO_HANDLE = ushort;
using Source.Common.Filesystem;
using Source;


#if CLIENT_DLL
using Game.Client.HUD;
#endif

using Source.Common.Formats.Keyvalues;

namespace Game.Shared;

public enum WeaponSound
{
	Empty,
	Single,
	SingleNPC,
	Double,
	DoubleNPC,
	Burst,
	Reload,
	ReloadNPC,
	MeleeMiss,
	MeleeHit,
	MeleeHitWorld,
	Special1,
	Special2,
	Special3,
	Taunt,
	Deploy,

	Num
}

public delegate FileWeaponInfo CreateWeaponInfoFn();

public static class WeaponParse
{
	public static CreateWeaponInfoFn CreateWeaponInfo = null!;

	static readonly FileWeaponInfo NullWeaponInfo = new();
	static WEAPON_FILE_INFO_HANDLE currentHandle = 0;
	static readonly Dictionary<ulong, WEAPON_FILE_INFO_HANDLE> WeaponInfoHashDatabase = [];
	static readonly Dictionary<WEAPON_FILE_INFO_HANDLE, FileWeaponInfo> WeaponInfoDatabase = [];

	public static int GetWeaponSoundFromString(ReadOnlySpan<char> str) {
		throw new NotImplementedException();
	}

	internal static void PrecacheFileWeaponInfoDatabase(IFileSystem filesystem) {
		if (WeaponInfoDatabase.Count != 0)
			return;

		Span<char> fileBase = stackalloc char[512];

		KeyValues manifest = new("weaponscripts");
		if (manifest.LoadFromFile(filesystem, "scripts/weapon_manifest.txt", "GAME")) {
			for (KeyValues? sub = manifest.GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
				if (stricmp(sub.Name, "file") == 0) {
					fileBase.Clear();
					sub.GetString().FileBase(fileBase);
					WEAPON_FILE_INFO_HANDLE tmp;
#if CLIENT_DLL
					if (ReadWeaponDataFromFileForSlot(filesystem, fileBase, out tmp))
						gWR.LoadWeaponSprites(tmp);
#else
					ReadWeaponDataFromFileForSlot(filesystem, fileBase, out tmp);
#endif
				}
				else
					Error($"Expecting 'file', got {sub.Name}\n");
			}
		}
	}

	private static bool ReadWeaponDataFromFileForSlot(IFileSystem filesystem, ReadOnlySpan<char> weaponName, out WEAPON_FILE_INFO_HANDLE handle) {
		handle = FindWeaponInfoSlot(weaponName);
		FileWeaponInfo? fileInfo = GetFileWeaponInfoFromHandle(handle);
		Assert(fileInfo != null);
		if (fileInfo.ParsedScript)
			return true;

		Span<char> sz = stackalloc char[128];
		sprintf(sz, "scripts/%s").S(weaponName);

		KeyValues keyValues = new KeyValues();
		if (!keyValues.LoadFromFile(filesystem, sz.SliceNullTerminatedString()))
			return false;

		fileInfo.Parse(keyValues, weaponName);

		return true;
	}

	private static FileWeaponInfo? GetFileWeaponInfoFromHandle(WEAPON_FILE_INFO_HANDLE handle) {
		return WeaponInfoDatabase.TryGetValue(handle, out var info) ? info : null;
	}	

	private static WEAPON_FILE_INFO_HANDLE LookupWeaponInfoSlot(ReadOnlySpan<char> weaponName) {
		return WeaponInfoHashDatabase.TryGetValue(weaponName.Hash(), out var handle) ? handle : unchecked((WEAPON_FILE_INFO_HANDLE)(-1));
	}

	private static WEAPON_FILE_INFO_HANDLE FindWeaponInfoSlot(ReadOnlySpan<char> weaponName) {
		UtlSymId_t hash = weaponName.Hash();
		if (WeaponInfoHashDatabase.TryGetValue(hash, out var lookup))
			return lookup;

		FileWeaponInfo insert = CreateWeaponInfo();

		lookup = currentHandle++;
		WeaponInfoDatabase[lookup] = insert;
		WeaponInfoHashDatabase[hash] = lookup;
		return lookup;
	}

	public const int MAX_SHOOT_SOUNDS = 16;
	public const int MAX_WEAPON_STRING = 80;
	public const int MAX_WEAPON_PREFIX = 16;
	public const int MAX_WEAPON_AMMO_NAME = 32;

	public const string WEAPON_PRINTNAME_MISSING = "!!! Missing printname on weapon";
}

public class FileWeaponInfo
{
	public virtual void Parse(KeyValues keyValuesData, ReadOnlySpan<char> weaponName) {
		ParsedScript = true;

		strcpy(ClassName, weaponName);
		strcpy(PrintName, keyValuesData.GetString("printname", WeaponParse.WEAPON_PRINTNAME_MISSING));

		strcpy(ViewModel, keyValuesData.GetString("viewmodel"));
		strcpy(WorldModel, keyValuesData.GetString("playermodel"));
		strcpy(AnimationPrefix, keyValuesData.GetString("anim_prefix"));
		Slot = keyValuesData.GetInt("bucket", 0);
		Position = keyValuesData.GetInt("bucket_position", 0);

#if CLIENT_DLL
		// todo: hud_fastswitch
#endif

		MaxClip1 = keyValuesData.GetInt("clip_size", WEAPON_NOCLIP);
		MaxClip2 = keyValuesData.GetInt("clip2_size", WEAPON_NOCLIP);
		DefaultClip1 = keyValuesData.GetInt("default_clip", MaxClip1);
		DefaultClip2 = keyValuesData.GetInt("default_clip2", MaxClip2);
		Weight = keyValuesData.GetInt("weight", 0);

		// todo: the rest of this!
	}

	public bool ParsedScript;
	public bool LoadedHudElements;
	public readonly char[] ClassName = new char[WeaponParse.MAX_WEAPON_STRING];
	public readonly char[] PrintName = new char[WeaponParse.MAX_WEAPON_STRING];        // Name for showing in HUD, etc.
	public readonly char[] ViewModel = new char[WeaponParse.MAX_WEAPON_STRING];        // View model of this weapon
	public readonly char[] WorldModel = new char[WeaponParse.MAX_WEAPON_STRING];       // Model of this weapon seen carried by the player
	public readonly char[] AnimationPrefix = new char[WeaponParse.MAX_WEAPON_PREFIX];  // Prefix of the animations that should be used by the player carrying this weapon
	public int Slot;                                  // inventory slot.
	public int Position;                              // position in the inventory slot.
	public int MaxClip1;                              // max primary clip size (-1 if no clip)
	public int MaxClip2;                              // max secondary clip size (-1 if no clip)
	public int DefaultClip1;                          // amount of primary ammo in the gun when it's created
	public int DefaultClip2;                          // amount of secondary ammo in the gun when it's created
	public int Weight;                                // this value used to determine this weapon's importance in autoselection.
	public int RumbleEffect;                          // Which rumble effect to use when fired? (xbox)
	public bool AutoSwitchTo;                         // whether this weapon should be considered for autoswitching to
	public bool AutoSwitchFrom;                       // whether this weapon can be autoswitched away from when picking up another weapon or ammo
	public int Flags;                                 // miscellaneous weapon flags
	public readonly char[] Ammo1 = new char[WeaponParse.MAX_WEAPON_AMMO_NAME];         // "primary" ammo type
	public readonly char[] Ammo2 = new char[WeaponParse.MAX_WEAPON_AMMO_NAME];         // "secondary" ammo type
	public readonly char[,] ShootSounds = new char[(int)WeaponSound.Num, WeaponParse.MAX_WEAPON_STRING];
	public int AmmoType;
	public int Ammo2Type;
	public bool MeleeWeapon;
	// This tells if the weapon was built right-handed (defaults to true).
	// This helps cl_righthand make the decision about whether to flip the model or not.
	public bool BuiltRightHanded;
	public bool AllowFlipping;  // False to disallow flipping the model, regardless of whether
								// it is built left or right handed.

#if CLIENT_DLL
	public int SpriteCount;
	public HudTexture IconActive;
	public HudTexture IconInactive;
	public HudTexture IconAmmo;
	public HudTexture IconAmmo2;
	public HudTexture IconCrosshair;
	public HudTexture IconAutoaim;
	public HudTexture IconZoomedCrosshair;
	public HudTexture IconZoomedAutoaim;
	public HudTexture IconSmall;
#endif

}
