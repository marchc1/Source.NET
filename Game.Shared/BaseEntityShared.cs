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

namespace Game.Client;
#else
global using SharedBaseEntity = Game.Server.BaseEntity;

using Source.Common;

namespace Game.Server;
#endif

using CommunityToolkit.HighPerformance;
using Source;
using System.Numerics;
using Source.Common.Mathematics;
using Game.Shared;


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
using System.Runtime.CompilerServices;

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
	public const int BASEENTITY_MSG_REMOVE_DECALS = 1;

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


	public static bool IsSimulatingOnAlternateTicks() => false; // TODO

	public bool IsAlive() => LifeState == (int)Source.LifeState.Alive;

	protected bool b_IsPlayerSimulated;
	public bool IsPlayerSimulated() => b_IsPlayerSimulated;

	public void AddFlag(EntityFlags flag) => flags |= (int)flag;
	public void RemoveFlag(EntityFlags flag) => flags &= (int)~flag;
	public void ClearFlags() => flags = 0;
	public void ToggleFlag(EntityFlags flag) => flags ^= (int)flag;

	public long GetNextThinkTick(ReadOnlySpan<char> context = default) {
		// todo
		return (long)TICK_NEVER_THINK;
	}

	public bool WillThink() {
		if (NextThinkTick > 0)
			return true;

		for (int i = 0; i < ThinkFunctions.Count; i++)
			if (ThinkFunctions[i].NextThinkTick > 0)
				return true;

		return false;
	}

	public void CheckHasThinkFunction(bool isThinking) {
		if (IsEFlagSet(EFL.NoThinkFunction) && isThinking) {
			RemoveEFlags(EFL.NoThinkFunction);
		}
		else if (!isThinking && !IsEFlagSet(EFL.NoThinkFunction) && !WillThink()) {
			AddEFlags(EFL.NoThinkFunction);
		}
	}

	public void SetViewOffset(in Vector3 v) => ViewOffset = v;

	public void SetNextThink(TimeUnit_t thinkTime, ReadOnlySpan<char> context = default) {
		int thinkTick = (thinkTime == TICK_NEVER_THINK) ? TICK_NEVER_THINK : TIME_TO_TICKS(thinkTime);

		// Are we currently in a think function with a context?
		int iIndex = 0;
		if (context.IsEmpty) {
			if (CurrentThinkContext != NO_THINK_CONTEXT) {
				Msg($"Warning: Setting base think function within think context {ThinkFunctions[CurrentThinkContext].Context}\n");
			}
			// Old system
			NextThinkTick = thinkTick;
			CheckHasThinkFunction(thinkTick == TICK_NEVER_THINK ? false : true);
			return;
		}
		else {
			// Find the think function in our list, and if we couldn't find it, register it
			iIndex = GetIndexForThinkContext(context);
			if (iIndex == NO_THINK_CONTEXT) {
				iIndex = RegisterThinkContext(context);
			}
		}

		// Old system
		ThinkFunctions.AsSpan()[iIndex].NextThinkTick = thinkTick;
		CheckHasThinkFunction(thinkTick == TICK_NEVER_THINK ? false : true);
	}

	public int RegisterThinkContext(ReadOnlySpan<char> context) {
		int iIndex = GetIndexForThinkContext(context);
		if (iIndex != NO_THINK_CONTEXT)
			return iIndex;

		// Make a new think func
		ThinkFunc sNewFunc = new();
		sNewFunc.Think = null;
		sNewFunc.NextThinkTick = 0;
		sNewFunc.Context = new string(context);

		// Insert it into our list
		ThinkFunctions.Add(sNewFunc);
		return ThinkFunctions.Count - 1;
	}

	public int GetIndexForThinkContext(ReadOnlySpan<char> context) {
		var thinkFunctions = ThinkFunctions.AsSpan();
		for (int i = 0; i < thinkFunctions.Length; i++)
			if (0 == strncmp(thinkFunctions[i].Context, context, MAX_CONTEXT_LENGTH))
				return i;

		return NO_THINK_CONTEXT;
	}


	public static BasePlayer? GetPredictionPlayer() => PredictionPlayer;
	public static void SetPredictionPlayer(BasePlayer? player) => PredictionPlayer = player;
	public static int GetPredictionRandomSeed()
#if GAME_DLL
		=> PredictionRandomSeed; // todo: this is more complex
#else
		=> PredictionRandomSeed;
#endif
	public static void SetPredictionRandomSeed(in UserCmd cmd) {
		if (Unsafe.IsNullRef(in cmd)) {
			PredictionRandomSeed = -1;
		}

		PredictionRandomSeed = cmd.RandomSeed;
#if GAME_DLL
		// todo: predictionrandomseedserver, ServerRandomSeed, etc
#endif
	}


	public virtual ref readonly Vector3 WorldSpaceCenter() {
		return ref GetAbsOrigin(); // todo
	}


	public virtual void SetEffects(EntityEffects effects) {
		if (Effects != (int)effects) {
			Effects = (int)effects;
#if !CLIENT_DLL
			// DispatchUpdateTransmitState();
#else
			UpdateVisibility();
#endif
		}
	}
	public virtual void AddEffects(EntityEffects effects) {
		Effects |= (int)effects;
		if ((effects & EntityEffects.NoDraw) != 0) {
#if !CLIENT_DLL
			// DispatchUpdateTransmitState();
#else
			UpdateVisibility();
#endif
		}
	}
	public virtual void RemoveEffects(EntityEffects effects) {
		Effects &= ~(int)effects;
		if ((effects & EntityEffects.NoDraw) != 0) {
#if !CLIENT_DLL
			// NetworkProp().MarkPVSInformationDirty();
			// DispatchUpdateTransmitState();
#else
			UpdateVisibility();
#endif
		}
	}


	public bool IsEffectActive(EntityEffects fx) {
		return ((EntityEffects)Effects & fx) != 0;
	}

	public EntityFlags GetFlags() => (EntityFlags)flags;
	public MoveType GetMoveType() => (MoveType)MoveType;

	public void CollisionRulesChanged() { } // TODO

	public void SetSimulatedEveryTick(bool sim) {
		if (SimulatedEveryTick != sim) {
			SimulatedEveryTick = sim;
#if CLIENT_DLL
			Interp_UpdateInterpolationAmounts(ref GetVarMapping());
#endif
		}
	}

	public void SetAnimatedEveryTick(bool anim) {
		if (AnimatedEveryTick != anim) {
			AnimatedEveryTick = anim;
#if CLIENT_DLL
			Interp_UpdateInterpolationAmounts(ref GetVarMapping());
#endif
		}
	}

	public TimeUnit_t GetAnimTime() => AnimTime;
	public TimeUnit_t GetSimulationTime() => SimulationTime;

	public void SetAnimTime(TimeUnit_t time) => AnimTime = time;
	public void SetSimulationTime(TimeUnit_t time) => SimulationTime = time;

	public void CheckHasGamePhysicsSimulation() {
		// todo
	}
}

#endif
