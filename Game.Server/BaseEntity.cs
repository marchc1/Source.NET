using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

using DEFINE = Source.DEFINE<BaseEntity>;
using FIELD = Source.FIELD<BaseEntity>;

#if HL2_DLL
public enum Class_T
{
	None = 0,
	Player,
	PlayerAlly,
	PlayerAllyVital,
	Antlion,
	Barnacle,
	Bullseye,
	//BULLSQUID,	
	CitizenPassive,
	CitizenRebel,
	Combine,
	CombineGunship,
	Conscript,
	Headcrab,
	//Houndeye,
	Manhack,
	MetroPolice,
	Military,
	Scanner,
	Stalker,
	Vortigaunt,
	Zombie,
	ProtoSniper,
	Missile,
	Flare,
	EarthFauna,
	HackedRollermine,
	CombineHunter,

	NumAIClasses
}

#elif HL1_DLL
#endif

public struct ResponseContext
{
	public string Name;
	public string Value;
	public TimeUnit_t ExpirationTime;
}

public struct ThinkFunc
{
	public BaseEntity.BASEPTR? Think;
	public string? Context;
	public long NextThinkTick;
	public long LastThinkTick;
}
public enum EntityEvent
{
	WaterTouch,
	WaterUntouch,
	ParentChanged
}
public enum ToggleState
{
	AtTop,
	AtBottom,
	GoingUp,
	GoingDown
}

public static class BaseEntity_ConCommands
{
	[ConCommand("ent_text", "Displays text debugging information about the given entity(ies) on top of the entity (See Overlay Text)\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_Text(in TokenizedCommand args) {

	}
	[ConCommand("ent_bbox", "Displays the movement bounding box for the given entity(ies) in orange.  Some entites will also display entity specific overlays.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_BBox(in TokenizedCommand args) {

	}
	[ConCommand("ent_absbox", "Displays the total bounding box for the given entity(s) in green.  Some entites will also display entity specific overlays.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_AbsBox(in TokenizedCommand args) {

	}
	[ConCommand("ent_rbox", "Displays the total bounding box for the given entity(s) in green.  Some entites will also display entity specific overlays.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_RBox(in TokenizedCommand args) {

	}
	[ConCommand("ent_attachments", "Displays the attachment points on an entity.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_AttachmentPoints(in TokenizedCommand args) {

	}
	[ConCommand("ent_viewoffset", "Displays the eye position for the given entity(ies) in red.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_ViewOffset(in TokenizedCommand args) {

	}
	[ConCommand("ent_remove", "Removes the given entity(s)\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_Remove(in TokenizedCommand args) {

	}
	[ConCommand("ent_remove_all", "Removes all entities of the specified type\n\tArguments:   	{entity_name} / {class_name} ", FCvar.Cheat)]
	public static void CC_Ent_RemoveAll(in TokenizedCommand args) {

	}
	[ConCommand("ent_setname", "Sets the targetname of the given entity(s)\n\tArguments:   	{new entity name} {entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_SetName(in TokenizedCommand args) {

	}
	[ConCommand("find_ent", "Find and list all entities with classnames or targetnames that contain the specified substring.\nFormat: find_ent <substring>\n", FCvar.Cheat)]
	public static void CC_Find_Ent(in TokenizedCommand args) {

	}
	[ConCommand("find_ent_index", "Display data for entity matching specified index.\nFormat: find_ent_index <index>\n", FCvar.Cheat)]
	public static void CC_Find_Ent_Index(in TokenizedCommand args) {

	}
	[ConCommand("ent_dump", "Usage:\n   ent_dump <entity name>\n", FCvar.Cheat)]
	public static void CC_Ent_Dump(in TokenizedCommand args) {

	}
	[ConCommand("ent_fire", "Usage:\n   ent_fire <target> [action] [value] [delay]\n", FCvar.Cheat)]
	public static void EntFireAutoComplete(in TokenizedCommand args) {

	}
	[ConCommand("ent_info", "Usage:\n   ent_info <class name>\n", FCvar.Cheat)]
	public static void CC_Ent_Info(in TokenizedCommand args) {

	}
	[ConCommand("ent_pause", "Toggles pausing of input/output message processing for entities.  When turned on processing of all message will stop.  Any messages displayed with 'ent_messages' will stop fading and be displayed indefinitely. To step through the messages one by one use 'ent_step'.", FCvar.Cheat)]
	public static void CC_Ent_Pause(in TokenizedCommand args) {

	}
	[ConCommand("picker", "Toggles 'picker' mode.  When picker is on, the bounding box, pivot and debugging text is displayed for whatever entity the player is looking at.\n\tArguments:	full - enables all debug information", FCvar.Cheat)]
	public static void CC_Ent_Picker(in TokenizedCommand args) {

	}
	[ConCommand("ent_pivot", "Displays the pivot for the given entity(ies).\n\t(y=up=green, z=forward=blue, x=left=red). \n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_Pivot(in TokenizedCommand args) {

	}
	[ConCommand("ent_step", "When 'ent_pause' is set this will step through one waiting input / output message at a time.", FCvar.Cheat)]
	public static void CC_Ent_Step(in TokenizedCommand args) {

	}
	[ConCommand("ent_show_response_criteria", "Print, to the console, an entity's current criteria set used to select responses.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at ", FCvar.Cheat)]
	public static void CC_Ent_Show_Response_Criteria(in TokenizedCommand args) {

	}
	[ConCommand("ent_autoaim", "Displays the entity's autoaim radius.\n\tArguments:   	{entity_name} / {class_name} / no argument picks what player is looking at", FCvar.Cheat)]
	public static void CC_Ent_Autoaim(in TokenizedCommand args) {

	}
	[ConCommand("ent_create", "Creates an entity of the given type where the player is looking.  Additional parameters can be passed in in the form: ent_create <entity name> <param 1 name> <param 1> <param 2 name> <param 2>...<param N name> <param N>", FCvar.GameDLL | FCvar.Cheat)]
	public static void CC_Ent_Create(in TokenizedCommand args) {

	}
	[ConCommand("ent_teleport", "Teleport the specified entity to where the player is looking.\n\tFormat: ent_teleport <entity name>", FCvar.Cheat)]
	public static void CC_Ent_Teleport(in TokenizedCommand args) {

	}
	[ConCommand("ent_orient", "Orient the specified entity to match the player's angles. By default, only orients target entity's YAW. Use the 'allangles' option to orient on all axis.\n\tFormat: ent_orient <entity name> <optional: allangles>", FCvar.Cheat)]
	public static void CC_Ent_Orient(in TokenizedCommand args) {

	}

}

public partial class BaseEntity : IServerEntity
{
	public static Edict? g_pForceAttachEdict;

	public delegate void BASEPTR(BaseEntity self);
	public delegate void INPUTFUNCPTR(BaseEntity self, InputData data);
	public delegate void ENTITYFUNCPTR(BaseEntity self, BaseEntity? other);
	public delegate void TOUCHPTR(BaseEntity? other);
	public delegate void USEPTR(BaseEntity? activator, BaseEntity? caller, UseType useType, float value);
	public delegate void BLOCKPTR(BaseEntity? other);

	public virtual int RequiredEdictIndex() => -1;

	public BASEPTR? FnThink;
	public TOUCHPTR? FnTouch;
	public USEPTR? FnUse;
	public BLOCKPTR? FnBlocked;

	/// <summary>
	/// Classify - returns the type of group (i.e, "houndeye", or "human military" so that NPCs with different classnames
	/// still realize that they are teammates. (overridden for NPCs that form groups)
	/// </summary>
	/// <returns></returns>
	public virtual Class_T Classify() => Class_T.None;

	static int PredictionRandomSeed = -1;
	static BasePlayer? PredictionPlayer;

	public static bool DisableTouchFuncs = false;
	public static bool AccurateTriggerBboxChecks = true;

	public const int TEAMNUM_NUM_BITS = 15; // < gmod increased 6 . 15
	public virtual bool IsPlayer() => false;
	public virtual bool IsBaseCombatCharacter() => false;
	public virtual bool IsNPC() => false;
	public virtual bool IsNextBot() => false;
	public virtual bool IsBaseCombatWeapon() => false;
	public virtual bool IsCombatItem() => false;
	public virtual bool IsNetClient() => false;
	public bool ClassMatches(ReadOnlySpan<char> classOrWildcard) => Classname.AsSpan().SequenceEqual(classOrWildcard);
	public bool NameMatches(ReadOnlySpan<char> name) => false; // todo
	public virtual bool IsPredicted() => false;
	public virtual bool IsTemplate() => false;
	public bool IsDormant() => IsEFlagSet(EFL.Dormant);

	public ReadOnlySpan<char> TeamID() {
		var team = GetTeam();
		return team == null ? "" : team.GetName();
	}

	private static void SendProxy_AnimTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseEntity entity = (BaseEntity)instance;

#if false
		BaseAnimating? animating = entity.GetBaseAnimating();
		Assert(animating != null);
		if (animating != null)
			Assert(!animating.IsUsingClientSideAnimation());
#endif

		int tickNumber = TIME_TO_TICKS(entity.AnimTime);
		long tickBase = gpGlobals.GetNetworkBase(gpGlobals.TickCount, entity.EntIndex());

		int addt = 0;
		if (tickNumber >= tickBase)
			addt = (int)((tickNumber - tickBase) & 0xff);

		outData.Int = addt;
	}

	private static void SendProxy_SimulationTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseEntity entity = (BaseEntity)instance;

		int tickNumber = TIME_TO_TICKS(entity.SimulationTime);
		long tickBase = gpGlobals.GetNetworkBase(gpGlobals.TickCount, entity.EntIndex());

		int addt = 0;
		if (tickNumber >= tickBase)
			addt = (int)((tickNumber - tickBase) & 0xff);

		outData.Int = addt;
	}

	public static SendTable DT_AnimTimeMustBeFirst = new(nameof(DT_AnimTimeMustBeFirst), [
		SendPropInt (FIELD.OF(nameof(AnimTime)), 8, PropFlags.Unsigned|PropFlags.ChangesOften|PropFlags.EncodedAgainstTickCount, proxyFn: SendProxy_AnimTime),
	]);

	public virtual int Save(ISave save) { throw new NotImplementedException(); }
	public virtual int Restore(IRestore save) { throw new NotImplementedException(); }
	public virtual bool ShouldSavePhysics() => true;
	public virtual void OnSave(IEntitySaveUtils utils) {
		// CalcAbsolutePosition();
		// CalcAbsoluteVelocity();
	}
	public virtual void OnRestore() {

	}

	public static object? SendProxy_ClientSideAnimation(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		BaseEntity entity = (BaseEntity)instance;
		BaseAnimating? animating = entity.GetBaseAnimating();

		if (animating != null /*&& !animating.IsUsingClientSideAnimation()*/)
			return data;
		else
			return null;
	}
	public static SendTable DT_PredictableId = new(nameof(DT_PredictableId), [
		SendPropPredictableId(FIELD.OF(nameof(PredictableId))),
		SendPropInt(FIELD.OF(nameof(b_IsPlayerSimulated)), 1, PropFlags.Unsigned)
	]);

	public static SendTable DT_BaseEntity = new([
		SendPropDataTable("AnimTimeMustBeFirst", DT_AnimTimeMustBeFirst, SendProxy_ClientSideAnimation),

		SendPropInt(FIELD.OF(nameof(SimulationTime)), SIMULATION_TIME_WINDOW_BITS, PropFlags.Unsigned | PropFlags.ChangesOften | PropFlags.EncodedAgainstTickCount, proxyFn: SendProxy_SimulationTime /* todo */),
		SendPropVector(NetworkVarFields.Origin, -1, PropFlags.Coord | PropFlags.ChangesOften, 0, Constants.HIGH_DEFAULT, SendProxy_Origin),
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
		SendPropFloat(NetworkVarFields.ShadowCastDistance, 12, PropFlags.Unsigned),
		SendPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		SendPropEHandle(FIELD.OF(nameof(EffectEntity))),
		SendPropEHandle(FIELD.OF(nameof(MoveParent))),
		SendPropInt(FIELD.OF(nameof(ParentAttachment)), NUM_PARENTATTACHMENT_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(MoveType)), (int)Source.MoveType.MaxBits, PropFlags.Unsigned ),
		SendPropInt(FIELD.OF(nameof(MoveCollide)), (int)Source.MoveCollide.MaxBits, PropFlags.Unsigned ),
		SendPropQAngles (NetworkVarFields.Rotation, 24, PropFlags.ChangesOften | PropFlags.RoundDown, SendProxy_Angles ),
		SendPropInt( FIELD.OF(nameof( TextureFrameIndex) ),     8, PropFlags.Unsigned ),
		SendPropDataTable( "predictable_id", DT_PredictableId, SendProxy_SendPredictableId ),
		SendPropInt(FIELD.OF(nameof(SimulatedEveryTick)),       1, PropFlags.Unsigned ),
		SendPropInt(FIELD.OF(nameof(AnimatedEveryTick)),        1, PropFlags.Unsigned ),
		SendPropBool( NetworkVarFields.AlternateSorting ),

		// The rest of this is Garry's Mod specific in order
		SendPropInt(FIELD.OF(nameof(m_takedamage)), 8),
		SendPropInt(FIELD.OF(nameof(RealClassName)), 16, PropFlags.Unsigned),

		SendPropInt(FIELD.OF(nameof(OverrideMaterial)), 16, PropFlags.Unsigned, SendProxy_OverrideMaterial),

		SendPropInt(FIELD.OF_ARRAYINDEX(nameof(OverrideSubMaterials), 0), 16, PropFlags.Unsigned),
		SendPropArray2(null, 32, "OverrideSubMaterials"),

		SendPropInt(FIELD.OF(nameof(Health)), 32, PropFlags.Normal | PropFlags.ChangesOften | PropFlags.VarInt),
		SendPropInt(NetworkVarFields.MaxHealth, 32),
		SendPropInt(FIELD.OF(nameof(SpawnFlags)), 32),
		SendPropInt(FIELD.OF(nameof(GModFlags)), 7),
		SendPropBool(FIELD.OF(nameof(OnFire))),
		SendPropFloat(FIELD.OF(nameof(CreationTime)), 0, PropFlags.NoScale),

		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Velocity), 0), 0, PropFlags.NoScale | PropFlags.ChangesOften),
		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Velocity), 1), 0, PropFlags.NoScale | PropFlags.ChangesOften),
		SendPropFloat(FIELD.OF_VECTORELEM(nameof(Velocity), 2), 0, PropFlags.NoScale | PropFlags.ChangesOften),

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

	public BaseEntity(bool serverOnly = false) {
		// todo todo

		CollisionProp().Init(this);
		NetworkProp().Init(this);

		AddEFlags(EFL.NoThinkFunction | EFL.NoGamePhysicsSimulation | EFL.UsePartitionWhenNotSolid);

		SetSolid(SolidType.None);
		ClearSolidFlags();

		SetMoveType(Source.MoveType.None);
		SetModelIndex(0);

		ClearFlags();

		if (serverOnly)
			AddEFlags(EFL.ServerOnly);

		AddEFlags(EFL.UsePartitionWhenNotSolid);
	}

	public virtual void StopLoopingSounds() { }

	public void Remove() => Util.Remove(this);

	public void MakeDormant() {
		AddEFlags(EFL.Dormant);
		SetThink(null);

		if (Edict() == null)
			return;

		eflags |= EFL.Dormant;
		AddSolidFlags(SolidFlags.NotSolid);
		SetMoveType(Source.MoveType.None);
		AddEffects(EntityEffects.NoDraw);
		SetNextThink(TICK_NEVER_THINK);
	}

	public bool IsBSPModel() {
		if (GetSolid() == SolidType.BSP)
			return true;

		Model? model = modelinfo.GetModel(GetModelIndex());
		if (GetSolid() == SolidType.VPhysics && modelinfo.GetModelType(model) == ModelType.Brush)
			return true;

		return false;
	}

	public bool IsViewable() {
		if (IsEffectActive(EntityEffects.NoDraw))
			return false;

		if (IsBSPModel()) {
			if (GetMoveType() != Source.MoveType.None)
				return true;
		}
		else if (GetModelIndex() != 0)
			return true;
		return false;
	}

	public virtual void UpdateOnRemove() {
		Util.g_bReceivedChainedUpdateOnRemove = true;

		// Virtual call to shut down any looping sounds.
		StopLoopingSounds();

		// Notifies entity listeners, etc
		gEntList.NotifyRemoveEntity(GetRefEHandle());

		if (Edict() != null) {
			AddFlag(EntityFlags.KillMe);
			if ((GetFlags() & EntityFlags.Graphed) != 0) {
				/*	<<TODO>>
				// this entity was a LinkEnt in the world node graph, so we must remove it from
				// the graph since we are removing it from the world.
				for ( int i = 0 ; i < WorldGraph.m_cLinks ; i++ )
				{
					if ( WorldGraph.m_pLinkPool [ i ].m_pLinkEnt == pev )
					{
						// if this link has a link ent which is the same ent that is removing itself, remove it!
						WorldGraph.m_pLinkPool [ i ].m_pLinkEnt = NULL;
					}
				}
				*/
			}
		}

		if (GlobalName != null) {
			// NOTE: During level shutdown the global list will suppress this
			// it assumes your changing levels or the game will end
			// causing the whole list to be flushed
			GlobalEntity.SetState(GlobalName, GlobalEState.Dead);
		}

		VPhysicsDestroyObject();

		// This is only here to allow the MOVETYPE_NONE to be set without the
		// assertion triggering. Why do we bother setting the MOVETYPE to none here?
		RemoveEffects(EntityEffects.BoneMerge);
		SetMoveType(Source.MoveType.None);

		// If we have a parent, unlink from it.
		UnlinkFromParent(this);

		// Any children still connected are orphans, mark all for delete
		List<BaseEntity> childrenList = [];
		GetAllChildren(this, childrenList);
		if (childrenList.Count != 0) {
			DevMsg(2, $"Warning: Deleting orphaned children of {GetClassname()}\n");
			for (int i = childrenList.Count() - 1; i >= 0; --i)
				Util.Remove(childrenList[i]);
		}

		SetGroundEntity(null);

		// if (DynamicModelPending) 
		// 	sg_DynamicLoadHandlers.Remove(this);


		if (IVModelInfo.IsDynamicModelIndex(ModelIndex)) {
			modelinfo.ReleaseDynamicModel(ModelIndex); // no-op if not dynamic
			ModelIndex = -1;
		}
	}

	public string GetDebugName() {
		if (this == null)
			return "<<null>>";

		return Name ?? Classname ?? "<<no name>>";
	}

	EHANDLE Parent;
	public float Gravity;
	public void SetPredictionEligible(bool canpredict) { } // nothing in game code
	public ref readonly Vector3 GetLocalOrigin() => ref __nv_Origin;
	public ref readonly QAngle GetLocalAngles() => ref __nv_Rotation;
	private static void SendProxy_OverrideMaterial(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseEntity entity = (BaseEntity)instance;
		outData.Int = entity.OverrideMaterial;
	}
	private static void SendProxy_Angles(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseEntity entity = (BaseEntity)instance;
		Assert(entity != null);

		QAngle angles;
		if (true /*entity.UseStepSimulationNetworkAngles*/)
			angles = entity.GetLocalAngles();

		outData.Vector[0] = MathLib.AngleMod(angles.X);
		outData.Vector[1] = MathLib.AngleMod(angles.Y);
		outData.Vector[2] = MathLib.AngleMod(angles.Z);
	}
	private static void SendProxy_Origin(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseEntity entity = (BaseEntity)instance;
		Assert(entity != null);

		Vector3 vector3;
		if (true /*entity.UseStepSimulationNetworkAngles*/)
			vector3 = entity.GetLocalOrigin();

		outData.Vector[0] = vector3.X;
		outData.Vector[1] = vector3.Y;
		outData.Vector[2] = vector3.Z;
	}
	protected static object? SendProxy_SendPredictableId(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		BaseEntity entity = (BaseEntity)instance;
		if (entity == null || !entity.PredictableId.IsActive())
			return null;

		int id_player_index = entity.PredictableId.GetPlayer();
		recipients.SetOnly(id_player_index);

		return instance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BaseEntity? GetMoveParent() => MoveParent.Get();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BaseEntity? FirstMoveChild() => MoveChild.Get();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BaseEntity? NextMovePeer() => MovePeer.Get();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool AnyPlayersInHierarchy_R(BaseEntity ent) {
		if (ent.IsPlayer())
			return true;

		for (BaseEntity? cur = ent.FirstMoveChild(); cur != null; cur = cur.NextMovePeer())
			if (AnyPlayersInHierarchy_R(cur))
				return true;

		return false;
	}
	public void RecalcHasPlayerChildBit() {
		if (AnyPlayersInHierarchy_R(this))
			AddEFlags(EFL.HasPlayerChild);
		else
			RemoveEFlags(EFL.HasPlayerChild);
	}
	public bool DoesHavePlayerChild() {
		return IsEFlagSet(EFL.HasPlayerChild);
	}
	public virtual void OnEntityEvent<T>(EntityEvent ev, T? data) {
		switch (ev) {
			case EntityEvent.WaterTouch:

				break;
			case EntityEvent.WaterUntouch:

				break;

			default:
				return;
		}
	}

	public void TransformStepData_ParentToWorld(BaseEntity parent) {
		// Fix up our step simulation points to be in the proper local space
		ref StepSimulationData step = ref GetDataObject<StepSimulationData>(DataObjectType.StepSimulation);
		if (!Unsafe.IsNullRef(ref step)) {
			// Convert our positions
			Util.ParentToWorldSpace(parent, ref step.Previous2.Origin, ref step.Previous2.Rotation);
			Util.ParentToWorldSpace(parent, ref step.Previous.Origin, ref step.Previous.Rotation);
		}
	}

	public void TransformStepData_ParentToParent(BaseEntity oldParent, BaseEntity newParent) {
		// Fix up our step simulation points to be in the proper local space
		ref StepSimulationData step = ref GetDataObject<StepSimulationData>(DataObjectType.StepSimulation);
		if (!Unsafe.IsNullRef(ref step)) {
			// Convert our positions
			Util.ParentToWorldSpace(oldParent, ref step.Previous2.Origin, ref step.Previous2.Rotation);
			Util.WorldToParentSpace(newParent, ref step.Previous2.Origin, ref step.Previous2.Rotation);

			Util.ParentToWorldSpace(oldParent, ref step.Previous.Origin, ref step.Previous.Rotation);
			Util.WorldToParentSpace(newParent, ref step.Previous.Origin, ref step.Previous.Rotation);
		}
	}

	public void TransformStepData_WorldToParent(BaseEntity parent) {
		// Fix up our step simulation points to be in the proper local space
		ref StepSimulationData step = ref GetDataObject<StepSimulationData>(DataObjectType.StepSimulation);
		if (!Unsafe.IsNullRef(ref step)) {
			// Convert our positions
			Util.WorldToParentSpace(parent, ref step.Previous2.Origin, ref step.Previous2.Rotation);
			Util.WorldToParentSpace(parent, ref step.Previous.Origin, ref step.Previous.Rotation);
		}
	}

	public void SetParent(string newParent, BaseEntity activator, int attachment = -1) {

	}

	public void SetParent(BaseEntity parentEntity, int attachment = -1) {
		if (attachment == -1)
			attachment = ParentAttachment;

		bool wasNotParented = GetParent() == null;
		BaseEntity? oldParent = GetParent();

		Parent.Set(parentEntity);

		if (parentEntity == this) {
			Assert(false);
			Parent.Set(null);
		}

		if (Parent.Get() == null) {
			ParentName = null;
			TransformStepData_ParentToWorld(oldParent);
			return;
		}

		ParentName = parentEntity.Name;
		RemoveSolidFlags(SolidFlags.RootParentAligned);

		if (parentEntity != null) {
			if (parentEntity.GetRootMoveParent()!.GetSolid() == SolidType.BSP)
				AddSolidFlags(SolidFlags.RootParentAligned);
			else {
				// Must be SOLID_VPHYSICS because parent might rotate
				if (GetSolid() == SolidType.BSP)
					SetSolid(SolidType.VPhysics);
			}
		}

		// set the move parent if we have one
		if (Edict() != null) {
			// add ourselves to the list
			LinkChild(Parent.Get()!, this);

			ParentAttachment = (byte)attachment;

			EntityMatrix matrix = default, childMatrix = default;
			matrix.InitFromEntity(parentEntity, ParentAttachment); // parent.world
			childMatrix.InitFromEntityLocal(this); // child.world
			Vector3 localOrigin = matrix.WorldToLocal(GetLocalOrigin());

			// I have the axes of local space in world space. (childMatrix)
			// I want to compute those world space axes in the parent's local space
			// and set that transform (as angles) on the child's object so the net
			// result is that the child is now in parent space, but still oriented the same way
			Matrix4x4 tmp = matrix.Transpose(); // world.parent
			tmp.MatrixMul(childMatrix, out matrix.Underlying); // child.parent
			MathLib.MatrixToAngles(matrix, out QAngle angles);
			SetLocalAngles(angles);
			Util.SetOrigin(this, localOrigin);

			// Move our step data into the correct space
			if (wasNotParented) {
				// Transform step data from world to parent-space
				TransformStepData_WorldToParent(this);
			}
			else {
				// Transform step data between parent-spaces
				TransformStepData_ParentToParent(oldParent, this);
			}
		}
		if (VPhysicsGetObject() != null) {
			if (VPhysicsGetObject()!.IsStatic()) {
				if (VPhysicsGetObject()!.IsAttachedToConstraint(false))
					Warning($"SetParent on static object, all constraints attached to {GetDebugName()} ({GetClassname()})will now be broken!\n");

				VPhysicsDestroyObject();
				VPhysicsInitShadow(false, false);
			}
		}
		CollisionRulesChanged();
	}


	public static void EmitSound<IRF>(scoped in IRF filter, int entIndex, scoped in EmitSound_t parms) where IRF : IRecipientFilter {
		BaseEntity? entity = Util.EntityByIndex(entIndex);
		entity?.ModifyEmitSoundParams(ref Unsafe.AsRef(in parms));
		g_SoundEmitterSystem.EmitSound(filter, entIndex, ref Unsafe.AsRef(in parms));
	}

	public virtual Vector3 GetSoundEmissionOrigin() => WorldSpaceCenter();

	public BaseEntity? GetParent() => Parent.Get();

	public static BaseEntity? GetContainingEntity(Edict? ent) {
		if (ent != null && ent.GetUnknown() != null)
			return (BaseEntity?)ent.GetUnknown()!.GetBaseEntity();
		return null;
	}

	public virtual Vector3 GetStepOrigin() => GetLocalOrigin();
	public virtual QAngle GetStepAngles() => GetLocalAngles();

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSolidFlags(SolidFlags flags) => CollisionProp().SetSolidFlags(flags);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsSolidFlagSet(SolidFlags flagMask) => CollisionProp().IsSolidFlagSet(flagMask);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public SolidFlags GetSolidFlags() => (SolidFlags)CollisionProp().GetSolidFlags();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddSolidFlags(SolidFlags flags) => CollisionProp().AddSolidFlags(flags);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void RemoveSolidFlags(SolidFlags flags) => CollisionProp().RemoveSolidFlags(flags);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsSolid() => CollisionProp().IsSolid();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSolid(SolidType val) => CollisionProp().SetSolid(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public SolidType GetSolid() => CollisionProp().GetSolid();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCollisionBounds(in Vector3 mins, in Vector3 maxs) => CollisionProp().SetCollisionBounds(in mins, in maxs);


	public Team? GetTeam() => GetGlobalTeam(TeamNum);

	IPhysicsObject? PhysicsObject = null!;
	public void VPhysicsUpdate(IPhysicsObject physics) { }
	public IPhysicsObject? VPhysicsGetObject() => PhysicsObject;

	public void VPhysicsSetObject(IPhysicsObject? physics) {
		if (PhysicsObject != null && physics != null)
			Warning($"Overwriting physics object for {GetClassname()}\n");
		PhysicsObject = physics;
	}

	bool VPhysicsInitSetup() {
		// don't support logical ents
		if (Edict() == null || IsMarkedForDeletion())
			return false;

		// If this entity already has a physics object, then it should have been deleted prior to making this call.
		Assert(PhysicsObject == null);
		VPhysicsDestroyObject();

		return true;
	}

	public IPhysicsObject? VPhysicsInitNormal(SolidType solidType, SolidFlags nSolidFlags, bool createAsleep) {
		return VPhysicsInitNormal(solidType, nSolidFlags, createAsleep, ref Unsafe.NullRef<Solid>());
	}

	public IPhysicsObject? VPhysicsInitNormal(SolidType solidType, SolidFlags nSolidFlags, bool createAsleep, ref Solid solid) {
		if (!VPhysicsInitSetup())
			return null;

		// NOTE: This has to occur before PhysModelCreate because that call will
		// call back into ShouldCollide(), which uses solidtype for rules.
		SetSolid(solidType);
		SetSolidFlags(nSolidFlags);

		// No physics
		if (solidType == SolidType.None)
			return null;

		// create a normal physics object
		IPhysicsObject? physicsObject = PhysModelCreate(this, GetModelIndex(), GetAbsOrigin(), GetAbsAngles(), ref solid);
		if (physicsObject != null) {
			VPhysicsSetObject(physicsObject);
			SetMoveType(Source.MoveType.VPhysics);

			if (!createAsleep)
				physicsObject.Wake();
		}

		return physicsObject;
	}
	public int VPhysicsGetObjectList(Span<IPhysicsObject> list) => throw new NotImplementedException();

	public bool IsFloating() => false; // TODO

	public static BaseEntity? Instance(Edict? ent) => GetContainingEntity(ent);
	public static BaseEntity? Instance(int ent) => Instance(INDEXENT(ent)!);

	public static BaseEntity? Create(ReadOnlySpan<char> name, Vector3 origin, QAngle angles, BaseEntity? owner = null) {
		BaseEntity? ent = CreateNoSpawn(name, origin, angles, owner);
		Util.DispatchSpawn(ent);
		return ent;
	}

	public static BaseEntity? CreateNoSpawn(ReadOnlySpan<char> name, Vector3 origin, QAngle angles, BaseEntity? owner = null) {
		BaseEntity? ent = CreateEntityByName(name);
		if (ent == null) {
			AssertMsg(false, "CreateNoSpawn: only works for CBaseEntities");
			return null;
		}

		ent.SetLocalOrigin(origin);
		ent.SetLocalAngles(angles);
		ent.SetOwnerEntity(owner);

		gEntList.NotifyCreateEntity(ent);

		return ent;
	}

	public byte RenderFX;
	public byte RenderMode;
	public byte OldRenderMode;
	public int Effects;
	public Source.Color ColorRender;
	public int TeamNum;
	public int CollisionGroup;
	public float Elasticity;
	[NetworkVar] public partial float ShadowCastDistance { get; set; }
	public byte ParentAttachment;
	public byte MoveType;
	public byte MoveCollide;
	public Vector3 AbsOrigin;
	public QAngle AbsRotation;
	[NetworkVar] public partial Vector3 Origin { get; set; }
	[NetworkVar] public partial QAngle Rotation { get; set; }
	public bool TextureFrameIndex;
	public bool SimulatedEveryTick;
	public bool AnimatedEveryTick;
	[NetworkVar] public partial bool AlternateSorting { get; set; }

	public byte m_takedamage;
	public ushort RealClassName;
	public ushort OverrideMaterial;
	public InlineArray32<ushort> OverrideSubMaterials;
	public int Health;
	[NetworkVar] public partial int MaxHealth { get; set; }
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

	public EHANDLE OwnerEntity = new();
	public EHANDLE EffectEntity = new();
	public EHANDLE MoveParent = new();
	public EHANDLE MoveChild = new();
	public EHANDLE MovePeer = new();
	public EHANDLE GroundEntity = new();

	public string? ModelName;

	public int LifeState;
	public Vector3 BaseVelocity;
	public int NextThinkTick;
	public int LastThinkTick;
	public byte WaterLevel;
	public byte WaterType;

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

	public int DataObjectTypes;

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

	public virtual Vector3 BodyTarget(in Vector3 posSrc, bool noisy) => WorldSpaceCenter();
	public virtual Vector3 HeadTarget(in Vector3 posSrc) => EyePosition();

	public virtual int GetMaxHealth() => MaxHealth;
	public void GetMaxHealth(int amt) => MaxHealth = amt;

	public int GetHealth() => Health;
	public int SetHealth(int amt) => Health = amt;

	public float HealthFraction() {
		if (GetMaxHealth() == 0)
			return 1.0f;

		float fraction = (float)GetHealth() / (float)GetMaxHealth();
		fraction = Math.Clamp(fraction, 0.0f, 1.0f);
		return fraction;
	}

	public int TakeHealth(float health, DamageType damageType) {
		if (Edict() == null || (Damage)m_takedamage < Damage.Yes)
			return 0;

		int iMax = GetMaxHealth();

		// heal
		if (Health >= iMax)
			return 0;

		int oldHealth = Health;

		Health += (int)health;

		if (Health > iMax)
			Health = iMax;

		return Health - oldHealth;
	}

	static int TakeDamage__warningCount = 0;

	public int TakeDamage(in TakeDamageInfo inputInfo) {
		if (null == g_pGameRules)
			return 0;

		bool bHasPhysicsForceDamage = !g_pGameRules.Damage_NoPhysicsForce(inputInfo.GetDamageType());
		if (bHasPhysicsForceDamage && inputInfo.GetDamageType() != DamageType.Generic) {
			// If you hit this assert, you've called TakeDamage with a damage type that requires a physics damage
			// force & position without specifying one or both of them. Decide whether your damage that's causing 
			// this is something you believe should impart physics force on the receiver. If it is, you need to 
			// setup the damage force & position inside the CTakeDamageInfo (Utility functions for this are in
			// takedamageinfo.cpp. If you think the damage shouldn't cause force (unlikely!) then you can set the 
			// damage type to DMG_GENERIC, or | DMG_CRUSH if you need to preserve the damage type for purposes of HUD display.

			if (inputInfo.GetDamageForce() == vec3_origin || inputInfo.GetDamagePosition() == vec3_origin) {
				if (++TakeDamage__warningCount < 10) {
					if (inputInfo.GetDamageForce() == vec3_origin)
						DevWarning("CBaseEntity::TakeDamage:  with inputInfo.GetDamageForce() == vec3_origin\n");
					if (inputInfo.GetDamagePosition() == vec3_origin)
						DevWarning("CBaseEntity::TakeDamage:  with inputInfo.GetDamagePosition() == vec3_origin\n");
				}
			}
		}

		// Make sure our damage filter allows the damage.
		if (!PassesDamageFilter(in inputInfo))
			return 0;

		if (!g_pGameRules.AllowDamage(this, in inputInfo))
			return 0;


		if (PhysIsInCallback())
			PhysCallbackDamage(this, in inputInfo);
		else {
			TakeDamageInfo info = inputInfo;

			// Scale the damage by the attacker's modifier.
			if (info.GetAttacker() != null)
				info.ScaleDamage(info.GetAttacker()!.GetAttackDamageScale(this));

			// Scale the damage by my own modifiers
			info.ScaleDamage(GetReceivedDamageScale(info.GetAttacker()));

			//Msg("%s took %.2f Damage, at %.2f\n", GetClassname(), info.GetDamage(), gpGlobals.curtime );

			return OnTakeDamage(info);
		}
		return 0;
	}

	public readonly LinkedList<DamageModifier> DamageModifiers = [];

	public virtual float GetAttackDamageScale(BaseEntity? victim) {
		float flScale = 1;
		foreach (var damageModifier in DamageModifiers)
			if (!damageModifier.IsDamageDoneToMe())
				flScale *= damageModifier.GetModifier();
		return flScale;
	}

	EHANDLE DamageFilter;

	public virtual bool PassesDamageFilter(in TakeDamageInfo info) {
		if (DamageFilter.Get() != null) {
			BaseFilter filter = (BaseFilter)DamageFilter.Get()!;
			return filter.PassesDamageFilter(in info);
		}
		return true;
	}

	public virtual float GetReceivedDamageScale(BaseEntity? victim) {
		float flScale = 1;
		foreach (var damageModifier in DamageModifiers)
			if (damageModifier.IsDamageDoneToMe())
				flScale *= damageModifier.GetModifier();
		return flScale;
	}

	public virtual void NetworkStateChanged() => NetworkProp().NetworkStateChanged();
	public virtual void NetworkStateChanged(IFieldAccessor accessor) => NetworkProp().NetworkStateChanged(accessor);

	public int VPhysicsTakeDamage(in TakeDamageInfo info) {
		// don't let physics impacts or fire cause objects to move (again)
		bool bNoPhysicsForceDamage = g_pGameRules.Damage_NoPhysicsForce(info.GetDamageType());
		if (bNoPhysicsForceDamage || info.GetDamageType() == DamageType.Generic)
			return 1;

		Assert(VPhysicsGetObject() != null);
		if (VPhysicsGetObject() != null) {
			Vector3 force = info.GetDamageForce();
			Vector3 offset = info.GetDamagePosition();

			// If you hit this assert, you've called TakeDamage with a damage type that requires a physics damage
			// force & position without specifying one or both of them. Decide whether your damage that's causing 
			// this is something you believe should impart physics force on the receiver. If it is, you need to 
			// setup the damage force & position inside the CTakeDamageInfo (Utility functions for this are in
			// takedamageinfo.cpp. If you think the damage shouldn't cause force (unlikely!) then you can set the 
			// damage type to DMG_GENERIC, or | DMG_CRUSH if you need to preserve the damage type for purposes of HUD display.
#if !TF_DLL
			Assert(force != vec3_origin && offset != vec3_origin);
#else
			// todo
#endif

			PhysicsFlags gameFlags = VPhysicsGetObject()!.GetGameFlags();
			if ((gameFlags & PhysicsFlags.PlayerHeld) != 0) {
				// if the player is holding the object, use it's real mass (player holding reduced the mass)
				BasePlayer? player = Util.GetLocalPlayer();
				if (player != null) {
					float mass = player.GetHeldObjectMass(VPhysicsGetObject()!);
					if (mass != 0.0f) {
						float ratio = VPhysicsGetObject()!.GetMass() / mass;
						force *= ratio;
					}
				}
			}
			else if ((gameFlags & PhysicsFlags.PartOfRagdoll) != 0 && (gameFlags & PhysicsFlags.ConstraintStatic) != 0) {
				IPhysicsObject[] list = ArrayPool<IPhysicsObject>.Shared.Rent(VPHYSICS_MAX_OBJECT_LIST_COUNT);
				int count = VPhysicsGetObjectList(list);
				for (int i = 0; i < count; i++) {
					if (0 == (list[i].GetGameFlags() & PhysicsFlags.ConstraintStatic)) {
						list[i].ApplyForceOffset(force, offset);
						return 1;
					}
				}

			}
			VPhysicsGetObject()!.ApplyForceOffset(force, offset);
		}

		return 1;
	}

	public ref readonly QAngle GetLocalAngularVelocity() => ref AngVelocity;

	public void ComputeAbsPosition(in Vector3 localPosition, out Vector3 absPosition) {
		BaseEntity? moveParent = GetMoveParent();
		if (moveParent == null)
			absPosition = localPosition;
		else
			MathLib.VectorTransform(localPosition, moveParent.EntityToWorldTransform(), out absPosition);
	}

	public void SetLocalAngularVelocity(in QAngle vecAngVelocity) {
		if (!IsEntityQAngleVelReasonable(vecAngVelocity)) {
			if (CheckEmitReasonablePhysicsSpew())
				Warning($"Bad SetLocalAngularVelocity({vecAngVelocity.X},{vecAngVelocity.Y},{vecAngVelocity.Z}) on {GetDebugName()}");
			Assert(false);
			return;
		}

		if (AngVelocity != vecAngVelocity)
			AngVelocity = vecAngVelocity;
	}

	public void SetLocalVelocity(in Vector3 velocity) {
		Vector3 vecVelocity = velocity;

		// Safety check against receive a huge impulse, which can explode physics
		switch (CheckEntityVelocity(ref vecVelocity)) {
			case -1:
				Warning($"Discarding SetLocalVelocity({vecVelocity.X},{vecVelocity.Y},{vecVelocity.Z}) on {GetDebugName()}\n");
				Assert(false);
				return;
			case 0:
				if (CheckEmitReasonablePhysicsSpew())
					Warning($"Clamping SetLocalVelocity({velocity.X},{velocity.Y},{velocity.Z}) on {GetDebugName()}\n");
				break;
		}

		if (Velocity != vecVelocity) {
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.VelocityChanged);
			Velocity = vecVelocity;
		}
	}

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


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref readonly Vector3 WorldAlignMins() {
		Assert(!CollisionProp().IsBoundsDefinedInEntitySpace());
		Assert(CollisionProp().GetCollisionAngles() == vec3_angle);
		return ref CollisionProp().OBBMins();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref readonly Vector3 WorldAlignMaxs() {
		Assert(!CollisionProp().IsBoundsDefinedInEntitySpace());
		Assert(CollisionProp().GetCollisionAngles() == vec3_angle);
		return ref CollisionProp().OBBMaxs();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref readonly Vector3 WorldAlignSize() {
		Assert(!CollisionProp().IsBoundsDefinedInEntitySpace());
		Assert(CollisionProp().GetCollisionAngles() == vec3_angle);
		return ref CollisionProp().OBBSize();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float BoundingRadius() => CollisionProp().BoundingRadius();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPointSized() => CollisionProp().BoundingRadius() == 0.0f;
	public virtual int OnTakeDamage(in TakeDamageInfo info) {
		Vector3 vecTemp = default;

		if (Edict() == null || (Damage)m_takedamage == 0)
			return 0;

		if (info.GetInflictor() != null)
			vecTemp = info.GetInflictor()!.WorldSpaceCenter() - (WorldSpaceCenter());
		else
			vecTemp.Init(1, 0, 0);


		// this global is still used for glass and other non-NPC killables, along with decals.
		g_vecAttackDir = vecTemp;
		MathLib.VectorNormalize(ref g_vecAttackDir);

		// save damage based on the target's armor level

		// figure momentum add (don't let hurt brushes or other triggers move player)

		// physics objects have their own calcs for this: (don't let fire move things around!)
		if (!IsEFlagSet(EFL.NoDamageForces)) {
			if ((GetMoveType() == Source.MoveType.VPhysics)) {
				VPhysicsTakeDamage(info);
			}
			else {
				if (info.GetInflictor() != null && (GetMoveType() == Source.MoveType.Walk || GetMoveType() == Source.MoveType.Step) &&
					!info.GetAttacker()!.IsSolidFlagSet(SolidFlags.Trigger)) {
					Vector3 vecDir, vecInflictorCentroid;
					vecDir = WorldSpaceCenter();
					vecInflictorCentroid = info.GetInflictor()!.WorldSpaceCenter();
					vecDir -= vecInflictorCentroid;
					MathLib.VectorNormalize(ref vecDir);

					Vector3 worldSize = WorldAlignSize();
					float flForce = info.GetDamage() * ((32 * 32 * 72.0f) / (worldSize.X * worldSize.Y * worldSize.Z)) * 5;

					if (flForce > 1000.0f)
						flForce = 1000.0f;
					ApplyAbsVelocityImpulse(vecDir * flForce);
				}
			}
		}

		if ((Damage)m_takedamage != Damage.EventsOnly) {
			// do the damage
			Health -= (int)info.GetDamage();
			if (Health <= 0) {
				Event_Killed(info);
				return 0;
			}
		}

		return 1;
	}

	public virtual void Event_KilledOther(BaseEntity killed, in TakeDamageInfo info) {

	}

	public virtual void Event_Killed(in TakeDamageInfo info) {
		info.GetAttacker()?.Event_KilledOther(this, info);

		m_takedamage = (byte)Damage.No;
		LifeState = (int)Source.LifeState.Dead;
		Util.Remove(this);
	}

	public static bool FClassnameIs(BaseEntity? entity, ReadOnlySpan<char> classname) {
		if (entity == null)
			return false;

		return 0 == strcmp(entity.GetClassname(), classname);
	}

	public bool IsServer() => true;
	public bool IsClient() => false;
	public ReadOnlySpan<char> GetDLLType() => "server";

	public WaterLevel GetWaterLevel() => (WaterLevel)WaterLevel;
	public void SetWaterLevel(WaterLevel level) => WaterLevel = (byte)level;
	public void SetMoveCollide(MoveCollide moveCollide) => MoveCollide = (byte)moveCollide;
	public CollisionProperty CollisionProp() => Collision;

	public void SetModel(ReadOnlySpan<char> modelName) {
		modelName = modelName.SliceNullTerminatedString();

		int modelIndex = modelinfo.GetModelIndex(modelName);
		Model? model = modelinfo.GetModel(modelIndex);
		if (model != null && modelinfo.GetModelType(model) != ModelType.Brush)
			Msg($"Setting CBaseEntity to non-brush model {modelName}\n");

		Util.SetModel(this, modelName);
	}

	internal void SetAbsAngles(in QAngle absAngles) {
		// This is necessary to get the other fields of m_rgflCoordinateFrame ok
		CalcAbsolutePosition();

		// FIXME: The normalize caused problems in server code like momentary_rot_button that isn't
		//        handling things like +/-180 degrees properly. This should be revisited.
		//QAngle angleNormalize( AngleNormalize( absAngles.x ), AngleNormalize( absAngles.y ), AngleNormalize( absAngles.z ) );

		if (AbsRotation == absAngles)
			return;

		// All children are invalid, but we are not
		InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnglesChanged);
		RemoveEFlags(EFL.DirtyAbsTransform);

		AbsRotation = absAngles;
		MathLib.AngleMatrix(absAngles, out CoordinateFrame);
		MathLib.MatrixSetColumn(AbsOrigin, 3, ref CoordinateFrame);

		QAngle angNewRotation = default;
		BaseEntity? moveParent = GetMoveParent();
		if (moveParent == null)
			angNewRotation = absAngles;
		else {
			if (AbsRotation == moveParent.GetAbsAngles())
				angNewRotation.Init();
			else {
				// Moveparent case: transform the abs transform into local space
				Matrix3x4 worldToParent, localMatrix;
				MathLib.MatrixInvert(moveParent.EntityToWorldTransform(), out worldToParent);
				MathLib.ConcatTransforms(worldToParent, CoordinateFrame, out localMatrix);
				MathLib.MatrixAngles(localMatrix, out angNewRotation);
			}
		}

		if (Rotation != angNewRotation) {
			Rotation = angNewRotation;
			SetSimulationTime(gpGlobals.CurTime);
		}
	}

	public string? Name;

	public string GetEntityName() {
		return Name;
	}
	public void SetName(ReadOnlySpan<char> name) {
		Name = new(name.SliceNullTerminatedString());
	}
	public void SetName(string? name) {
		Name = name;
	}
	public void SetOwnerEntity(BaseEntity? owner) => OwnerEntity.Set(owner);
	public BaseEntity? GetOwnerEntity() => OwnerEntity.Get();

	public void SetMoveType(MoveType val, MoveCollide moveCollide = Source.MoveCollide.Default) {
		if (MoveType == (byte)val) {
			MoveCollide = (byte)moveCollide;
			return;
		}

		// This is needed to the removal of MOVETYPE_FOLLOW:
		// We can't transition from follow to a different movetype directly
		// or the leaf code will break.
		Assert(!IsEffectActive(EntityEffects.BoneMerge));
		MoveType = (byte)val;
		MoveCollide = (byte)moveCollide;

		CollisionRulesChanged();

		switch ((MoveType)MoveType) {
			case Source.MoveType.Walk: {
					SetSimulatedEveryTick(true);
					SetAnimatedEveryTick(true);
				}
				break;
			case Source.MoveType.Step: {
					// This will probably go away once I remove the cvar that controls the test code
					SetSimulatedEveryTick(Physics.g_bTestMoveTypeStepSimulation);
					SetAnimatedEveryTick(false);
				}
				break;
			case Source.MoveType.Fly:
			case Source.MoveType.FlyGravity: {
					// Initialize our water state, because these movetypes care about transitions in/out of water
					UpdateWaterState();
				}
				break;
			default: {
					SetSimulatedEveryTick(true);
					SetAnimatedEveryTick(false);
				}
				break;
		}

		// This will probably go away or be handled in a better way once I remove the cvar that controls the test code
		CheckStepSimulationChanged();
		CheckHasGamePhysicsSimulation();
	}


	public bool GetCheckUntouch() => IsEFlagSet(EFL.CheckUntouch);
	public Handle<BasePlayer> PlayerSimulationOwner = new();

	void UpdateBaseVelocity() { }

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public EFL GetEFlags() => eflags;

	public Vector3 AbsVelocity;
	public QAngle AngVelocity;

	public ref readonly Vector3 GetAbsVelocity() {
		return ref AbsVelocity;
	}

	public string? Classname; // prev m_iClassname
	public string? GlobalName; // prev m_iGlobalname
	public string? ParentName; // prev m_iParent
	public int HammerID;

	public void SetClassname(ReadOnlySpan<char> classname) {
		Classname = new(classname);
	}

	public Edict Edict() => NetworkProp().Edict();

	public void PostConstructor(ReadOnlySpan<char> classname) {
		if (!classname.IsEmpty)
			SetClassname(classname);

		Assert(Classname != null);

		// Possibly get an edict, and add self to global list of entites.
		if (IsEFlagSet(EFL.ServerOnly))
			gEntList.AddNonNetworkableEntity(this);
		else {
			// Certain entities set up their edicts in the constructor
			if (!IsEFlagSet(EFL.NoAutoEdictAttach)) {
				NetworkProp().AttachEdict(g_pForceAttachEdict);
				g_pForceAttachEdict = null;
			}

			// Some ents like the player override the AttachEdict function and do it at a different time.
			// While precaching, they don't ever have an edict, so we don't need to add them to
			// the entity list in that case.
			if (Edict() != null) {
				gEntList.AddNetworkableEntity(this, EntIndex());

				// Cache our IServerNetworkable pointer for the engine for fast access.
				Edict()?.Networkable = NetworkProp();
			}
		}

		CheckHasThinkFunction(false);
		CheckHasGamePhysicsSimulation();
	}

	public virtual IServerVehicle? GetServerVehicle() => null;
	public ICollideable? GetCollideable() {
		return Collision;
	}

	public virtual ReadOnlySpan<char> GetClassname() {
		return Classname;
	}

	public static readonly DataMap DataDesc = new(typeof(BaseEntity), []);
	public virtual DataMap? GetDataDescMap() => DataDesc;

	public virtual bool AcceptInput(ReadOnlySpan<char> inputName, BaseEntity? activator, BaseEntity? caller, Variant_t value, int outputID) {
		// if (ent_messages_draw.GetBool()) todo

		for (DataMap? dmap = GetDataDescMap(); dmap != null; dmap = dmap.BaseMap) {
			for (int i = 0; i < dmap.DataNumFields; i++) {
				TypeDescription desc = dmap.DataDesc[i];
				if ((desc.Flags & FieldTypeDescFlags.Input) == 0)
					continue;

				if (stricmp(desc.ExternalName, inputName) != 0)
					continue;

				if (caller != null)
					DevMsg(2, $"({gpGlobals.CurTime:F2}) input {caller.GetEntityName()}: {GetDebugName()}.{inputName}({value.ToString()})\n");
				else
					DevMsg(2, $"({gpGlobals.CurTime:F2}) input <NULL>: {GetDebugName()}.{inputName}({value.ToString()})\n");

				// if ((DebugOverlays & OVERLAY_MESSAGE_BIT) != 0)
				// 	DrawInputOverlay(inputName, caller, value);

				if (value.FieldType() != desc.FieldType)
					if (!(value.FieldType() == Source.Common.FieldType.Void && desc.FieldType == Source.Common.FieldType.String))
						if (!value.Convert(desc.FieldType)) {
							Warning($"!! ERROR: bad input/output link:\n!! {GetClassname()}({GetDebugName()},{inputName}) doesn't match type from {(caller != null ? caller.GetClassname() : "<null>")}({(caller != null ? caller.GetEntityName() : "<null>")})\n");
							return false;
						}

				if (desc.InputFunc is INPUTFUNCPTR pfnInput) {
					InputData data = new() {
						Activator = activator,
						Caller = caller,
						Value = value,
						OutputID = outputID
					};

					pfnInput(this, data);
				}
				else if ((desc.Flags & FieldTypeDescFlags.Key) != 0) {
					value.SetOther(desc.Accessor, this);
					NetworkStateChanged();
				}

				return true;
			}
		}

		DevMsg(2, $"unhandled input: ({inputName}) -> ({GetClassname()},{GetDebugName()})\n");
		return false;
	}
	public virtual void Spawn() { }
	public virtual void Activate() { }
	public virtual void Precache() {

	}

	static bool _AllowPrecache;
	public static bool IsPrecacheAllowed() => _AllowPrecache;
	public static bool SetAllowPrecache(bool allow) => _AllowPrecache = allow;

	public static int PrecacheModel(ReadOnlySpan<char> name, bool preload = true) {
		if (name.IsStringEmpty)
			return -1;

		if (!BaseEntity.IsPrecacheAllowed())
			if (!engine.IsModelPrecached(name))
				DevMsg($"Late precache of {name} -- not necessarily a bug now that we allow ~everything to be dynamically loaded.\n");

		int idx = engine.PrecacheModel(name, preload);
		if (idx != -1)
			PrecacheModelComponents(idx);

		return idx;
	}

	public static void PrecacheModelComponents(int modelIndex) {
		Model? model = (Model?)modelinfo.GetModel(modelIndex);
		if (model != null || modelinfo.GetModelType(model) != ModelType.Studio)
			return;

		// sounds
		if (IsPC()) {
			ReadOnlySpan<char> name = modelinfo.GetModelName(model);
			if (!g_ModelSoundsCache.TryGetValue(name.Hash(), out _)) {
				ReadOnlySpan<char> extension = Path.GetExtension(name);

				if (!stristr(extension, "mdl").IsEmpty)
					DevMsg(2, $"Late precache of {name}, need to rebuild modelsounds.cache\n");
				else {
					if (extension[0] == '\0')
						Warning($"Precache of {name} ambigious (no extension specified)\n");
					else
						Warning($"Late precache of {name} (file missing?)\n");
					return;
				}
			}

			if (g_ModelSoundsCache.TryGetValue(name.Hash(), out ModelSoundsCache? entry))
				entry.PrecacheSoundList();
		}
	}

	static readonly Dictionary<UtlSymId_t, ModelSoundsCache> g_ModelSoundsCache = [];

	public bool HasSpawnFlags(int flags) => (SpawnFlags & flags) != 0;

	public int GetModelIndex() => ModelIndex;

	/// <summary>
	/// The equiv of the dtor (kinda...)
	/// see CBaseEntity::~CBaseEntity() etc
	/// </summary>
	public virtual void Term() {
		VPhysicsDestroyObject();
		DestroyAllDataObjects();

		{
			Util.g_bDisableEhandleAccess = false;
			// BaseEntity.PhysicsRemoveTouchedList(this);
			// BaseEntity.PhysicsRemoveGroundList(this);
			SetGroundEntity(null); // remove us from the ground entity if we are on it
			DestroyAllDataObjects();
			Util.g_bDisableEhandleAccess = true;

			// Remove this entity from the ent list (NOTE:  This Makes EHANDLES go NULL)
			gEntList.RemoveEntity(GetRefEHandle());
		}
	}

	public ReadOnlySpan<char> GetModelName() => ModelName;
	public void SetModelName(ReadOnlySpan<char> modelName) {
		ModelName = new(modelName.SliceNullTerminatedString());
		DispatchUpdateTransmitState();
	}

	public IServerNetworkable? GetNetworkable() => Network;

	public ref readonly BaseHandle GetRefEHandle() => ref RefEHandle;

	public bool DynamicModelAllowed;
	public bool DynamicModelSetBounds;
	public bool DynamicModelPending;

	public void SetModelIndex(int index) {
		if (IVModelInfo.IsDynamicModelIndex(index) && !(GetBaseAnimating() != null && DynamicModelAllowed)) {
			AssertMsg(false, "dynamic model support not enabled on server entity");
			index = -1;
		}

		if (index != ModelIndex) {
			if (DynamicModelPending) {
				// sg_DynamicLoadHandlers.Remove(this);
				throw new NotImplementedException("sg_DynamicLoadHandlers.Remove(this)");
			}

			modelinfo.ReleaseDynamicModel(ModelIndex);
			modelinfo.AddRefDynamicModel(index);
			ModelIndex = index;

			DynamicModelSetBounds = false;

			if (IVModelInfo.IsDynamicModelIndex(index)) {
				DynamicModelPending = true;
				throw new NotImplementedException("sg_DynamicLoadHandlers[sg_DynamicLoadHandlers.Insert(this)].Register(index)");
				// sg_DynamicLoadHandlers[sg_DynamicLoadHandlers.Insert(this)].Register(index);
			}
			else {
				DynamicModelPending = false;
				OnNewModel();
			}
		}
		DispatchUpdateTransmitState();
	}

	public void OnModelLoadComplete(Model model) {
		Assert(DynamicModelPending && IVModelInfo.IsDynamicModelIndex(ModelIndex));
		Assert(model == modelinfo.GetModel(ModelIndex));

		DynamicModelPending = false;

		if (DynamicModelSetBounds) {
			DynamicModelSetBounds = false;
			SetCollisionBoundsFromModel();
		}

		OnNewModel();
	}

	public Model? GetModel() => modelinfo.GetModel(GetModelIndex());

	public void SetCollisionBoundsFromModel() {
		if (IsDynamicModelLoading()) {
			DynamicModelSetBounds = true;
			return;
		}

		Model? model = GetModel();
		if (model != null) {
			modelinfo.GetModelBounds(model, out Vector3 mns, out Vector3 mxs);
			Util.SetSize(this, mns, mxs);
		}
	}

	protected void EnableDynamicModels() => DynamicModelAllowed = true;

	public bool IsDynamicModelLoading() => DynamicModelPending;

	protected virtual StudioHdr? OnNewModel() {
		return null;
	}


	BaseHandle RefEHandle;
	public void SetRefEHandle(in BaseHandle handle) => RefEHandle = handle;

	protected int flags;
	EFL eflags;
	public Matrix3x4 CoordinateFrame;


	public ref readonly Vector3 GetBaseVelocity() => ref BaseVelocity;
	public void SetBaseVelocity(in Vector3 v) => BaseVelocity = v;

	public void SetAbsOrigin(Vector3 vector3) {
		AssertMsg(vector3.IsValid(), "Invalid origin set");

		if (AbsOrigin == vector3)
			return;

		InvalidatePhysicsRecursive(InvalidatePhysicsBits.PositionChanged);
		RemoveEFlags(EFL.DirtyAbsVelocity);

		AbsOrigin = vector3;

		MathLib.MatrixSetColumn(in vector3, 3, ref CoordinateFrame);

		Vector3 newOrigin;
		BaseEntity? moveParent = GetMoveParent();
		if (moveParent == null)
			newOrigin = vector3;
		else {
			MathLib.ConcatTransforms(moveParent.EntityToWorldTransform(), CoordinateFrame, out Matrix3x4 tempMat);
			MathLib.VectorTransform(in vector3, in tempMat, out newOrigin);
		}

		if (Origin != newOrigin) {
			Origin = newOrigin;
			SetSimulationTime(gpGlobals.CurTime);
		}
	}

	static bool s_bAbsQueriesValid = true;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SetAbsQueriesValid(bool valid) => s_bAbsQueriesValid = valid;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsAbsQueriesValid() => s_bAbsQueriesValid;

	public ref readonly Vector3 GetAbsOrigin() {
		Assert(BaseEntity.IsAbsQueriesValid());

		if (IsEFlagSet(EFL.DirtyAbsTransform))
			this.CalcAbsolutePosition();

		return ref AbsOrigin;
	}
	public ref readonly Vector3 GetViewOffset() => ref ViewOffset;
	public ref readonly QAngle GetAbsAngles() {
		Assert(BaseEntity.IsAbsQueriesValid());

		if (IsEFlagSet(EFL.DirtyAbsTransform))
			this.CalcAbsolutePosition();

		return ref AbsRotation;
	}

	public void SetLocalOrigin(in Vector3 origin) {
		if (!IsEntityPositionReasonable(origin)) {
			if (CheckEmitReasonablePhysicsSpew())
				Warning($"Bad SetLocalOrigin({origin.X},{origin.Y},{origin.Z}) on {GetDebugName()}\n");
			Assert(false);
			return;
		}

		if (Origin != origin) {
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.PositionChanged);
			Origin = origin;
			SetSimulationTime(gpGlobals.CurTime);
		}
	}

	public void SetLocalAngles(in QAngle angles) {
		if (!IsEntityQAngleReasonable(angles)) {
			if (CheckEmitReasonablePhysicsSpew())
				Warning($"Bad SetLocalAngles({angles.X},{angles.Y},{angles.Z}) on {GetDebugName()}\n");
			AssertMsg(false, $"Bad SetLocalAngles({angles.X},{angles.Y},{angles.Z}) on {GetDebugName()}\n");
			return;
		}

		if (Rotation != angles) {
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnglesChanged);
			Rotation = angles;
			SetSimulationTime(gpGlobals.CurTime);
		}
	}

	public ref Matrix3x4 GetParentToWorldTransform(ref Matrix3x4 tempMatrix) {
		BaseEntity? moveParent = GetMoveParent();
		if (moveParent == null) {
			Assert(false);
			MathLib.SetIdentityMatrix(out tempMatrix);
			return ref tempMatrix;
		}

		if (ParentAttachment != 0) {
			BaseAnimating? animating = moveParent.GetBaseAnimating();
			if (animating != null && animating.GetAttachment(ParentAttachment, out tempMatrix))
				return ref tempMatrix;
		}

		// If we fall through to here, then just use the move parent's abs origin and angles.
		return ref moveParent.EntityToWorldTransform();
	}
	public ref Matrix3x4 EntityToWorldTransform() {
		// Assert()

		if (IsEFlagSet(EFL.DirtyAbsTransform))
			CalcAbsolutePosition();

		return ref CoordinateFrame;
	}

	readonly object CalcAbsolutePositionMutex = new();

	protected void CalcAbsolutePosition() {
		if (!IsEFlagSet(EFL.DirtyAbsTransform))
			return;

		{
#if !BUILD_GMOD
			lock (CalcAbsolutePositionMutex)
#endif
			{
				// Test again under the lock, in case another thread did the work in the interim
				if (!IsEFlagSet(EFL.DirtyAbsTransform)) {
					return;
				}

				// Plop the entity.parent matrix into m_rgflCoordinateFrame
				MathLib.AngleMatrix(Rotation, Origin, out CoordinateFrame);

				BaseEntity? moveParent = GetMoveParent();
				if (moveParent == null) {
					// no move parent, so just copy existing values
					AbsOrigin = Origin;
					AbsRotation = Rotation;
				}
				else {
					// concatenate with our parent's transform
					Matrix3x4 tmpMatrix, scratchSpace = default;
					MathLib.ConcatTransforms(GetParentToWorldTransform(ref scratchSpace), CoordinateFrame, out tmpMatrix);
					MathLib.MatrixCopy(tmpMatrix, out CoordinateFrame);

					// pull our absolute position out of the matrix
					MathLib.MatrixGetColumn(CoordinateFrame, 3, out AbsOrigin);

					// if we have any angles, we have to extract our absolute angles from our matrix
					if ((Rotation == vec3_angle) && (ParentAttachment == 0))
						// just copy our parent's absolute angles
						MathLib.VectorCopy(moveParent.GetAbsAngles(), out AbsRotation);
					else
						MathLib.MatrixAngles(CoordinateFrame, out AbsRotation);
				}

				RemoveEFlags(EFL.DirtyAbsTransform);
			}

			// Do this callback *after* we have updated the position, and (importantly) after we clear the dirty flag, because this callback can potentially
			// end up recursively calling back in here, so the dirty flag must be cleared to break the recursion in that case.
			if (HasDataObjectType(DataObjectType.PositionWatcher))
				ReportPositionChanged(this);
		}
	}

	public virtual void Use(BaseEntity? activator, BaseEntity? caller, UseType useType, float value) {
		if (FnUse != null)
			FnUse(activator, caller, useType, value);
		else
			Parent.Get()?.Use(activator, caller, useType, value);
	}

	public string? Target;
	public BaseEntity? GetNextTarget() {
		if (Target == null)
			return null;
		return gEntList.FindEntityByName(null, Target);
	}

	public void TraceAttackToTriggers(in TakeDamageInfo info, in Vector3 start, in Vector3 end, in Vector3 dir) {
		Ray ray = default;
		ray.Init(start, end);

		TriggerTraceEnum triggerTraceEnum = new(ref ray, info, dir, Mask.Shot);
		enginetrace.EnumerateEntities(ray, true, ref triggerTraceEnum);
	}

	public virtual void Think() {
		if (FnThink != null)
			FnThink(this);
	}

	public virtual EntityCapabilities ObjectCaps() {
		Model? model = GetModel();
		bool isBrush = (model != null && modelinfo.GetModelType(model) == ModelType.Brush);

		// We inherit our parent's use capabilities so that we can forward use commands
		// to our parent.
		BaseEntity? parent = GetParent();
		if (parent != null) {
			EntityCapabilities caps = parent.ObjectCaps();

			if (!isBrush)
				caps &= (EntityCapabilities.AcrossTransition | EntityCapabilities.ImpulseUse | EntityCapabilities.ContinuousUse | EntityCapabilities.OnOffUse | EntityCapabilities.DirectionalUse);
			else
				caps &= (EntityCapabilities.ImpulseUse | EntityCapabilities.ContinuousUse | EntityCapabilities.OnOffUse | EntityCapabilities.DirectionalUse);

			if (parent.IsPlayer())
				caps |= EntityCapabilities.AcrossTransition;

			return caps;
		}
		else if (!isBrush)
			return EntityCapabilities.AcrossTransition;

		return 0;
	}

	public virtual void StartTouch(BaseEntity? other) { }
	public virtual void Touch(BaseEntity? other) { }
	public virtual void EndTouch(BaseEntity? other) { }
	public virtual void StartBlocked(BaseEntity? other) { }
	public virtual void Blocked(BaseEntity? other) { }
	public virtual void EndBlocked() { }

	private void ReportPositionChanged(BaseEntity baseEntity) {
		throw new NotImplementedException();
	}

	readonly ServerNetworkProperty Network = new();
	public ServerNetworkProperty NetworkProp() => Network;
	public int EntIndex() => Network.EntIndex();
	public float GetGravity() => Gravity;
	public void SetGravity(float gravity) => Gravity = gravity;
	public void ClearSolidFlags() => CollisionProp().ClearSolidFlags();
	public object? GetBaseEntity() => this;
	public virtual BaseAnimating? GetBaseAnimating() => null;

	public virtual void ComputeWorldSpaceSurroundingBox(out Vector3 vecMins, out Vector3 vecMaxs) {
		Assert(false);
		vecMins = default;
		vecMaxs = default;
	}
	private float GetFriction() => Friction;

	internal void SetTransmit(CheckTransmitInfo info, bool always) {
		int entIndex = EntIndex();

		if (info.TransmitEdict.Get(entIndex) != 0)
			return;

		ServerNetworkProperty networkParent = NetworkProp().GetNetworkParent();

		info.TransmitEdict.Set(entIndex);

		if (always || networkParent != null)
			info.TransmitAlways.Set(entIndex);
		else
			Network.RecomputePVSInformation();

		if (networkParent != null) {
			BaseEntity? moveParent = networkParent.GetBaseEntity();
			moveParent!.SetTransmit(info, always);
		}
	}

	public virtual EdictFlags ShouldTransmit(CheckTransmitInfo info) {
		EdictFlags flags = DispatchUpdateTransmitState();

		if ((flags & EdictFlags.PVSCheck) != 0)
			return EdictFlags.PVSCheck;
		else if ((flags & EdictFlags.Always) != 0)
			return EdictFlags.Always;
		else if ((flags & EdictFlags.DontSend) != 0)
			return EdictFlags.DontSend;

		BaseEntity? recipientEntity = Instance(info.ClientEnt);
		Assert(recipientEntity != null && recipientEntity.IsPlayer());

		BasePlayer recipientPlayer = (BasePlayer)recipientEntity!;

		Team? team = recipientPlayer.GetTeam();
		if (team != null) {
			// 	if (team.ShouldTransmitToPlayer(recipientPlayer, this))
			// 		return EdictFlags.Always;
		}

		return EdictFlags.PVSCheck;
	}

	public virtual EdictFlags UpdateTransmitState() {
		Assert(g_InsideDispatchUpdateTransmitState > 0);

		if (IsEffectActive(EntityEffects.NoDraw) /*&& !MoveChild.Get()*/)
			return SetTransmitState(EdictFlags.DontSend);

		if (!IsEFlagSet(EFL.ForceCheckTransmit)) {
			if (GetModelIndex() == 0 || GetModelName().IsEmpty)
				return SetTransmitState(EdictFlags.DontSend);
		}

		if (GetModelIndex() == 1)
			return SetTransmitState(EdictFlags.Always);

		if (IsEFlagSet(EFL.InSkybox))
			return SetTransmitState(EdictFlags.Always);

		return SetTransmitState(EdictFlags.PVSCheck);
	}

	static int g_InsideDispatchUpdateTransmitState = 0;
	byte TransmitStateOwnedCounter = 0;
	public EdictFlags DispatchUpdateTransmitState() {
		Edict ed = Edict();

		if (TransmitStateOwnedCounter != 0)
			return ed != null ? ed.StateFlags : 0;

		g_InsideDispatchUpdateTransmitState++;
		EdictFlags ret = UpdateTransmitState();
		g_InsideDispatchUpdateTransmitState--;

		return ret;
	}

	public EdictFlags SetTransmitState(EdictFlags flag) {
		Edict ed = Edict();

		if (ed == null)
			return 0;

		ed.ClearTransmitState();

		EdictFlags old = ed.StateFlags;
		ed.StateFlags |= flag;

		if ((old & EdictFlags.DontSend) != (ed.StateFlags & EdictFlags.DontSend))
			engine.NotifyEdictFlagsChange(EntIndex());

		return ed.StateFlags;
	}
}

[LinkEntityToClass("info_player_start")]
[LinkEntityToClass("info_landmark")]
public class PointEntity : BaseEntity
{
	public override void Spawn() {
		SetSolid(Source.SolidType.None);
	}

	// todo
	// public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;
}

public class ServerOnlyEntity : BaseEntity
{

}

public class ServerOnlyPointEntity : ServerOnlyEntity
{

}

public class LogicalEntity : ServerOnlyEntity
{

}
