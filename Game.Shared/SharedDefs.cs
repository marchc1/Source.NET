#if !CLIENT_DLL && !GAME_DLL
global using EHANDLE = Source.Common.BaseHandle; // < Intellisense compatibility
#endif

global using static Game.Shared.SharedDefs;

using Source;
using Source.Common;

using System.Runtime.CompilerServices;

namespace Game.Shared;

using Source.Common.Networking;

using System;

[Flags]
public enum WeaponFlags
{
	SelectionEmpty = 1 << 0,
	NoAutoReload = 1 << 1,
	NoAutoSwitchEmpty = 1 << 2,
	LimitInWorld = 1 << 3,
	Exhaustible = 1 << 4,
	DoHitLocationDmg = 1 << 5,
	NoAmmoPickups = 1 << 6,
	NoItemPickup = 1 << 7,
}
/// <summary>
/// BaseEntity.eflags
/// </summary>
[Flags]
public enum EFL : uint
{
	/// <summary>This entity is marked for death -- allows safe deletion</summary>
	KillMe = 1 << 0,
	/// <summary>Entity is dormant, no updates to client</summary>
	Dormant = 1 << 1,
	/// <summary>Noclip command is active</summary>
	NoClipActive = 1 << 2,
	/// <summary>Model is setting up its bones</summary>
	SettingUpBones = 1 << 3,
	/// <summary>Special entity not deleted on restart</summary>
	KeepOnRecreateEntities = 1 << 4,
	/// <summary>One of the child entities is a player</summary>
	HasPlayerChild = 1 << 5,
	/// <summary>Client-only: update shadow</summary>
	DirtyShadowUpdate = 1 << 6,
	/// <summary>Another entity watches events on this entity</summary>
	Notify = 1 << 7,
	/// <summary>Transmit entity even if it has no model</summary>
	ForceCheckTransmit = 1 << 8,
	/// <summary>Set on frozen bots</summary>
	BotFrozen = 1 << 9,
	/// <summary>Non-networked entity</summary>
	ServerOnly = 1 << 10,
	/// <summary>Don't attach the edict automatically</summary>
	NoAutoEdictAttach = 1 << 11,
	/// <summary>Dirty absolute transform</summary>
	DirtyAbsTransform = 1 << 12,
	/// <summary>Dirty absolute velocity</summary>
	DirtyAbsVelocity = 1 << 13,
	/// <summary>Dirty angular velocity</summary>
	DirtyAbsAngVelocity = 1 << 14,
	/// <summary>Dirty surrounding collision bounds</summary>
	DirtySurroundingCollisionBounds = 1 << 15,
	/// <summary>Dirty spatial partition</summary>
	DirtySpatialPartition = 1 << 16,
	/// <summary>Entity is in the skybox</summary>
	InSkybox = 1 << 17,
	/// <summary>Show up in partition even when not solid</summary>
	UsePartitionWhenNotSolid = 1 << 18,
	/// <summary>Entity is floating in fluid</summary>
	TouchingFluid = 1 << 19,
	/// <summary>Being lifted by barnacle</summary>
	IsBeingLiftedByBarnacle = 1 << 20,
	/// <summary>Not pushed by rotorwash</summary>
	NoRotorWashPush = 1 << 21,
	/// <summary>Entity has no think function</summary>
	NoThinkFunction = 1 << 22,
	/// <summary>Skip physics simulation</summary>
	NoGamePhysicsSimulation = 1 << 23,
	/// <summary>Check untouch</summary>
	CheckUntouch = 1 << 24,
	/// <summary>Don't block NPC line-of-sight</summary>
	DontBlockLOS = 1 << 25,
	/// <summary>NPCs shouldn't walk on this entity</summary>
	DontWalkOn = 1 << 26,
	/// <summary>Entity shouldn't dissolve</summary>
	NoDissolve = 1 << 27,
	/// <summary>Mega physcannon can't ragdoll this</summary>
	NoMegaPhysCannonRagdoll = 1 << 28,
	/// <summary>Don't adjust velocity in water</summary>
	NoWaterVelocityChange = 1 << 29,
	/// <summary>Physcannon can't pick up or punt</summary>
	NoPhysCannonInteraction = 1 << 30,
	/// <summary>Ignore forces from physics damage</summary>
	NoDamageForces = 1u << 31
}

public static class SharedDefs
{
	public const int SIMULATION_TIME_WINDOW_BITS = 8;

	public const int NOINTERP_PARITY_MAX = 4;
	public const int NOINTERP_PARITY_MAX_BITS = 2;
	public const int ANIMATION_CYCLE_BITS = 15;

	public const int MAX_VIEWMODELS = 3;
	public const int MAX_BEAM_ENTS = 10;
	public const int MAX_WEAPONS = 256;
	public const int MAX_WEAPON_SLOTS = 6;
	public const int MAX_WEAPON_POSITIONS = 20;
	public const int MAX_ITEMS = 5;
	public const int WEAPON_NOCLIP = -1;

	public const int NUM_AUDIO_LOCAL_SOUNDS = 8;

	public const int MAX_SUIT_DEVICES = 3;
	public const int MAX_AMMO_TYPES = 256;
	public const int MAX_AMMO_SLOTS = 256;
	public const int MAX_SPLINE_POINTS = 16;

	public static ClientClass WithManualClassID(this ClientClass clientClass, StaticClassIndices classID) {
		clientClass.ClassID = (int)classID;
		return clientClass;
	}

	public static ServerClass WithManualClassID(this ServerClass clientClass, StaticClassIndices classID) {
		clientClass.ClassID = (int)classID;
		return clientClass;
	}
}


public enum HideHudBits
{
	WeaponSelection = 1 << 0,
	Flashlight = 1 << 1,
	All = 1 << 2,
	Health = 1 << 3,
	PlayerDead = 1 << 4,
	NeedSuit = 1 << 5,
	MiscStatus = 1 << 6,
	Chat = 1 << 7,
	Crosshair = 1 << 8,
	VehicleCrosshair = 1 << 9,
	InVehicle = 1 << 10,
	BonusProgress = 1 << 11,

	BitCount = 12
}


[InlineArray(NUM_AUDIO_LOCAL_SOUNDS)] public struct InlineArrayNumLocalAudioSounds<T> { public T item; }
[InlineArray(MAX_AMMO_SLOTS)] public struct InlineArrayMaxAmmoSlots<T> { public T item; }
[InlineArray(NetFlow.MAX_FLOWS)] public struct InlineArrayMaxFlows<T> { public T item; }
[InlineArray(MAX_SPLINE_POINTS)] public struct InlineArrayMaxSplinePoints<T> { public T item; }
[InlineArray(Constants.MAX_PLAYERS)] public struct InlineArrayMaxPlayers<T> { public T item; }
[InlineArray(Constants.MAX_PLAYERS + 1)] public struct InlineArrayMaxPlayersPlusOne<T> { public T item; }

[InlineArray(MAX_WEAPONS)]
public struct InlineArrayNewMaxWeapons<T> where T : new()
{
	public T item;
	public InlineArrayNewMaxWeapons() { for (int i = 0; i < MAX_WEAPONS; i++) this[i] = new(); }
}


[InlineArray(MAX_VIEWMODELS)]
public struct InlineArrayNewMaxViewmodels<T> where T : new()
{
	public T item;
	public InlineArrayNewMaxViewmodels() { for (int i = 0; i < MAX_VIEWMODELS; i++) this[i] = new(); }
}
[InlineArray(Constants.MAX_PLAYERS)]
public struct InlineArrayNewMaxPlayers<T> where T : new()
{
	public T item;
	public InlineArrayNewMaxPlayers() { for (int i = 0; i < MAX_VIEWMODELS; i++) this[i] = new(); }
}

[InlineArray(Constants.MAX_PLAYERS + 1)]
public struct InlineArrayNewMaxPlayersPlus1<T> where T : new()
{
	public T item;
	public InlineArrayNewMaxPlayersPlus1() { for (int i = 0; i < MAX_VIEWMODELS; i++) this[i] = new(); }
}

[InlineArray(MAX_BEAM_ENTS)]
public struct InlineArrayNewMaxBeamEnts<T> where T : new()
{
	public T item;
	public InlineArrayNewMaxBeamEnts() { for (int i = 0; i < MAX_BEAM_ENTS; i++) this[i] = new(); }
}



[InlineArray(kMAXCONTROLPOINTS)]
public struct InlineArrayNewMaxControlPoints<T> where T : new()
{
	public const int kMAXCONTROLPOINTS = 63;
	public T item;
	public InlineArrayNewMaxControlPoints() { for (int i = 0; i < kMAXCONTROLPOINTS; i++) this[i] = new(); }
}
