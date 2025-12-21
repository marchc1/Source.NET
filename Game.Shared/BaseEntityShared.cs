#if CLIENT_DLL || GAME_DLL
global using static Game.
#if CLIENT_DLL
Client
#else
Server
#endif
	.SharedBaseEntityConstants;




#if CLIENT_DLL
global using SharedBaseEntity = Game.Client.C_BaseEntity;
using Source.Common;
using Source;
using System.Numerics;
using Source.Common.Mathematics;

using Game.Shared;
namespace Game.Client;
#else
global using SharedBaseEntity = Game.Server.BaseEntity;
using Source.Common;
using Source;
using System.Numerics;
using Source.Common.Mathematics;

using Game.Shared;
namespace Game.Server;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif

using FIELD = Source.FIELD<SharedBaseEntity>;

public static class SharedBaseEntityConstants
{
	public const int NUM_PARENTATTACHMENT_BITS = 8; // < gmod increased 6 -> 8
}

[Flags]
public enum EntityCapabilities : uint
{
	MustSpawn = 0x00000001,
	AcrossTransition = 0x00000002,
	ForceTransition = 0x00000004,
	NotifyOnTransition = 0x00000008,
	ImpulseUse = 0x00000010,
	ContinuousUse = 0x00000020,
	OnOffUse = 0x00000040,
	DirectionalUse = 0x00000080,
	UseOnGround = 0x00000100,
	UseInRadius = 0x00000200,
	SaveNonNetworkable = 0x00000400,
	Master = 0x10000000,
	WCEditPosition = 0x40000000,
	DontSave = 0x80000000
}

public enum InvalidatePhysicsBits
{
	PositionChanged = 0x1,
	AnglesChanged = 0x2,
	VelocityChanged = 0x4,
	AnimationChanged = 0x8,
}

public partial class
#if CLIENT_DLL
	C_BaseEntity
#else
	BaseEntity
#endif
{
	// TODO FIXME REVIEW: SHOULD THIS ACTUALLY GO HERE?
	public static Table DT_ScriptedEntity = new(nameof(DT_ScriptedEntity), [
#if CLIENT_DLL
		RecvPropString(FIELD.OF(nameof(ScriptName)))
#elif GAME_DLL
		SendPropString(FIELD.OF(nameof(ScriptName)))
#endif
	]);
	public static readonly Class CC_ScriptedEntity = new("ScriptedEntity", DT_ScriptedEntity);
	public InlineArrayMaxPath<char> ScriptName;

	public bool IsAnimatedEveryTick() => AnimatedEveryTick;
	public bool IsSimulatedEveryTick() => SimulatedEveryTick;

	public virtual Vector3 EyePosition() => GetAbsOrigin() + GetViewOffset();
	public virtual ref readonly QAngle EyeAngles() => ref GetAbsAngles();
	public void InvalidatePhysicsRecursive(InvalidatePhysicsBits changeFlags) {
		EFL dirtyFlags = 0;

		if ((changeFlags & InvalidatePhysicsBits.VelocityChanged) != 0)
			dirtyFlags |= EFL.DirtyAbsVelocity;

		if ((changeFlags & InvalidatePhysicsBits.PositionChanged) != 0) {
			dirtyFlags |= EFL.DirtyAbsTransform;
			// TODO: mark dirty
		}

		if ((changeFlags & InvalidatePhysicsBits.PositionChanged) != 0) {
			dirtyFlags |= EFL.DirtyAbsTransform;
			changeFlags |= InvalidatePhysicsBits.PositionChanged | InvalidatePhysicsBits.VelocityChanged;
		}

		AddEFlags(dirtyFlags);
		// todo: children
	}


	public TimeUnit_t GetAnimTime() => AnimTime;
	public TimeUnit_t GetSimulationTime() => SimulationTime;

	public void SetAnimTime(TimeUnit_t time) => AnimTime = time;
	public void SetSimulationTime(TimeUnit_t time) => SimulationTime = time;

	public static bool IsSimulatingOnAlternateTicks() => false; // TODO

	public bool IsAlive() => LifeState == (int)Source.LifeState.Alive;

	protected bool b_IsPlayerSimulated;
	public bool IsPlayerSimulated() => b_IsPlayerSimulated;

	public void AddFlag(int flag) => flags |= flag;
	public void RemoveFlag(int flag) => flags &= ~flag;
	public void ClearFlags(int flag) => flags= 0;
	public void ToggleFlag(int flag) => flags ^= flag;


	public virtual ref readonly Vector3 WorldSpaceCenter() {
		return ref GetAbsOrigin(); // todo
	}
}

#endif
