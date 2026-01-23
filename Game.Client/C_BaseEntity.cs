global using static Game.Client.BaseEntityConsts;
global using static Game.Client.PredictableList;

using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

using FIELD = Source.FIELD<Game.Client.C_BaseEntity>;

namespace Game.Client;

public static class BaseEntityConsts
{
	public const int MULTIPLAYER_BACKUP = 90;
}

public enum InterpolateResult
{
	Stop = 0,
	Continue = 1
}

public enum EntClientFlags
{
	GettingShadowRenderBounds = 0x0001,
	DontUseIK = 0x0002,
	AlwaysInterpolate = 0x0004,
}

public class PredictionContext : IPoolableObject
{
	static readonly ObjectPool<PredictionContext> instances = new();
	public static PredictionContext Alloc() => instances.Alloc();
	public static void Free(PredictionContext instance) => instances.Free(instance);

	public void Init() { }

	public void Reset() {
		Active = false;
		CreationCommandNumber = -1;
		CreationModule = null;
		CreationLineNumber = 0;
		ServerEntity.Set(null);
	}

	public bool Active;
	public int CreationCommandNumber;
	public string? CreationModule;
	public int CreationLineNumber;
	public readonly Handle<C_BaseEntity> ServerEntity = new();
}


public struct ThinkFunc
{
	public C_BaseEntity.BASEPTR? Think;
	public string Context;
	public long NextThinkTick;
	public long LastThinkTick;
}

public class PredictableList : IPredictableList
{
	public static readonly PredictableList g_Predictables = new();
	public C_BaseEntity? GetPredictable(int slot) {
		return cl_entitylist.GetBaseEntityFromHandle(Predictables[slot]);
	}

	public int GetPredictableCount() {
		return Predictables.Count;
	}

	internal void AddToPredictableList(ClientEntityHandle? add) {
		Assert(add != null);

		if (Predictables.Contains(add))
			return;

		Predictables.Add(add);
		int count = Predictables.Count;
		if (count < 2)
			return;

		int i, j;
		for (i = 0; i < count; i++) {
			for (j = i + 1; j < count; j++) {
				ClientEntityHandle h1 = Predictables[i];
				ClientEntityHandle h2 = Predictables[j];

				C_BaseEntity? p1 = cl_entitylist.GetBaseEntityFromHandle(h1);
				C_BaseEntity? p2 = cl_entitylist.GetBaseEntityFromHandle(h2);

				if (p1 == null || p2 == null) {
					Assert(false);
					continue;
				}

				if (p1.EntIndex() != -1 && p2.EntIndex() != -1) {
					if (p1.EntIndex() < p2.EntIndex())
						continue;
				}

				if (p2.EntIndex() == -1)
					continue;

				Predictables[i] = h2;
				Predictables[j] = h1;
			}
		}
	}

	internal void RemoveFromPredictablesList(ClientEntityHandle remove) {
		Assert(remove != null);

		Predictables.Remove(remove);
	}

	private readonly List<ClientEntityHandle> Predictables = [];
}

public partial class C_BaseEntity : IClientEntity
{
	public delegate void BASEPTR();
	public delegate void ENTITYFUNCPTR(C_BaseEntity? other);

	public const int SLOT_ORIGINALDATA = -1;
	public static C_BaseEntity? CreateEntityByName(ReadOnlySpan<char> className) {
		C_BaseEntity? ent = GetClassMap().CreateEntity(className);
		if (ent != null)
			return ent;

		Warning($"Can't find factory for entity: {className}\n");
		return null;
	}

	static readonly LinkedList<C_BaseEntity> InterpolationList = [];
	static readonly LinkedList<C_BaseEntity> TeleportList = [];
	static bool s_bInterpolate = true;
	static bool g_bWasSkipping;
	static bool g_bWasThreaded;
	static bool s_bAbsQueriesValid = true;
	static bool s_bAbsRecomputationEnabled = true;
	static ConVar cl_interpolate = new("cl_interpolate", "1", FCvar.UserInfo | FCvar.DevelopmentOnly);

	ClientThinkHandle_t thinkHandle;
	public ClientThinkHandle_t GetThinkHandle() => thinkHandle;
	public void SetThinkHandle(ClientThinkHandle_t handle) => thinkHandle = handle;

	static int PredictionRandomSeed = -1;
	static C_BasePlayer? PredictionPlayer;


	public virtual C_BaseAnimating? GetBaseAnimating() => null;

	public virtual bool IsWorld() => EntIndex() == 0;
	public virtual bool IsPlayer() => false;
	public virtual bool IsBaseCombatCharacter() => false;
	public virtual bool IsNPC() => false;
	public virtual bool IsNextBot() => false;
	public virtual bool IsBaseCombatWeapon() => false;
	public virtual bool IsCombatItem() => false;

	public ReadOnlySpan<char> GetClassname() => "not_yet_implemented";


	public virtual void PreEntityPacketReceived(int commandsAcknowledged) {
		throw new NotImplementedException();
	}

	public static void InterpolateServerEntities() {
		s_bInterpolate = cl_interpolate.GetBool();

		// Don't interpolate during timedemo playback
		if (engine.IsPlayingTimeDemo() || engine.IsPaused())
			s_bInterpolate = false;

		if (!engine.IsPlayingDemo()) {
			INetChannelInfo? nci = engine.GetNetChannelInfo();
			if (nci != null && nci.GetTimeSinceLastReceived() > 0.5)
				s_bInterpolate = false;
		}

		if (IsSimulatingOnAlternateTicks() != g_bWasSkipping || IsEngineThreaded() != g_bWasThreaded) {
			g_bWasSkipping = IsSimulatingOnAlternateTicks();
			g_bWasThreaded = IsEngineThreaded();

			C_BaseEntityIterator iterator = new();
			C_BaseEntity? ent;
			while ((ent = iterator.Next()) != null) {
				ent.Interp_UpdateInterpolationAmounts(ref ent.GetVarMapping());
			}
		}

		// Enable extrapolation?
		InterpolationContext.SetLastTimeStamp(engine.GetLastTimeStamp());
		if (cl_extrapolate.GetBool() && !engine.IsPaused())
			InterpolationContext.EnableExtrapolation(true);

		// Smoothly interpolate position for server entities.
		ProcessTeleportList();
		ProcessInterpolatedList();
	}

	public static void ProcessTeleportList() {
		LinkedListNode<C_BaseEntity>? entNode = TeleportList.First;
		while (entNode != null) {
			C_BaseEntity entity = entNode.Value;

			bool teleport = entity.Teleported();
			bool ef_nointerp = entity.IsNoInterpolationFrame();

			if (teleport || ef_nointerp) {
				entity.OldMoveParent.Set(entity.NetworkMoveParent);
				entity.OldParentAttachment = entity.ParentAttachment;
				entity.MoveToLastReceivedPosition(true);
				entity.ResetLatched();

				entNode = entNode.Next;
			}
			else {
				// Note: removing from the teleport list modifies the collection, so grab the next node now before removing
				var nextNode = entNode.Next;
				entity.RemoveFromTeleportList();
				entNode = nextNode;
			}
		}
	}

	public void ResetLatched() {
		if (IsClientCreated())
			return;

		Interp_Reset(ref GetVarMapping());
	}

	public static bool IsAbsQueriesValid() => !ThreadInMainThread() || s_bAbsQueriesValid;
	public static void SetAbsQueriesValid(bool valid) {
		if (!ThreadInMainThread())
			return;

		s_bAbsQueriesValid = !valid;
	}

	public static bool IsAbsRecomputationsEnabled() => !ThreadInMainThread() || s_bAbsRecomputationEnabled;
	public static void EnableAbsRecomputations(bool enable) {
		if (!ThreadInMainThread())
			return;

		s_bAbsRecomputationEnabled = enable;
	}

	void Clear() {
		Dormant = true;
		CreationTick = -1;
		ModelInstance = MODEL_INSTANCE_INVALID;
		renderHandle = INVALID_CLIENT_RENDER_HANDLE;
		thinkHandle = INVALID_THINK_HANDLE;
		Index = -1;
		SetLocalOrigin(vec3_origin);
		SetLocalAngles(vec3_angle);
		Model = null;
		AbsOrigin = default;
		AbsRotation = default;
		Velocity = default;
		ViewOffset = default;
		BaseVelocity = default;
		ModelIndex = default;
		AnimTime = default;
		SimulationTime = default;

		eflags = 0;
		RenderMode = 0;
		OldRenderMode = 0;
		RenderFX = 0;
		Friction = 0;

		UpdateVisibility();
	}

	static readonly List<C_BaseEntity> AimEntsList = [];
	public Action? FnThink;
	public virtual void Think(){
		if (FnThink != null)
			this.FnThink();
	}

	internal static void CalcAimEntPositions() {
		foreach (var ent in AimEntsList) {
			Assert(ent.GetMoveParent() != null);
			if (ent.IsEffectActive(EntityEffects.BoneMerge))
				ent.CalcAbsolutePosition();
		}
	}

	internal static void AddVisibleEntities() {
		// TODO
		// Requires leaf system
	}

	public bool IsNoInterpolationFrame() => OldInterpolationFrame != InterpolationFrame;

	public bool Teleported() => OldMoveParent != NetworkMoveParent || OldParentAttachment != ParentAttachment;

	public static void ProcessInterpolatedList() {
		LinkedListNode<C_BaseEntity>? curr = InterpolationList.First;
		LinkedListNode<C_BaseEntity>? next = curr?.Next;
		while (curr != null) {
			C_BaseEntity entity = curr.Value;
			entity.ReadyToDraw = entity.Interpolate(gpGlobals.CurTime);
			if (curr.List == null) // We got removed!!
				curr = next;

			curr = curr?.Next;
			next = curr?.Next;
		}
	}

	static ConVar cl_extrapolate = new("1", FCvar.Cheat, "Enable/disable extrapolation if interpolation history runs out.");
	static ConVar cl_interp_npcs = new("0.0", FCvar.UserInfo, "Interpolate NPC positions starting this many seconds in past (or cl_interp, if greater)");
	static ConVar cl_interp_all = new("0", 0, "Disable interpolation list optimizations.", 0, 0, cc_cl_interp_all_changed);

	private static void cc_cl_interp_all_changed(IConVar ivar, in ConVarChangeContext ctx) {
		ConVarRef var = new(ivar);
		if (var.GetInt() != 0) {
			C_BaseEntityIterator iterator = new();
			C_BaseEntity? ent;
			while ((ent = iterator.Next()) != null) {
				if (ent.ShouldInterpolate()) {
					ent.AddToInterpolationList();
				}
			}
		}
	}

	private static C_BaseEntity? FindPreviouslyCreatedEntity(PredictableId testId) {
		// TODO: Prediction system
		return null;
	}

	private static void RecvProxy_AnimTime(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		C_BaseEntity pEntity = (C_BaseEntity)instance;

		long t = gpGlobals.GetNetworkBase(gpGlobals.TickCount, pEntity.EntIndex()) + data.Value.Int;

		while (t < gpGlobals.TickCount - 127)
			t += 256;
		while (t > gpGlobals.TickCount + 127)
			t -= 256;

		pEntity.AnimTime = t * TICK_INTERVAL;
	}

	private static void RecvProxy_EffectFlags(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		// ((C_BaseEntity)instance).SetEffects(data.Value.Int);
	}


	public static RecvTable DT_AnimTimeMustBeFirst = new(nameof(DT_AnimTimeMustBeFirst), [
		RecvPropInt(FIELD.OF(nameof(AnimTime)), 0, RecvProxy_AnimTime),
	]);
	public static readonly ClientClass CC_AnimTimeMustBeFirst = new ClientClass("AnimTimeMustBeFirst", null, null, DT_AnimTimeMustBeFirst);


	public static RecvTable DT_PredictableId = new(nameof(DT_PredictableId), [
		RecvPropPredictableId(FIELD.OF(nameof(PredictableId))),
		RecvPropInt(FIELD.OF(nameof(b_IsPlayerSimulated))),
	]);
	public static readonly ClientClass CC_PredictableId = new ClientClass("PredictableId", null, null, DT_PredictableId);

	protected static void RecvProxy_SimulationTime(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		C_BaseEntity entity = (C_BaseEntity)instance;

		int addt = data.Value.Int;
		int tickbase = (int)gpGlobals.GetNetworkBase(gpGlobals.TickCount, entity.EntIndex());

		int t = tickbase + addt;

		while (t < gpGlobals.TickCount - 127)
			t += 256;
		while (t > gpGlobals.TickCount + 127)
			t -= 256;

		entity.SimulationTime = (t * TICK_INTERVAL);
	}

	public static RecvTable DT_BaseEntity = new([
		RecvPropDataTable("AnimTimeMustBeFirst", DT_AnimTimeMustBeFirst),

		RecvPropInt(FIELD.OF(nameof(SimulationTime)), 0, RecvProxy_SimulationTime),
		RecvPropInt(FIELD.OF(nameof(InterpolationFrame))),

		RecvPropVector(FIELD.OF_NAMED(nameof(NetworkOrigin), nameof(Origin))),
		RecvPropQAngles(FIELD.OF_NAMED(nameof(NetworkAngles), nameof(Rotation))),

		RecvPropInt(FIELD.OF(nameof(ModelIndex)), 0, RecvProxy_IntToModelIndex16_BackCompatible),

		RecvPropInt(FIELD.OF(nameof(Effects)), 0, RecvProxy_EffectFlags),
		RecvPropInt(FIELD.OF(nameof(RenderMode))),
		RecvPropInt(FIELD.OF(nameof(RenderFX))),
		RecvPropInt(FIELD.OF(nameof(ColorRender))),
		RecvPropInt(FIELD.OF(nameof(TeamNum))),
		RecvPropInt(FIELD.OF(nameof(CollisionGroup))),
		RecvPropFloat(FIELD.OF(nameof(Elasticity))),
		RecvPropFloat(FIELD.OF(nameof(ShadowCastDistance))),
		RecvPropEHandle(FIELD.OF(nameof(OwnerEntity))),
		RecvPropEHandle(FIELD.OF(nameof(EffectEntity))),
		RecvPropInt(FIELD.OF(nameof(MoveParent)), 0, RecvProxy_IntToMoveParent),
		RecvPropInt(FIELD.OF(nameof(ParentAttachment))),

		RecvPropInt(FIELD.OF(nameof(MoveType)), 0, RecvProxy_MoveType),
		RecvPropInt(FIELD.OF(nameof(MoveCollide)), 0, RecvProxy_MoveCollide),
		RecvPropQAngles (FIELD.OF(nameof(Rotation))),
		RecvPropInt( FIELD.OF(nameof( TextureFrameIndex) )),
		RecvPropDataTable( "predictable_id", DT_PredictableId ),
		RecvPropInt(FIELD.OF(nameof(SimulatedEveryTick))),
		RecvPropInt(FIELD.OF(nameof(AnimatedEveryTick))),
		RecvPropBool( FIELD.OF(nameof( AlternateSorting ))),

		RecvPropDataTable(nameof(Collision), FIELD.OF(nameof(Collision)), CollisionProperty.DT_CollisionProperty, 0, RECV_GET_OBJECT_AT_FIELD(FIELD.OF(nameof(Collision)))),

		// gmod specific
		RecvPropInt(FIELD.OF(nameof(TakeDamage))),
		RecvPropInt(FIELD.OF(nameof(RealClassName))),

		RecvPropInt(FIELD.OF(nameof(OverrideMaterial))),

		RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(OverrideSubMaterials), 0), PropFlags.Unsigned),
		RecvPropArray2(null, 32, "OverrideSubMaterials"),

		RecvPropInt(FIELD.OF(nameof(Health))),
		RecvPropInt(FIELD.OF(nameof(MaxHealth))),
		RecvPropInt(FIELD.OF(nameof(SpawnFlags))),
		RecvPropInt(FIELD.OF(nameof(GModFlags))),
		RecvPropBool(FIELD.OF(nameof(OnFire))),
		RecvPropFloat(FIELD.OF(nameof(CreationTime))),

		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 0)),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 1)),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(Velocity), 2)),

		// NW2 table
		RecvPropGModTable(FIELD.OF(nameof(GMOD_DataTable))),

		// Addon exposed data tables
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_bool)), RecvPropBool(FIELD.OF_ARRAYINDEX(nameof(GMOD_bool), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_float)), RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(GMOD_float), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_int)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(GMOD_int), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_Vector)), RecvPropVector(FIELD.OF_ARRAYINDEX(nameof(GMOD_Vector), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_QAngle)), RecvPropQAngles(FIELD.OF_ARRAYINDEX(nameof(GMOD_QAngle), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(GMOD_EHANDLE)), RecvPropEHandle(FIELD.OF_ARRAYINDEX(nameof(GMOD_EHANDLE), 0))),
		RecvPropString(FIELD.OF(nameof(GMOD_String0))),
		RecvPropString(FIELD.OF(nameof(GMOD_String1))),
		RecvPropString(FIELD.OF(nameof(GMOD_String2))),
		RecvPropString(FIELD.OF(nameof(GMOD_String3))),

		// Creation IDs
		RecvPropInt(FIELD.OF(nameof(CreationID))),
		RecvPropInt(FIELD.OF(nameof(MapCreatedID))),
	]);

	private static void RecvProxy_OverrideMaterial(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		// Warning("RecvProxy_OverrideMaterial not implemented yet\n");
	}

	private static void RecvProxy_MoveCollide(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		// Warning("RecvProxy_MoveCollide not implemented yet\n");
	}

	private static void RecvProxy_MoveType(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		// Warning("RecvProxy_MoveType not implemented yet\n");
	}

	public static readonly ClientClass ClientClass = new ClientClass("BaseEntity", null, null, DT_BaseEntity)
																		.WithManualClassID(StaticClassIndices.CBaseEntity);

	static readonly DynamicAccessor DA_Origin = FIELD.OF(nameof(Origin));
	static readonly DynamicAccessor DA_Rotation = FIELD.OF(nameof(Rotation));

	public C_BaseEntity() {
		AddVar(DA_Origin, IV_Origin, LatchFlags.LatchSimulationVar);
		AddVar(DA_Rotation, IV_Rotation, LatchFlags.LatchSimulationVar);

		DataChangeEventRef.Struct = unchecked((ulong)-1);
		EntClientFlags = 0;

		RenderFXBlend = 255;
		Predictable = false;

		SimulatedEveryTick = false;
		AnimatedEveryTick = false;

		ReadyToDraw = true;
		PredictionContext = null;
		Clear();
	}

	public virtual bool IsTwoPass() => modelinfo.IsTranslucentTwoPass(GetModel());

	public int Index;

	private Model? Model;
	ModelInstanceHandle_t ModelInstance;

	public TimeUnit_t AnimTime;
	public TimeUnit_t OldAnimTime;

	public TimeUnit_t SimulationTime;
	public TimeUnit_t OldSimulationTime;

	public TimeUnit_t CreateTime;

	public byte InterpolationFrame;
	public byte OldInterpolationFrame;
	public int ModelIndex;
	public byte OldParentAttachment;
	public byte ParentAttachment;

	public byte MoveType;
	public byte MoveCollide;
	public bool TextureFrameIndex;
	public bool SimulatedEveryTick;
	public bool AnimatedEveryTick;
	public bool AlternateSorting;

	public readonly PredictableId PredictableId = new();

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
	public float Friction;

	public CollisionProperty Collision = new();

	public int Effects;
	public byte RenderMode;
	public byte RenderFX;
	public byte RenderFXBlend;
	public Color ColorRender;
	public int CollisionGroup;
	public float Elasticity;
	public float ShadowCastDistance;
	public byte OldRenderMode;

	public InlineArray32<bool> GMOD_bool;
	public InlineArray32<float> GMOD_float;
	public InlineArray32<int> GMOD_int;
	public InlineArray32<Vector3> GMOD_Vector;
	public InlineArray32<QAngle> GMOD_QAngle;
	public InlineArrayNew32<EHANDLE> GMOD_EHANDLE = new();
	public InlineArray512<char> GMOD_String0;
	public InlineArray512<char> GMOD_String1;
	public InlineArray512<char> GMOD_String2;
	public InlineArray512<char> GMOD_String3;

	public readonly GModTable GMOD_DataTable = new();

	public int Speed;
	public int TeamNum;

	public readonly EHANDLE OwnerEntity = new();
	public readonly EHANDLE EffectEntity = new();
	public readonly EHANDLE GroundEntity = new();
	public readonly EHANDLE NetworkMoveParent = new();
	public readonly EHANDLE OldMoveParent = new();
	public int LifeState;
	public Vector3 BaseVelocity;
	public int NextThinkTick;
	public byte WaterLevel;

	public long CreationTick;

	public bool OldShouldDraw;

	public Vector3 AbsOrigin;
	public QAngle AbsRotation;
	public Vector3 ViewOffset;
	public Vector3 OldOrigin;
	public QAngle OldRotation;
	public Vector3 NetworkOrigin;
	public QAngle NetworkAngles;

	public EntClientFlags EntClientFlags;

	public Vector3 Origin;
	public readonly InterpolatedVar<Vector3> IV_Origin = new("Origin");
	public QAngle Rotation;
	public readonly InterpolatedVar<QAngle> IV_Rotation = new("Rotation");


	public readonly Handle<C_BasePlayer> PlayerSimulationOwner = new();
	public readonly ReusableBox<ulong> DataChangeEventRef = new();

	public static C_BaseEntity? Instance(BaseHandle handle) => cl_entitylist.GetBaseEntityFromHandle(handle);
	public static C_BaseEntity? Instance(int ent) => cl_entitylist.GetBaseEntity(ent);

	public bool FClassnameIs(C_BaseEntity? entity, ReadOnlySpan<char> classname) {
		if (entity == null)
			return false;

		return 0 == strcmp(entity.GetClassname(), classname);
	}

	public int GetFxBlend() => RenderFXBlend;
	public void GetColorModulation(Span<float> color) {
		color[0] = ColorRender.R / 255f;
		color[1] = ColorRender.G / 255f;
		color[2] = ColorRender.B / 255f;
	}
	public virtual void ClientThink() { }

	public bool ReadyToDraw;
	public virtual int DrawModel(StudioFlags flags) {
		if (!ReadyToDraw)
			return 0;
		int drawn = 0;
		if (Model == null)
			return drawn;

		switch (Model.Type) {
			case ModelType.Brush:
				drawn = DrawBrushModel((flags & StudioFlags.Transparency) != 0, flags, (flags & StudioFlags.TwoPass) != 0);
				break;
			case ModelType.Studio:
				Warning($"ERROR:  Can't draw studio model {modelinfo.GetModelName(Model)} because {GetClientClass().NetworkName ?? "unknown"} is not derived from C_BaseAnimating\n");
				break;
			case ModelType.Sprite:
				Warning("ERROR:  Sprite model's not supported any more except in legacy temp ents\n");
				break;
		}

		DrawBBoxVisualizations();

		return drawn;
	}

	public virtual bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, TimeUnit_t currentTime) {
		return true;
	}
	public virtual void SetupWeights(Matrix3x4 boneToWorldOut, Span<float> flexWeights, TimeUnit_t currentTime) {

	}
	public virtual void DoAnimationEvents() {

	}

	public CollisionProperty CollisionProp() => Collision;

	static readonly ConVar r_drawrenderboxes = new("r_drawrenderboxes", "0", FCvar.Cheat);

	public void DrawBBoxVisualizations() {
		if (r_drawrenderboxes.GetInt() != 0) {
			GetRenderBounds(out Vector3 vecRenderMins, out Vector3 vecRenderMaxs);
			debugoverlay.AddBoxOverlay(GetRenderOrigin(), vecRenderMins, vecRenderMaxs, GetRenderAngles(), 255, 0, 255, 0, 0.01f);
		}
	}

	private int DrawBrushModel(bool v1, StudioFlags flags, bool v2) {
		// todo
		return 1;
	}

	public ref readonly Vector3 GetLocalOrigin() => ref Origin;
	public ref readonly QAngle GetLocalAngles() => ref Rotation;
	public ref readonly Vector3 GetAbsOrigin() {
		CalcAbsolutePosition();
		return ref AbsOrigin;
	}
	public ref readonly QAngle GetAbsAngles() {
		CalcAbsolutePosition();
		return ref AbsRotation;
	}
	public ref readonly Vector3 GetViewOffset() => ref ViewOffset;

	public virtual ClientClass GetClientClass() => ClientClassRetriever.GetOrError(GetType());


	public IClientNetworkable GetClientNetworkable() => this;
	public Source.Common.IClientRenderable GetClientRenderable() => this;
	public IClientThinkable GetClientThinkable() => this;
	public IClientEntity GetIClientEntity() => this;
	public IClientUnknown GetIClientUnknown() => this;



	public Model? GetModel() => Model;
	public virtual void ValidateModelIndex() {
		SetModelByIndex(ModelIndex);
	}
	void SetModelPointer(Model? model) {
		if (model != Model) {
			DestroyModelInstance();
			Model = model;
			OnNewModel();

			UpdateVisibility();
		}
	}
	protected virtual StudioHdr? OnNewModel() {
		return null; // what the hell????????????????? 
	}
	void SetModelIndex(int index) {
		ModelIndex = index;
		Model? model = modelinfo.GetModel(ModelIndex);
		SetModelPointer(model);
	}
	public void SetModelByIndex(int modelIndex) {
		SetModelIndex(modelIndex);
	}
	public bool SetModel(ReadOnlySpan<char> modelName) {
		if (!modelName.IsEmpty) {
			int modelIndex = modelinfo.GetModelIndex(modelName);
			SetModelByIndex(modelIndex);
			return modelIndex != -1;
		}
		else {
			SetModelByIndex(-1);
			return false;
		}
	}

	public virtual ref readonly Vector3 GetRenderOrigin() => ref GetAbsOrigin();
	public virtual ref readonly QAngle GetRenderAngles() => ref GetAbsAngles();
	public void GetRenderBounds(out Vector3 mins, out Vector3 maxs) {
		ModelType nModelType = modelinfo.GetModelType(Model);
		if (nModelType == ModelType.Studio || nModelType == ModelType.Brush)
			modelinfo.GetModelRenderBounds(GetModel(), out mins, out maxs);
		else {
			// todo
			mins = maxs = default;
		}
	}

	public void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs) {
		throw new NotImplementedException();
	}

	public bool IsTransparent() {
		// todo; we need IModelInfoClient for this
		return false;
	}

	public void UpdateOnRemove() {
		// VPhysicsDestroyObject();

		// Assert(GetMoveParent() == null);
		// UnlinkFromHierarchy();
		// SetGroundEntity(NULL);
	}

	public readonly EHANDLE MoveParent = new();
	public readonly EHANDLE MoveChild = new();
	public readonly EHANDLE MovePeer = new();
	public readonly EHANDLE MovePrevPeer = new();

	public void UnlinkFromHierarchy() {
		// todo
	}

	public void Release() {
		using (C_BaseAnimating.AutoAllowBoneAccess boneaccess = new(true, true))
			UnlinkFromHierarchy();

		// if (IsIntermediateDataAllocated()) 
		// DestroyIntermediateData();

		UpdateOnRemove();
	}

	public bool OnPredictedEntityRemove(bool isbeingremoved, C_BaseEntity predicted) {
		PredictionContext? ctx = predicted.PredictionContext;
		Assert(ctx != null);
		if (ctx != null) {
			// Create backlink to actual entity
			ctx.ServerEntity.Set(this);
		}

		// If it comes through with an ID, it should be eligible
		SetPredictionEligible(true);

		// Start predicting simulation forward from here
		CheckInitPredictable("OnPredictedEntityRemove");

		// Always mark it dormant since we are the "real" entity now
		predicted.SetDormantPredictable(true);

		InvalidatePhysicsRecursive(InvalidatePhysicsBits.PositionChanged | InvalidatePhysicsBits.AnglesChanged | InvalidatePhysicsBits.VelocityChanged);

		// By default, signal that it should be deleted right away
		// If a derived class implements this method, it might chain to here but return
		// false if it wants to keep the dormant predictable around until the chain of
		//  DATA_UPDATE_CREATED messages passes
		return true;
	}

	public virtual bool ShouldPredict() => false;

	public int RestoreData(ReadOnlySpan<char> context, int slot, PredictionCopyType type) {
		// todo
		return 0;
	}

	public void AddToAimEntsList() {
		// todo
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetModelIndex() => ModelIndex;

	public void OnPostRestoreData() {
		InvalidatePhysicsRecursive(InvalidatePhysicsBits.PositionChanged | InvalidatePhysicsBits.AnglesChanged | InvalidatePhysicsBits.VelocityChanged);

		if (GetMoveParent() != null)
			AddToAimEntsList();

		if (GetModel() != modelinfo.GetModel(GetModelIndex()))
			SetModelByIndex(GetModelIndex());
	}

	private void CheckInitPredictable(ReadOnlySpan<char> context) {
		if (cl_predict.GetInt() == 0)
			return;

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();

		if (player == null)
			return;

		if (!GetPredictionEligible()) {
			if (PredictableId.IsActive() && (player.Index - 1) == PredictableId.GetPlayer())
				SetPredictionEligible(true);
			else
				return;

		}

		if (IsClientCreated())
			return;

		if (!ShouldPredict())
			return;

		if (IsIntermediateDataAllocated())
			return;

		// Msg( "Predicting init %s at %s\n", GetClassname(), context );

		InitPredictable();
	}

	bool PredictionEligible;
	public long SimulationTick;
	public bool GetPredictionEligible() => PredictionEligible;
	public void SetPredictionEligible(bool canpredict) => PredictionEligible = canpredict;


	public virtual bool ShouldDraw() {
		if ((RenderMode)RenderMode == Source.RenderMode.None)
			return false;

		return Model != null && !IsEffectActive(EntityEffects.NoDraw) && Index != 0;
	}

	public virtual bool Init(int entNum, int serialNum) {
		Index = entNum;
		cl_entitylist.AddNetworkableEntity(GetIClientUnknown(), entNum, serialNum);
		Interp_SetupMappings(ref GetVarMapping());
		CreationTick = gpGlobals.TickCount;

		return true;
	}

	public virtual EntityCapabilities ObjectCaps() => 0;

	public virtual void Dispose() {
		GC.SuppressFinalize(this);
	}

	double SpawnTime;
	double LastMessageTime;

	public void MoveToLastReceivedPosition(bool force = false) {
		if (force || (RenderFx)RenderFX != RenderFx.Ragdoll) {
			SetLocalOrigin(GetNetworkOrigin());
			SetLocalAngles(GetNetworkAngles());
		}
	}
	public virtual void PreDataUpdate(DataUpdateType updateType) {
		if (HLClient.AddDataChangeEvent(this, updateType, DataChangeEventRef))
			OnPreDataChanged(updateType);

		bool newentity = updateType == DataUpdateType.Created;

		if (!newentity)
			Interp_RestoreToLastNetworked(ref GetVarMapping());

		if (newentity && !IsClientCreated()) {
			SpawnTime = engine.GetLastTimeStamp();
			Spawn();
		}

		OldOrigin = GetNetworkOrigin();
		OldRotation = GetNetworkAngles();
		OldAnimTime = AnimTime;
		OldSimulationTime = SimulationTime;

		OldRenderMode = RenderMode;

		// TODO: client leaf sorting

		OldInterpolationFrame = InterpolationFrame;
		OldShouldDraw = ShouldDraw();
	}

	public virtual void PostDataUpdate(DataUpdateType updateType) {
		if ((RenderFx)RenderFX == RenderFx.Ragdoll && updateType == DataUpdateType.Created)
			MoveToLastReceivedPosition(true);
		else
			MoveToLastReceivedPosition(false);

		if (Index == 0) {
			ModelIndex = 1;
			// SetSolid(SolidType.BSP);
		}

		if (OldRenderMode != RenderMode)
			SetRenderMode((RenderMode)RenderMode, true);

		bool animTimeChanged = AnimTime != OldAnimTime;
		bool originChanged = OldOrigin != GetLocalOrigin();
		bool anglesChanged = OldRotation != GetLocalAngles();
		bool simTimeChanged = SimulationTime != OldSimulationTime;

		// Detect simulation changes 
		bool simulationChanged = originChanged || anglesChanged || simTimeChanged;

		bool predictable = GetPredictable();

		if (!predictable && !IsClientCreated()) {
			if (animTimeChanged)
				OnLatchInterpolatedVariables(LatchFlags.LatchAnimationVar);

			if (simulationChanged)
				OnLatchInterpolatedVariables(LatchFlags.LatchSimulationVar);

		}
		else if (predictable)
			OnStoreLastNetworkedValue();

		// HierarchySetParent(NetworkMoveParent);

		MarkMessageReceived();

		ValidateModelIndex();

		if (updateType == DataUpdateType.Created) {
			ProxyRandomValue = Random.Shared.NextSingle();
			ResetLatched();
			CreationTick = gpGlobals.TickCount;
		}

		// CheckInitPredictable("PostDataUpdate");
		// TODO: Some stuff involving localplayer and ownage
		// TODO: Partition/leaf stuff

		if (!IsClientCreated())
			if (Teleported() || IsNoInterpolationFrame())
				AddToTeleportList();

		if (OldMoveParent != NetworkMoveParent)
			UpdateVisibility();
		if (OldShouldDraw != ShouldDraw())
			UpdateVisibility();
	}



	public void InitPredictable() {
		Assert(!GetPredictable());
		// todo
	}

	public bool IsIntermediateDataAllocated() {
		return false; // todo
	}

	private void OnStoreLastNetworkedValue() {
		bool bRestore = false;
		Vector3 savePos = default;
		QAngle saveAng = default;

		if (RenderFX == (byte)RenderFx.Ragdoll && GetPredictable()) {
			bRestore = true;
			savePos = GetLocalOrigin();
			saveAng = GetLocalAngles();

			MoveToLastReceivedPosition(true);
		}

		int c = VarMap.Entries.Count;
		for (int i = 0; i < c; i++) {
			VarMapEntry e = VarMap.Entries[i];
			IInterpolatedVar watcher = e.Watcher;

			LatchFlags type = watcher.GetVarType();

			if ((type & LatchFlags.ExcludeAutoLatch) != 0)
				continue;

			watcher.NoteLastNetworkedValue();
		}

		if (bRestore) {
			SetLocalOrigin(savePos);
			SetLocalAngles(saveAng);
		}
	}

	float ProxyRandomValue;

	public virtual void SetRenderMode(RenderMode renderMode, bool forceUpdate) {
		RenderMode = (byte)renderMode;
	}

	protected void MarkMessageReceived() {
		LastMessageTime = engine.GetLastTimeStamp();
	}

	public void SetDormant(bool dormant) {
		Dormant = dormant;
		UpdateVisibility();
	}

	public BaseHandle? GetClientHandle() => RefEHandle;

	public bool InitializeAsClientEntity(ReadOnlySpan<char> modelName, RenderGroup renderGroup) {
		int modelIndex;

		if (!modelName.IsEmpty) {
			modelIndex = modelinfo.GetModelIndex(modelName);

			if (modelIndex == -1) {
				// Model could not be found
				AssertMsg(false, "Model could not be found, index is -1");
				return false;
			}
		}
		else
			modelIndex = -1;

		Interp_SetupMappings(ref GetVarMapping());
		return InitializeAsClientEntityByIndex(modelIndex, renderGroup);
	}

	public bool InitializeAsClientEntityByIndex(int index, RenderGroup renderGroup) {
		this.Index = -1;

		// Setup model data.
		SetModelByIndex(index);

		// Add the client entity to the master entity list.
		cl_entitylist.AddNonNetworkableEntity(GetIClientUnknown());
		Assert(GetClientHandle() != cl_entitylist.InvalidHandle());

		// Add the client entity to the renderable "leaf system." (Renderable)
		AddToLeafSystem(renderGroup);

		// Add the client entity to the spatial partition. (Collidable)
		// CollisionProp()->CreatePartitionHandle();

		SpawnClientEntity();

		return true;
	}

	public virtual void SpawnClientEntity() {

	}

	protected virtual void UpdateVisibility() {
		// todo: tools
		if (ShouldDraw() && !IsDormant())
			AddToLeafSystem();
		else
			RemoveFromLeafSystem();
	}

	ClientRenderHandle_t renderHandle;

	public ClientRenderHandle_t GetRenderHandle() => renderHandle;
	public ref ClientRenderHandle_t RenderHandle() => ref renderHandle;

	public virtual RenderGroup GetRenderGroup() {
		if (RenderMode == (int)Source.RenderMode.None)
			return RenderGroup.OpaqueEntity;

		// The rest of this can be implemented later
		return RenderGroup.OpaqueEntity;
	}

	public void AddToLeafSystem() => AddToLeafSystem(GetRenderGroup());
	public void AddToLeafSystem(RenderGroup group) {
		if (renderHandle == INVALID_CLIENT_RENDER_HANDLE) {
			clientLeafSystem.AddRenderable(this, group);
			clientLeafSystem.EnableAlternateSorting(renderHandle, AlternateSorting);
		}
		else {
			clientLeafSystem.SetRenderGroup(renderHandle, group);
			clientLeafSystem.RenderableChanged(renderHandle);
		}
	}

	private void RemoveFromLeafSystem() {
		if (renderHandle != INVALID_CLIENT_RENDER_HANDLE) {
			clientLeafSystem.RemoveRenderable(renderHandle);
			renderHandle = INVALID_CLIENT_RENDER_HANDLE;
		}
		// DestroyShadow();
	}


	public virtual void NotifyShouldTransmit(ShouldTransmiteState state) {
		if (EntIndex() < 0)
			return;

		switch (state) {
			case ShouldTransmiteState.Start: {
					SetDormant(false);

					if (PredictableId.IsActive()) {
						PredictableId.SetAcknowledged(true);

						C_BaseEntity? otherEntity = FindPreviouslyCreatedEntity(PredictableId);
						if (otherEntity != null) {
							Assert(otherEntity.IsClientCreated());
							Assert(otherEntity.PredictableId.IsActive());
							// We need IsHandleValid/GetClientHandle stuff.
							// Assert(cl_entitylist.IsHandleValid(otherEntity.GetClientHandle()));

							// otherEntity.PredictableId.SetAcknowledged(true);

							// if (OnPredictedEntityRemove(false, otherEntity)) 
							// otherEntity.Release();
						}
					}
				}
				break;

			case ShouldTransmiteState.End: {
					UnlinkFromHierarchy();
					SetDormant(true);
				}
				break;

			default:
				Assert(false);
				break;
		}
	}

	LinkedListNode<C_BaseEntity>? InterpolationListEntry;
	LinkedListNode<C_BaseEntity>? TeleportListEntry;

	public void AddToInterpolationList() {
		if (InterpolationListEntry == null)
			InterpolationListEntry = InterpolationList.AddLast(this);
	}

	public void RemoveFromInterpolationList() {
		if (InterpolationListEntry != null) {
			InterpolationList.Remove(InterpolationListEntry);
			InterpolationListEntry = null;
		}
	}

	public void AddToTeleportList() {
		if (TeleportListEntry == null)
			TeleportListEntry = TeleportList.AddLast(this);
	}

	public void RemoveFromTeleportList() {
		if (TeleportListEntry != null) {
			TeleportList.Remove(TeleportListEntry);
			TeleportListEntry = null;
		}
	}

	public virtual void OnPreDataChanged(DataUpdateType updateType) {
		OldMoveParent.Set(NetworkMoveParent);
		OldParentAttachment = ParentAttachment;
	}

	public virtual void OnDataChanged(DataUpdateType updateType) {
		CreateShadow();

		if (updateType == DataUpdateType.Created)
			UpdateVisibility();
	}

	public virtual void OnLatchInterpolatedVariables(LatchFlags flags) {
		TimeUnit_t changetime = GetLastChangeTime(flags);

		bool updateLastNetworkedValue = (flags & LatchFlags.InteroplateOmitUpdateLastNetworked) == 0;

		int c = VarMap.Entries.Count;
		for (int i = 0; i < c; i++) {
			VarMapEntry e = VarMap.Entries[i];
			IInterpolatedVar watcher = e.Watcher;

			LatchFlags type = watcher.GetVarType();

			if ((type & flags) == 0)
				continue;

			if ((type & LatchFlags.ExcludeAutoLatch) != 0)
				continue;

			if (watcher.NoteChanged(changetime, updateLastNetworkedValue))
				e.NeedsToInterpolate = true;
		}

		if (ShouldInterpolate())
			AddToInterpolationList();
	}

	private TimeUnit_t GetLastChangeTime(LatchFlags flags) {
		if (GetPredictable() || IsClientCreated())
			return gpGlobals.CurTime;

		Assert(!((flags & LatchFlags.LatchAnimationVar) != 0 && (flags & LatchFlags.LatchSimulationVar) != 0));

		if ((flags & LatchFlags.LatchAnimationVar) != 0)
			return GetAnimTime();

		if ((flags & LatchFlags.LatchSimulationVar) != 0) {
			TimeUnit_t st = GetSimulationTime();
			if (st == 0.0)
				return gpGlobals.CurTime;

			return st;
		}

		Assert(false);

		return gpGlobals.CurTime;
	}

	private void CreateShadow() {

	}

	public virtual void Spawn() { }
	public virtual void Precache() { }

	public readonly PredictableId PredictionId = new();
	public PredictionContext? PredictionContext;
	bool Dormant;
	bool Predictable;
	bool DormantPredictable;
	long IncomingPacketEntityBecameDormant;

	public bool GetPredictable() => Predictable;
	public void SetPredictable(bool state) {
		Predictable = state;
		Interp_UpdateInterpolationAmounts(ref GetVarMapping());
	}


	public bool BecameDormantThisPacket() {
		Assert(IsDormantPredictable());
		if (IncomingPacketEntityBecameDormant != prediction.GetIncomingPacketNumber())
			return false;
		return true;
	}

	public bool IsDormantPredictable() {
		return DormantPredictable;
	}

	public void SetDormantPredictable(bool dormant) {
		Assert(IsClientCreated());
		DormantPredictable = true;
		IncomingPacketEntityBecameDormant = prediction.GetIncomingPacketNumber();
	}

	public bool IsClientCreated() {
		if (PredictionContext != null) {
			Assert(!GetPredictable());
			return true;
		}
		return false;
	}

	public Matrix3x4 EntityToWorldTransform() {
		CalcAbsolutePosition();
		return CoordinateFrame;
	}

	public ref Vector3 GetNetworkOrigin() => ref NetworkOrigin;
	public ref QAngle GetNetworkAngles() => ref NetworkAngles;

	public void SetLocalOrigin(in Vector3 origin) {
		// This has a lot more logic thats needed later TODO FIXME
		Origin = origin;
	}

	public void SetLocalAngles(in QAngle angles) {
		// This has a lot more logic thats needed later TODO FIXME
		Rotation = angles;
	}

	public void SetNetworkAngles(in QAngle angles) {
		NetworkAngles = angles;
	}



	int flags;
	EFL eflags = EFL.DirtyAbsTransform; // << TODO: FIGURE OUT WHAT ACTUALLY INITIALIZES THIS.
	public Matrix3x4 CoordinateFrame;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsMarkedForDeletion() => (eflags & EFL.KillMe) != 0;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddEFlags(EFL flags) => eflags |= flags;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void RemoveEFlags(EFL flags) => eflags &= ~flags;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsEFlagSet(EFL mask) => (eflags & mask) != 0;


	readonly object CalcAbsolutePositionMutex = new();

	private void CalcAbsolutePosition() {
		if (!s_bAbsRecomputationEnabled)
			return;

		eflags |= EFL.DirtyAbsTransform;

		if ((eflags & EFL.DirtyAbsTransform) == 0)
			return;

		lock (CalcAbsolutePositionMutex) {
			if ((eflags & EFL.DirtyAbsTransform) == 0)
				return;

			RemoveEFlags(EFL.DirtyAbsTransform);

			if (!MoveParent.IsValid()) {
				MathLib.AngleMatrix(GetLocalAngles(), GetLocalOrigin(), out CoordinateFrame);
				AbsOrigin = GetLocalOrigin();
				AbsRotation = GetLocalAngles();
				MathLib.NormalizeAngles(ref AbsRotation);
				return;
			}

			if (IsEffectActive(EntityEffects.BoneMerge)) {
				MoveToAimEnt();
				return;
			}

			// todo
		}
	}

	public virtual IClientVehicle? GetClientVehicle() => null;

	public void SetRemovalFlag(bool remove) {
		if (remove)
			eflags |= EFL.KillMe;
		else
			eflags &= ~EFL.KillMe;
	}

	public readonly List<ThinkFunc> ThinkFunctions = [];
	public int CurrentThinkContext = NO_THINK_CONTEXT;
	public void SetNextClientThink(TimeUnit_t nextThinkTime) {
		Assert(GetClientHandle() != INVALID_CLIENTENTITY_HANDLE);
		ClientThinkList().SetNextClientThink(GetClientHandle()!, nextThinkTime);
	}

	public virtual void GetAimEntOrigin(C_BaseEntity attachedTo, out Vector3 origin, out QAngle angles) {
		origin = attachedTo.GetAbsOrigin();
		angles = attachedTo.GetAbsAngles();
	}

	public void SetAbsOrigin(in Vector3 absOrigin) {
		CalcAbsolutePosition();

		if (AbsOrigin == absOrigin)
			return;

		InvalidatePhysicsRecursive(InvalidatePhysicsBits.PositionChanged);
		RemoveEFlags(EFL.DirtyAbsTransform);

		AbsOrigin = absOrigin;
		// TODO: Coordinate frame...
		C_BaseEntity? moveParent = GetMoveParent();

		if (moveParent == null) {
			Origin = absOrigin;
			return;
		}

		// TODO: Handle move parents...
	}

	public void SetAbsAngles(in QAngle absAngles) {
		CalcAbsolutePosition();

		if (AbsRotation == absAngles)
			return;

		InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnglesChanged);
		RemoveEFlags(EFL.DirtyAbsTransform);

		AbsRotation = absAngles;
		// TODO: Set coordinate frame

		C_BaseEntity? moveParent = GetMoveParent();

		if (moveParent == null) {
			Rotation = absAngles;
			return;
		}

		// TODO: Handle move parents...
	}

	public void MoveToAimEnt() {
		GetAimEntOrigin(GetMoveParent(), out Vector3 aimEntOrigin, out QAngle aimEntAngles);
		SetAbsOrigin(aimEntOrigin);
		SetAbsAngles(aimEntAngles);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsDormant() => IsServerEntity() && Dormant;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsServerEntity() => Index != -1;

	public int EntIndex() {
		return Index;
	}

	public void ReceiveMessage(int classID, bf_read msg) {
		throw new NotImplementedException();
	}

	public void SetDestroyedOnRecreateEntities() {

	}

	public virtual ICollideable GetCollideable() => throw new NotImplementedException();
	public virtual BaseHandle GetRefEHandle() {
		return RefEHandle;
	}
	public virtual void SetRefEHandle(BaseHandle handle) {
		RefEHandle.Index = handle.Index;
	}


	private ref readonly Vector3 GetLocalVelocity() {
		return ref vec3_origin; // todo
	}


	public void OnDataUnchangedInPVS() {
		// HierarchySetParent(NetworkMoveParent);
		MarkMessageReceived();
	}

	public virtual IPVSNotify? GetPVSNotifyInterface() {
		return null;
	}

	public void ComputeFxBlend() {
		// todo
	}

	public virtual object GetDataTableBasePtr() {
		return this;
	}

	public readonly BaseHandle RefEHandle = new();

	static double AdjustInterpolationAmount(C_BaseEntity entity, double baseInterpolation) {
		// We don't have cl_interp_npcs yet so this isn't needed
		return baseInterpolation;
	}

	public double GetInterpolationAmount(LatchFlags flags) {
		int serverTickMultiple = 1;
		// TODO: IsSimulatingOnAlternateTicks

		if (GetPredictable() || IsClientCreated()) {
			return TICK_INTERVAL * serverTickMultiple;
		}

		bool playingDemo = false; // engine.IsPlayingDemo();
		bool playingMultiplayer = !playingDemo && (gpGlobals.MaxClients > 1);
		bool playingNonLocallyRecordedDemo = playingDemo; // && !engine.IsPlayingDemoALocallyRecordedDemo();
		if (playingMultiplayer || playingNonLocallyRecordedDemo) {
			return AdjustInterpolationAmount(this, TICKS_TO_TIME(TIME_TO_TICKS(CdllBoundedCVars.GetClientInterpAmount()) + serverTickMultiple));
		}

		// TODO: Re-evaluate this later
		//expandedServerTickMultiple += g_nThreadModeTicks;
		int expandedServerTickMultiple = 1;

		if (IsAnimatedEveryTick() && IsSimulatedEveryTick()) {
			return TICK_INTERVAL * expandedServerTickMultiple;
		}

		if ((flags & LatchFlags.LatchAnimationVar) != 0 && IsAnimatedEveryTick()) {
			return TICK_INTERVAL * expandedServerTickMultiple;
		}
		if ((flags & LatchFlags.LatchSimulationVar) != 0 && IsSimulatedEveryTick()) {
			return TICK_INTERVAL * expandedServerTickMultiple;
		}

		return AdjustInterpolationAmount(this, TICKS_TO_TIME(TIME_TO_TICKS(CdllBoundedCVars.GetClientInterpAmount()) + serverTickMultiple));
	}


	public void AddVar(DynamicAccessor accessor, IInterpolatedVar watcher, LatchFlags type, bool setup = false) {
		bool addIt = true;
		for (int i = 0; i < VarMap.Entries.Count; i++) {
			if (VarMap.Entries[i].Watcher == watcher) {
				if ((type & LatchFlags.ExcludeAutoInterpolate) != (watcher.GetVarType() & LatchFlags.ExcludeAutoInterpolate))
					RemoveVar(VarMap.Entries[i].Accessor, true);
				else
					addIt = false;

				break;
			}
		}

		if (addIt) {
			VarMapEntry map = new() {
				Accessor = accessor,
				Watcher = watcher,
				Type = type,
				NeedsToInterpolate = true
			};
			if ((type & LatchFlags.ExcludeAutoInterpolate) != 0) {
				VarMap.Entries.Add(map);
			}
			else {
				VarMap.Entries.Insert(0, map);
				++VarMap.InterpolatedEntries;
			}
		}

		if (setup) {
			watcher.Setup(this, accessor, type);
			watcher.SetInterpolationAmount(GetInterpolationAmount(watcher.GetVarType()));
		}
	}
	public void RemoveVar(DynamicAccessor accessor, bool assert = true) {
		for (int i = 0; i < VarMap.Entries.Count; i++) {
			if (VarMap.Entries[i].Accessor == accessor) {
				if ((VarMap.Entries[i].Type & LatchFlags.ExcludeAutoInterpolate) == 0)
					--VarMap.InterpolatedEntries;

				VarMap.Entries.RemoveAt(i);
				return;
			}
		}

		if (assert)
			AssertMsg(false, "RemoveVar");
	}
	public ref VarMapping GetVarMapping() => ref VarMap;
	public VarMapping VarMap = new();
	static double LastValue_Interp = -1;
	static double LastValue_InterpNPCs = -1;
	void CheckCLInterpChanged() {
		double curValue_Interp = CdllBoundedCVars.GetClientInterpAmount();
		if (LastValue_Interp == -1) LastValue_Interp = curValue_Interp;

		// float curValue_InterpNPCs = cl_interp_npcs.GetFloat();
		double curValue_InterpNPCs = 0;
		if (LastValue_InterpNPCs == -1) LastValue_InterpNPCs = curValue_InterpNPCs;

		if (LastValue_Interp != curValue_Interp || LastValue_InterpNPCs != curValue_InterpNPCs) {
			LastValue_Interp = curValue_Interp;
			LastValue_InterpNPCs = curValue_InterpNPCs;

			C_BaseEntityIterator iterator = new();
			C_BaseEntity? ent;
			while ((ent = iterator.Next()) != null)
				ent.Interp_UpdateInterpolationAmounts(ref ent.GetVarMapping());
		}
	}

	private void Interp_SetupMappings(ref VarMapping map) {
		if (Unsafe.IsNullRef(ref map))
			return;

		int c = map.Entries.Count();
		for (int i = 0; i < c; i++) {
			VarMapEntry e = map.Entries[i];
			IInterpolatedVar watcher = e.Watcher;
			DynamicAccessor accessor = e.Accessor;
			LatchFlags type = e.Type;

			watcher.Setup(this, accessor, type);
			watcher.SetInterpolationAmount(GetInterpolationAmount(watcher.GetVarType()));
		}
	}
	private int Interp_Interpolate(ref VarMapping map, double currentTime) {
		int noMoreChanges = 1;
		if (currentTime < map.LastInterpolationTime) {
			for (int i = 0; i < map.InterpolatedEntries; i++) {
				VarMapEntry e = map.Entries[i];

				e.NeedsToInterpolate = true;
			}
		}
		map.LastInterpolationTime = currentTime;

		for (int i = 0; i < map.InterpolatedEntries; i++) {
			VarMapEntry e = map.Entries[i];

			if (!e.NeedsToInterpolate)
				continue;

			IInterpolatedVar watcher = e.Watcher;
			Assert((watcher.GetVarType() & LatchFlags.ExcludeAutoInterpolate) == 0);

			if (watcher.Interpolate(currentTime) != 0)
				e.NeedsToInterpolate = false;
			else
				noMoreChanges = 0;
		}

		return noMoreChanges;
	}
	private void Interp_RestoreToLastNetworked(ref VarMapping map) {
		Vector3 oldOrigin = GetLocalOrigin();
		QAngle oldAngles = GetLocalAngles();
		Vector3 oldVel = GetLocalVelocity();

		int c = map.Entries.Count();
		for (int i = 0; i < c; i++) {
			VarMapEntry e = map.Entries[i];
			IInterpolatedVar watcher = e.Watcher;
			watcher.RestoreToLastNetworked();
		}

		BaseInterpolatePart2(oldOrigin, oldAngles, oldVel, 0);
	}

	public virtual bool Interpolate(TimeUnit_t currentTime) {
		Vector3 oldOrigin = default;
		QAngle oldAngles = default;
		Vector3 oldVel = default;

		int noMoreChanges = 0;
		InterpolateResult retVal = BaseInterpolatePart1(ref currentTime, ref oldOrigin, ref oldAngles, ref oldVel, ref noMoreChanges);

		if (noMoreChanges != 0)
			RemoveFromInterpolationList();

		if (retVal == InterpolateResult.Stop)
			return true;

		InvalidatePhysicsBits changeFlags = 0;
		BaseInterpolatePart2(oldOrigin, oldAngles, oldVel, changeFlags);

		return true;
	}



	protected InterpolateResult BaseInterpolatePart1(ref TimeUnit_t currentTime, ref Vector3 oldOrigin, ref QAngle oldAngles, ref Vector3 oldVel, ref int noMoreChanges) {
		noMoreChanges = 1;

		if (IsFollowingEntity() || !IsInterpolationEnabled()) {
			MoveToLastReceivedPosition();
			return InterpolateResult.Stop;
		}


		if (GetPredictable() || IsClientCreated()) {
			C_BasePlayer? localplayer = C_BasePlayer.GetLocalPlayer();
			if (localplayer != null && currentTime == gpGlobals.CurTime) {
				currentTime = localplayer.GetFinalPredictedTime();
				currentTime -= TICK_INTERVAL;
				currentTime += (gpGlobals.InterpolationAmount * TICK_INTERVAL);
			}
		}

		oldOrigin = Origin;
		oldAngles = Rotation;
		oldVel = Velocity;

		noMoreChanges = Interp_Interpolate(ref GetVarMapping(), currentTime);
		if (cl_interp_all.GetInt() != 0 || (EntClientFlags & EntClientFlags.AlwaysInterpolate) != 0)
			noMoreChanges = 0;

		return InterpolateResult.Continue;
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


	private static bool IsInterpolationEnabled() => s_bInterpolate;
	public C_BaseEntity? GetMoveParent() => MoveParent.Get();
	public C_BaseEntity? FirstMoveChild() => MoveChild.Get();
	public C_BaseEntity? NextMovePeer() => MovePeer.Get();
	public bool IsVisible() => renderHandle != INVALID_CLIENT_RENDER_HANDLE;


	public bool IsFollowingEntity() => IsEffectActive(EntityEffects.BoneMerge) && (GetMoveType() != Source.MoveType.None && GetMoveParent() != null);

	public virtual C_BaseEntity? GetFollowedEntity() {
		if (!IsFollowingEntity())
			return null;
		return GetMoveParent();
	}

	public virtual ModelInstanceHandle_t GetModelInstance() => ModelInstance;
	public virtual void SetModelInstance(ModelInstanceHandle_t modelInstance) => ModelInstance = modelInstance;
	public void CreateModelInstance() {
		if (ModelInstance == MODEL_INSTANCE_INVALID)
			ModelInstance = modelrender.CreateInstance(this);
	}
	public void DestroyModelInstance() {
		if (ModelInstance != MODEL_INSTANCE_INVALID) {
			modelrender.DestroyInstance(ModelInstance);
			ModelInstance = MODEL_INSTANCE_INVALID;
		}
	}

	public virtual bool GetAttachment(int number, out Vector3 origin, out QAngle angles) {
		origin = GetAbsOrigin();
		angles = GetAbsAngles();
		return true;
	}

	Vector3 AbsVelocity;

	// todo
	public ref readonly Vector3 GetAbsVelocity() {
		return ref AbsVelocity;
	}
	public void SetAbsVelocity(in Vector3 absVelocity) {
		if (AbsVelocity == absVelocity)
			return;

		// The abs velocity won't be dirty since we're setting it here
		InvalidatePhysicsRecursive(InvalidatePhysicsBits.VelocityChanged);
		eflags &= ~EFL.DirtyAbsVelocity;

		AbsVelocity = absVelocity;

		C_BaseEntity? moveParent = GetMoveParent();

		if (moveParent == null) {
			Velocity = absVelocity;
			return;
		}

		// First subtract out the parent's abs velocity to get a relative
		// velocity measured in world space
		Vector3 relVelocity;
		MathLib.VectorSubtract(absVelocity, moveParent.GetAbsVelocity(), out relVelocity);

		// Transform velocity into parent space
		MathLib.VectorIRotate(relVelocity, moveParent.EntityToWorldTransform(), out Velocity);
	}

	public ref readonly Vector3 GetBaseVelocity() => ref BaseVelocity;
	public void SetBaseVelocity(in Vector3 v) => BaseVelocity = v;

	public virtual bool GetAttachment(int number, out Vector3 origin) {
		origin = GetAbsOrigin();
		return true;
	}

	public virtual bool GetAttachment(int number, out Matrix3x4 matrix) {
		matrix = EntityToWorldTransform();
		return true;
	}

	public virtual bool GetAttachmentVelocity(int number, out Vector3 originVel, out Quaternion angleVel) {
		originVel = GetAbsVelocity();
		angleVel = default;
		angleVel.Init();
		return true;
	}


	protected bool ShouldInterpolate() {
		if (render.GetViewEntity() == Index)
			return true;

		if (Index == 0 || GetModel() == null)
			return false;

		if (IsVisible())
			return true;

		C_BaseEntity? child = FirstMoveChild();
		while (child != null) {
			if (child.ShouldInterpolate())
				return true;

			child = child.NextMovePeer();
		}

		return false;
	}

	public void Interp_Reset(ref VarMapping map) {
		int c = map.Entries.Count;
		for (int i = 0; i < c; i++) {
			VarMapEntry e = map.Entries[i];
			IInterpolatedVar watcher = e.Watcher;
			watcher.Reset();
		}
	}

	protected void BaseInterpolatePart2(Vector3 oldOrigin, QAngle oldAngles, Vector3 oldVel, InvalidatePhysicsBits changeFlags) {
		if (Origin != oldOrigin)
			changeFlags |= InvalidatePhysicsBits.PositionChanged;
		if (Rotation != oldAngles)
			changeFlags |= InvalidatePhysicsBits.AnglesChanged;
		if (Velocity != oldVel)
			changeFlags |= InvalidatePhysicsBits.VelocityChanged;

		if (changeFlags != 0)
			InvalidatePhysicsRecursive(changeFlags);
	}

	private void Interp_UpdateInterpolationAmounts(ref VarMapping map) {
		if (Unsafe.IsNullRef(ref map))
			return;

		int c = map.Entries.Count;
		for (int i = 0; i < c; i++) {
			VarMapEntry e = map.Entries[i];
			IInterpolatedVar watcher = e.Watcher;
			watcher.SetInterpolationAmount(GetInterpolationAmount(watcher.GetVarType()));
		}
	}
	private void Interp_HierarchyUpdateInterpolationAmounts() {

	}


	public void ShiftIntermediateDataForward(int slots_to_remove, int number_of_commands_run) {
		// todo
	}

	public bool GetCheckUntouch() => IsEFlagSet(EFL.CheckUntouch);
}

public class VarMapEntry
{
	public required LatchFlags Type;
	public required bool NeedsToInterpolate;
	public required DynamicAccessor Accessor;
	public required IInterpolatedVar Watcher;
}

public struct VarMapping
{
	public int InterpolatedEntries;
	public TimeUnit_t LastInterpolationTime;
	public List<VarMapEntry> Entries = [];
	public VarMapping() {
		InterpolatedEntries = 0;
	}
}
