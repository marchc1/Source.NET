using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

using FIELD = Source.FIELD<BaseEntity>;
public struct ThinkFunc
{
	public BaseEntity.BASEPTR? Think;
	public string? Context;
	public long NextThinkTick;
	public long LastThinkTick;
}
public partial class BaseEntity : IServerEntity
{
	public delegate void BASEPTR();
	public delegate void ENTITYFUNCPTR(BaseEntity? other);
	public delegate void USEPTR(BaseEntity? activator, BaseEntity? caller, UseType useType, float value);

	static int PredictionRandomSeed = -1;
	static BasePlayer? PredictionPlayer;

	public static bool DisableTouchFuncs = false;
	public static bool AccurateTriggerBboxChecks = true;

	public const int TEAMNUM_NUM_BITS = 15; // < gmod increased 6 -> 15
	public virtual bool IsPlayer() => false;
	public virtual bool IsBaseCombatCharacter() => false;
	public virtual bool IsNPC() => false;
	public virtual bool IsNextBot() => false;
	public virtual bool IsBaseCombatWeapon() => false;
	public virtual bool IsCombatItem() => false;
	private static void SendProxy_AnimTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID)
		=> throw new NotImplementedException();
	private static void SendProxy_SimulationTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID)
		=> throw new NotImplementedException();

	public static SendTable DT_AnimTimeMustBeFirst = new(nameof(DT_AnimTimeMustBeFirst), [
		SendPropInt (FIELD.OF(nameof(AnimTime)), 8, PropFlags.Unsigned|PropFlags.ChangesOften|PropFlags.EncodedAgainstTickCount, proxyFn: SendProxy_AnimTime),
	]);
	public static object? SendProxy_ClientSideAnimation(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		throw new NotImplementedException();
	}
	public static SendTable DT_PredictableId = new(nameof(DT_PredictableId), [
		SendPropPredictableId(FIELD.OF(nameof(PredictableId))),
		SendPropInt(FIELD.OF(nameof(b_IsPlayerSimulated)), 1, PropFlags.Unsigned)
	]);

	public static SendTable DT_BaseEntity = new([
		SendPropDataTable("AnimTimeMustBeFirst", DT_AnimTimeMustBeFirst, SendProxy_ClientSideAnimation),

		SendPropInt(FIELD.OF(nameof(SimulationTime)), SIMULATION_TIME_WINDOW_BITS, PropFlags.Unsigned | PropFlags.ChangesOften | PropFlags.EncodedAgainstTickCount, proxyFn: SendProxy_SimulationTime /* todo */),
		SendPropVector(FIELD.OF(nameof(Origin)), -1, PropFlags.Coord | PropFlags.ChangesOften, 0, Constants.HIGH_DEFAULT, proxyFn: null /* todo */),
		SendPropInt(FIELD.OF(nameof(InterpolationFrame)), NOINTERP_PARITY_MAX_BITS, PropFlags.Unsigned),
		SendPropModelIndex(FIELD.OF(nameof(ModelIndex))),
		SendPropDataTable(nameof(Collision), FIELD.OF(nameof(Collision)), CollisionProperty.DT_CollisionProperty),
		SendPropInt(FIELD.OF(nameof(RenderFX)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(RenderMode)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Effects)), (int)EntityEffects.MaxBits, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ColorRender)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TeamNum)), TEAMNUM_NUM_BITS, 0),
		SendPropInt(FIELD.OF(nameof(CollisionGroup)), 5, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Elasticity)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ShadowCastDistance)), 12, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		SendPropEHandle(FIELD.OF(nameof(EffectEntity))),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), NUM_PARENTATTACHMENT_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(MoveType)), (int)Source.MoveType.MaxBits, PropFlags.Unsigned ),
		SendPropInt(FIELD.OF(nameof(MoveCollide)), (int)Source.MoveCollide.MaxBits, PropFlags.Unsigned ),
		SendPropQAngles (FIELD.OF(nameof(Rotation)), 24, PropFlags.ChangesOften | PropFlags.RoundDown, SendProxy_Angles ),
		SendPropInt( FIELD.OF(nameof( TextureFrameIndex) ),     8, PropFlags.Unsigned ),
		SendPropDataTable( "predictable_id", DT_PredictableId, SendProxy_SendPredictableId ),
		SendPropInt(FIELD.OF(nameof(SimulatedEveryTick)),       1, PropFlags.Unsigned ),
		SendPropInt(FIELD.OF(nameof(AnimatedEveryTick)),        1, PropFlags.Unsigned ),
		SendPropBool( FIELD.OF(nameof( AlternateSorting ))),

		// The rest of this is Garry's Mod specific in order
		SendPropInt(FIELD.OF(nameof(TakeDamage)), 8),
		SendPropInt(FIELD.OF(nameof(RealClassName)), 16, PropFlags.Unsigned),

		SendPropInt(FIELD.OF(nameof(OverrideMaterial)), 16, PropFlags.Unsigned, SendProxy_OverrideMaterial),

		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(OverrideSubMaterials), 0), 16, PropFlags.Unsigned),
		SendPropArray2(null, 32, "OverrideSubMaterials"),

		SendPropInt(FIELD.OF(nameof(Health)), 32, PropFlags.Normal | PropFlags.ChangesOften | PropFlags.VarInt),
		SendPropInt(FIELD.OF(nameof(MaxHealth)), 32),
		SendPropInt(FIELD.OF(nameof(SpawnFlags)), 32),
		SendPropInt(FIELD.OF(nameof(GModFlags)), 7),
		SendPropBool(FIELD.OF(nameof(OnFire))),
		SendPropFloat(FIELD.OF(nameof(CreationTime)), 0, PropFlags.NoScale),

		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 0), 0, PropFlags.NoScale | PropFlags.ChangesOften),
		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 1), 0, PropFlags.NoScale | PropFlags.ChangesOften),
		SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 2), 0, PropFlags.NoScale | PropFlags.ChangesOften),

		SendPropGModTable(FIELD.OF(nameof(GMOD_DataTable))),

		// Addon exposed data tables
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_bool)), SendPropBool(FIELD.OF_ARRAYINDEX(nameof(GMOD_bool), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_float)), SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(GMOD_float), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_int)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(GMOD_int), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_Vector)),SendPropVector(FIELD.OF_ARRAYINDEX(nameof(GMOD_Vector), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_QAngle)), SendPropQAngles(FIELD.OF_ARRAYINDEX(nameof(GMOD_QAngle), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(GMOD_EHANDLE)), SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(GMOD_EHANDLE), 0))),
		SendPropString(FIELD.OF(nameof(GMOD_String0))),
		SendPropString(FIELD.OF(nameof(GMOD_String1))),
		SendPropString(FIELD.OF(nameof(GMOD_String2))),
		SendPropString(FIELD.OF(nameof(GMOD_String3))),

		// Creation IDs
		SendPropInt(FIELD.OF(nameof(CreationID)), 24),
		SendPropInt(FIELD.OF(nameof(MapCreatedID)), 16),
	]);

	public ref readonly Vector3 GetLocalOrigin() => ref AbsOrigin;
	private static void SendProxy_OverrideMaterial(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		Warning("SendProxy_OverrideMaterial not yet implemented\n");
	}
	private static void SendProxy_Angles(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		Warning("SendProxy_Angles not yet implemented\n");
	}
	protected static object? SendProxy_SendPredictableId(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		Warning("SendProxy_SendPredictableId not yet implemented\n");
		return null;
	}

	public byte RenderFX;
	public byte RenderMode;
	public byte OldRenderMode;
	public int Effects;
	public Color ColorRender;
	public int TeamNum;
	public int CollisionGroup;
	public float Elasticity;
	public float ShadowCastDistance;
	public byte ParentAttachment;
	public byte MoveType;
	public byte MoveCollide;
	public Vector3 AbsOrigin;
	public QAngle AbsRotation;
	public Vector3 Origin;
	public QAngle Rotation;
	public bool TextureFrameIndex;
	public bool SimulatedEveryTick;
	public bool AnimatedEveryTick;
	public bool AlternateSorting;

	public byte TakeDamage;
	public ushort RealClassName;
	public ushort OverrideMaterial;
	public InlineArray32<ushort> OverrideSubMaterials;
	public int Health;
	public int MaxHealth;
	public int SpawnFlags;
	public int GModFlags;
	public bool OnFire;
	public float CreationTime;
	public Vector3 Velocity;
	public int CreationID;
	public int MapCreatedID;

	public readonly List<ThinkFunc> ThinkFunctions = [];
	public int CurrentThinkContext = NO_THINK_CONTEXT;

	public readonly PredictableId PredictableId = new();

	public readonly GModTable GMOD_DataTable = new();

	public int Speed;

	public readonly EHANDLE OwnerEntity = new();
	public readonly EHANDLE EffectEntity = new();
	public readonly EHANDLE MoveParent = new();
	public readonly EHANDLE GroundEntity = new();

	public int LifeState;
	public Vector3 BaseVelocity;
	public int NextThinkTick;
	public byte WaterLevel;

	InlineArray32<bool> GMOD_bool;
	InlineArray32<float> GMOD_float;
	InlineArray32<int> GMOD_int;
	InlineArray32<Vector3> GMOD_Vector;
	InlineArray32<QAngle> GMOD_QAngle;
	InlineArray32<EHANDLE> GMOD_EHANDLE; // << ENSURE THESE ARE INITIALIZED!!!!
	InlineArray512<char> GMOD_String0;
	InlineArray512<char> GMOD_String1;
	InlineArray512<char> GMOD_String2;
	InlineArray512<char> GMOD_String3;


	public static readonly ServerClass ServerClass = new ServerClass("BaseEntity", DT_BaseEntity)
																		.WithManualClassID(StaticClassIndices.CBaseEntity);

	public TimeUnit_t AnimTime;
	public TimeUnit_t SimulationTime;
	public Vector3 ViewOffset;
	public Vector3 NetworkAngles;
	public byte InterpolationFrame;
	public int ModelIndex;
	public CollisionProperty Collision = new();
	public float Friction;
	public long SimulationTick;

	public bool FClassnameIs(BaseEntity? entity, ReadOnlySpan<char> classname) {
		if (entity == null)
			return false;

		return 0 == strcmp(entity.GetClassname(), classname);
	}

	void PhysicsStep() { }
	void PhysicsPusher() { }
	void PhysicsNone() { }
	void PhysicsRigidChild() { }
	void PhysicsNoclip() { }
	void PhysicsStepRunTimestep(TimeUnit_t timestep) { }
	void PhysicsToss() { }
	void PhysicsCustom() { }
	void PerformPush(TimeUnit_t movetime) { }
	void UpdateBaseVelocity() { }
	public void DispatchUpdateTransmitState() { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsMarkedForDeletion() => (eflags & EFL.KillMe) != 0;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddEFlags(EFL flags) {
		eflags |= flags;
		if ((flags & (EFL.ForceCheckTransmit | EFL.InSkybox)) != 0)
			DispatchUpdateTransmitState();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveEFlags(EFL flags) {
		eflags &= ~flags;
		if ((flags & (EFL.ForceCheckTransmit | EFL.InSkybox)) != 0)
			DispatchUpdateTransmitState();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsEFlagSet(EFL mask) => (eflags & mask) != 0;

	Vector3 AbsVelocity;

	public ref readonly Vector3 GetAbsVelocity() {
		return ref AbsVelocity;
	}


	public BaseEntity? GetMoveParent() => MoveParent.Get();
	public virtual IServerVehicle? GetServerVehicle() => null;
	public ICollideable? GetCollideable() {
		throw new NotImplementedException();
	}

	public virtual ReadOnlySpan<char> GetClassname() {
		throw new NotImplementedException();
	}
	public virtual void Spawn() { }
	public virtual void Precache() { }

	public int GetModelIndex() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetModelName() {
		throw new NotImplementedException();
	}

	public IServerNetworkable? GetNetworkable() {
		throw new NotImplementedException();
	}

	public BaseHandle? GetRefEHandle() {
		throw new NotImplementedException();
	}

	public void SetModelIndex(int index) {
		throw new NotImplementedException();
	}

	public void SetRefEHandle(BaseHandle handle) {
		throw new NotImplementedException();
	}


	int flags;
	EFL eflags;
	public Matrix3x4 CoordinateFrame;


	public ref readonly Vector3 GetBaseVelocity() => ref BaseVelocity;
	public void SetBaseVelocity(in Vector3 v) => BaseVelocity = v;

	public void SetAbsVelocity(in Vector3 absVelocity) {
		if (AbsVelocity == absVelocity)
			return;

		// The abs velocity won't be dirty since we're setting it here
		// All children are invalid, but we are not
		InvalidatePhysicsRecursive(InvalidatePhysicsBits.VelocityChanged);
		RemoveEFlags(EFL.DirtyAbsVelocity);

		AbsVelocity = absVelocity;

		// NOTE: Do *not* do a network state change in this case.
		// m_vecVelocity is only networked for the player, which is not manual mode
		BaseEntity? moveParent = GetMoveParent();
		if (moveParent == null) {
			Velocity = absVelocity;
			return;
		}

		// First subtract out the parent's abs velocity to get a relative
		// velocity measured in world space
		Vector3 relVelocity;
		MathLib.VectorSubtract(AbsVelocity, moveParent.GetAbsVelocity(), out relVelocity);

		// Transform relative velocity into parent space
		Vector3 vNew;
		MathLib.VectorIRotate(relVelocity, moveParent.EntityToWorldTransform(), out vNew);
		Velocity = vNew;
	}

	public ref readonly Vector3 GetAbsOrigin() => ref AbsOrigin;
	public ref readonly Vector3 GetViewOffset() => ref ViewOffset;
	public ref readonly QAngle GetAbsAngles() => ref AbsRotation;

	public void SetLocalOrigin(in Vector3 origin) { } // todo
	public void SetLocalAngles(in QAngle origin) { } // todo

	public ref Matrix3x4 EntityToWorldTransform() {

		return ref CoordinateFrame;
	}
}
