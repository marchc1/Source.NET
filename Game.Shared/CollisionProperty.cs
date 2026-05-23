#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
using Game.Client;
#endif

#if GAME_DLL
using Game.Server;
#endif

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Engine;

using System.Numerics;
using System.Security.AccessControl;
using System.Reflection.Metadata.Ecma335;

using DEFINE = Source.DEFINE<Game.Shared.CollisionProperty>;
using FIELD = Source.FIELD<Game.Shared.CollisionProperty>;

namespace Game.Shared;

public enum SurroundingBoundsType
{
	UseOBBCollisionBounds = 0,
	UseBestCollisionBounds,
	UseHitboxes,
	UseSpecifiedBounds,
	UseGameCode,
	UseRotationExpandedBounds,
	UseCollisionBoundsNeverVPhysics,

	BitCount = 3
}

public class DirtySpatialPartitionEntityList() : AutoGameSystem("DirtySpatialPartitionEntityList"), IPartitionQueryCallback
{
	public static readonly DirtySpatialPartitionEntityList s_DirtyKDTree = new();

	public static void UpdateDirtySpatialPartitionEntities() {
		SpatialPartitionListMask_t listMask = (int)
#if CLIENT_DLL
		PartitionListMask.ClientGameEdicts;
#else
		PartitionListMask.ServerGameEdicts;
#endif

		s_DirtyKDTree.OnPreQuery(listMask);
		s_DirtyKDTree.OnPostQuery(listMask);
	}

	public override bool Init() {
		partition.InstallQueryCallback(this);
		return true;
	}
	public override void Shutdown() {
		partition.RemoveQueryCallback(this);
	}
	public override void LevelShutdownPostEntity() {
		DirtyEntities.Clear();
	}
	public virtual void OnPreQuery_V1() {
		Assert(false);
	}
	public virtual void OnPreQuery(SpatialPartitionListMask_t listMask) {
#if CLIENT_DLL
		const int validMask = (int)PartitionListMask.ClientGameEdicts;
#else
		const int validMask = (int)PartitionListMask.ServerGameEdicts;
#endif

		if ((listMask & validMask) == 0)
			return;

		if (PartitionWriteId != 0 && PartitionWriteId == ThreadGetCurrentId())
			return;

#if CLIENT_DLL
		// FIXME: This should really be an assertion... feh!
		if (!C_BaseEntity.IsAbsRecomputationsEnabled()) {
			LockPartitionForRead();
			return;
		}
#endif

		// if you're holding a read lock, then these are entities that were still dirty after your trace started
		// or became dirty due to some other thread or callback. Updating them may cause corruption further up the
		// stack (e.g. partition iterator).  Ignoring the state change should be safe since it happened after the 
		// trace was requested or was unable to be resolved in a previous attempt (still dirty).
		if (DirtyEntities.Count != 0 && ReadLockCount.Value == 0) {
			List<BaseHandle> vecStillDirty = ListPool<BaseHandle>.Shared.Alloc();
			PartitionMutex.AcquireWriterLock(100000);
			PartitionWriteId = (uint)ThreadGetCurrentId();
			while (DirtyEntities.TryDequeue(out BaseHandle handle)) {
#if !CLIENT_DLL
				BaseEntity? entity = gEntList.GetBaseEntity(handle);
#else
				BaseEntity? entity = cl_entitylist.GetBaseEntityFromHandle(handle);
#endif

				if (entity != null) {
					// If an entity is in the middle of bone setup, don't call UpdatePartition
					//  which can cause it to redo bone setup on the same frame causing a recursive
					//  call to bone setup.
					if (!entity.IsEFlagSet(EFL.SettingUpBones))
						entity.CollisionProp().UpdatePartition();
					else
						vecStillDirty.Add(handle);
				}
			}
			if (vecStillDirty.Count() > 0)
				for (int i = 0; i < vecStillDirty.Count(); i++)
					DirtyEntities.Enqueue(vecStillDirty[i]);


			PartitionWriteId = 0;
			PartitionMutex.ReleaseWriterLock();

			ListPool<BaseHandle>.Shared.Free(vecStillDirty);
		}
		LockPartitionForRead();
	}
	public virtual void OnPostQuery(SpatialPartitionListMask_t listMask) {
#if CLIENT_DLL
		if ((listMask & (int)PartitionListMask.ClientGameEdicts) == 0)
			return;
#elif GAME_DLL
		if ((listMask & (int)PartitionListMask.ServerGameEdicts) == 0)
			return;
#endif

		if (PartitionWriteId != 0)
			return;

		UnlockPartitionForRead();
	}
	public void AddEntity(BaseEntity entity) {
		DirtyEntities.Enqueue(entity.GetRefEHandle());
	}

	public void LockPartitionForRead() {
		if (ReadLockCount.Value == 0)
			PartitionMutex.AcquireReaderLock(100000);
		++ReadLockCount.Value;
	}

	public void UnlockPartitionForRead() {
		--ReadLockCount.Value;
		if (ReadLockCount.Value == 0)
			PartitionMutex.ReleaseReaderLock();
	}

	readonly Queue<BaseHandle> DirtyEntities = [];
	readonly ReaderWriterLock PartitionMutex = new();
	uint PartitionWriteId;
	readonly ThreadLocal<int> ReadLockCount = new();
}

public class CollisionProperty : ICollideable
{
#if CLIENT_DLL
	public static readonly DataMap PredMap = new(typeof(CollisionProperty), [
		DEFINE.PRED_FIELD(nameof(MinsPreScaled), FieldType.Vector, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(MaxsPreScaled), FieldType.Vector, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(Mins), FieldType.Vector, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(Maxs), FieldType.Vector, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(SolidType), FieldType.Integer, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(SolidFlags), FieldType.Short, FieldTypeDescFlags.InSendTable ),
		DEFINE.PRED_FIELD(nameof(TriggerBloat), FieldType.Integer, FieldTypeDescFlags.InSendTable ),
	]);
	private static void RecvProxy_VectorDirtySurround(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		Vector3 vecold = field.GetValue<Vector3>(instance);
		Vector3 vecnew = data.Value.Vector;
		if (vecold != vecnew) {
			field.SetValue(instance, in vecnew);
			((CollisionProperty)instance)!.MarkSurroundingBoundsDirty();
		}
	}


	private static void RecvProxy_SolidFlags(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		field.SetValue(instance, data.Value.Int);
	}

	private static void RecvProxy_Solid(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		field.SetValue(instance, data.Value.Int);
	}

	private static void RecvProxy_OBBMinsPreScaled(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		CollisionProperty prop = (CollisionProperty)instance;
		Vector3 vecMins = data.Value.Vector;
		// prop.SetCollisionBounds(vecMins, prop.OBBMaxsPreScaled());
	}

	private static void RecvProxy_OBBMaxPreScaled(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		CollisionProperty prop = (CollisionProperty)instance;
		Vector3 vecMaxs = data.Value.Vector;
		// prop.SetCollisionBounds(prop.OBBMinsPreScaled(), vecMaxs);
	}

	private static void RecvProxy_IntDirtySurround(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		if (field.GetValue<byte>(instance) != (byte)data.Value.Int) {
			field.SetValue<int>(instance, data.Value.Int);
			((CollisionProperty)instance).MarkSurroundingBoundsDirty();
		}
	}
#else
	private static void SendProxy_SolidFlags(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		outData.Int = ((CollisionProperty)(instance)).SolidFlags;
	}

	private static void SendProxy_Solid(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		outData.Int = ((CollisionProperty)(instance)).SolidType;
	}
#endif

	public Vector3 MinsPreScaled;
	public Vector3 MaxsPreScaled;
	public Vector3 Mins;
	public Vector3 Maxs;
	float Radius;
	public ushort SolidFlags;
	SpatialPartitionHandle_t Partition;
	byte SurroundType;
	public byte SolidType;

	public byte TriggerBloat;
	Vector3 SurroundingMins;
	Vector3 SurroundingMaxs;
	Vector3 SpecifiedSurroundingMinsPreScaled;
	Vector3 SpecifiedSurroundingMaxsPreScaled;
	Vector3 SpecifiedSurroundingMins;
	Vector3 SpecifiedSurroundingMaxs;

	public void UseTriggerBounds(bool enable, float bloat) {
		TriggerBloat = (byte)bloat;
		// todo
	}
	public void SetSolid(SolidType val) {
		// todo
	}


	private void MarkSurroundingBoundsDirty() {

	}

	public IHandleEntity? GetEntityHandle() => Outer;

	public ref readonly Vector3 OBBMinsPreScaled() {
		throw new NotImplementedException();
	}

	public ref readonly Vector3 OBBMaxsPreScaled() {
		throw new NotImplementedException();
	}

	public ref readonly Vector3 OBBMins() {
		throw new NotImplementedException();
	}

	public ref readonly Vector3 OBBMaxs() {
		throw new NotImplementedException();
	}

	public void WorldSpaceTriggerBounds(out Vector3 vecWorldMins, out Vector3 vecWorldMaxs) {
		throw new NotImplementedException();
	}

	public bool TestCollision(in Ray ray, Contents contentsMask, ref Trace tr) {
		throw new NotImplementedException();
	}

	public bool TestHitboxes(in Ray ray, Contents contentsMask, ref Trace tr) {
		throw new NotImplementedException();
	}

	public int GetCollisionModelIndex() {
		throw new NotImplementedException();
	}

	public Model? GetCollisionModel() {
		throw new NotImplementedException();
	}

	public ref readonly Vector3 GetCollisionOrigin() {
		throw new NotImplementedException();
	}

	public ref readonly QAngle GetCollisionAngles() {
		throw new NotImplementedException();
	}

	public ref readonly Matrix3x4 CollisionToWorldTransform() {
		throw new NotImplementedException();
	}

	public SolidType GetSolid() {
		throw new NotImplementedException();
	}

	public int GetSolidFlags() {
		throw new NotImplementedException();
	}

	public IClientUnknown? GetIClientUnknown() {
		throw new NotImplementedException();
	}

	public int GetCollisionGroup() {
		throw new NotImplementedException();
	}

	public void WorldSpaceSurroundingBounds(out Vector3 vecMins, out Vector3 vecMaxs) {
		throw new NotImplementedException();
	}

	public bool ShouldTouchTrigger(int triggerSolidFlags) {
		throw new NotImplementedException();
	}

	public ref readonly Matrix3x4 GetRootParentToWorldTransform() {
		throw new NotImplementedException();
	}

	internal void SetSolidFlags(SolidFlags flags) => SolidFlags = (ushort)flags;

	internal bool IsSolidFlagSet(SolidFlags flagMask) => (SolidFlags & (ushort)flagMask) != 0;

	internal void RemoveSolidFlags(SolidFlags flags) {
		throw new NotImplementedException();
	}

	internal bool IsSolid() => Constants.IsSolid((SolidType)SolidType, SolidFlags);

	internal void AddSolidFlags(SolidFlags flags) => SetSolidFlags((SolidFlags)SolidFlags | flags);
	internal void ClearSolidFlags() => SetSolidFlags(0);


	public void CreatePartitionHandle() {
		Assert(Partition == PARTITION_INVALID_HANDLE);
		Partition = partition.CreateHandle(GetEntityHandle());
	}
	public void DestroyPartitionHandle() {
		if (Partition != PARTITION_INVALID_HANDLE) {
			partition.DestroyHandle(Partition);
			Partition = PARTITION_INVALID_HANDLE;
		}
	}
	public ushort GetPartitionHandle() => Partition;
	public void MarkPartitionHandleDirty() {

	}
	public void UpdateServerPartitionMask() {
#if !CLIENT_DLL
		SpatialPartitionHandle_t handle = GetPartitionHandle();
		if (handle == PARTITION_INVALID_HANDLE)
			return;

		// Remove it from whatever lists it may be in at the moment
		// We'll re-add it below if we need to.
		partition.Remove(handle);

		// Don't bother with deleted things
		if (Outer.Edict() == null)
			return;

		// don't add the world
		if (Outer.EntIndex() == 0)
			return;

		// Make sure it's in the list of all entities
		bool bIsSolid = IsSolid() || IsSolidFlagSet(Source.SolidFlags.Trigger);
		if (bIsSolid || Outer.IsEFlagSet(EFL.UsePartitionWhenNotSolid)) 
			partition.Insert(PartitionListMask.EngineNonStaticEdicts, handle);
		
		if (!bIsSolid)
			return;

		// Insert it into the appropriate lists.
		// We have to continually reinsert it because its solid type may have changed
		PartitionListMask mask = 0;
		if (!IsSolidFlagSet(Source.SolidFlags.NotSolid))
			mask |= PartitionListMask.EngineSolidEdicts;

		if (IsSolidFlagSet(Source.SolidFlags.Trigger))
			mask |= PartitionListMask.EngineTriggerEdicts;

		Assert(mask != 0);
		partition.Insert(mask, handle);
#endif
	}
	public float BoundingRadius() => Radius;
	internal void UpdatePartition() {
		if (Outer.IsEFlagSet(EFL.DirtySpatialPartition)) {
			Outer.RemoveEFlags(EFL.DirtySpatialPartition);

#if !CLIENT_DLL
			Assert(Outer.EntIndex() != 0);

			// Don't bother with deleted things
			if (Outer.Edict() == null)
				return;

			if (GetPartitionHandle() == PARTITION_INVALID_HANDLE) {
				CreatePartitionHandle();
				UpdateServerPartitionMask();
			}
#else
			if (GetPartitionHandle() == PARTITION_INVALID_HANDLE)
				return;
#endif

			// We don't need to bother if it's not a trigger or solid
			if (IsSolid() || IsSolidFlagSet(Source.SolidFlags.Trigger) || Outer.IsEFlagSet(EFL.UsePartitionWhenNotSolid)) {
				// Bloat a little bit...
				if (BoundingRadius() != 0.0f) {
					WorldSpaceSurroundingBounds(out Vector3 vecSurroundMins, out Vector3 vecSurroundMaxs);
					vecSurroundMins -= new Vector3(1, 1, 1);
					vecSurroundMaxs += new Vector3(1, 1, 1);
					partition.ElementMoved(GetPartitionHandle(), vecSurroundMins, vecSurroundMaxs);
				}
				else {
					partition.ElementMoved(GetPartitionHandle(), GetCollisionOrigin(), GetCollisionOrigin());
				}
			}
		}
	}

	public void Init(BaseEntity entity) {
		Outer = entity;

		MinsPreScaled.Init();
		MaxsPreScaled.Init();
		Mins.Init();
		Maxs.Init();
		Radius = 0.0f;
		TriggerBloat = 0;
		SolidFlags = 0;
		SolidType = (int)Source.SolidType.None;

		SurroundType = (int)SurroundingBoundsType.UseOBBCollisionBounds;
		SurroundingMins = vec3_origin;
		SurroundingMaxs = vec3_origin;
		SpecifiedSurroundingMinsPreScaled.Init();
		SpecifiedSurroundingMaxsPreScaled.Init();
		SpecifiedSurroundingMins.Init();
		SpecifiedSurroundingMaxs.Init();
	}

	BaseEntity Outer = null!;
	public BaseEntity GetOuter() => Outer;

#if CLIENT_DLL
	public static RecvTable DT_CollisionProperty = new([
		RecvPropVector(FIELD.OF(nameof(MinsPreScaled)), 0, RecvProxy_OBBMinsPreScaled),
		RecvPropVector(FIELD.OF(nameof(MaxsPreScaled)), 0, RecvProxy_OBBMaxPreScaled),
		RecvPropVector(FIELD.OF(nameof(Mins)), 0),
		RecvPropVector(FIELD.OF(nameof(Maxs)), 0),
		RecvPropInt(FIELD.OF(nameof(SolidType)), 0, RecvProxy_Solid),
		RecvPropInt(FIELD.OF(nameof(SolidFlags)), 0, RecvProxy_SolidFlags),
		RecvPropInt(FIELD.OF(nameof(SurroundType)), 0, RecvProxy_IntDirtySurround),
		RecvPropInt(FIELD.OF(nameof(TriggerBloat)), 0, RecvProxy_IntDirtySurround),
		RecvPropVector(FIELD.OF(nameof(SpecifiedSurroundingMinsPreScaled)), 0, RecvProxy_VectorDirtySurround),
		RecvPropVector(FIELD.OF(nameof(SpecifiedSurroundingMaxsPreScaled)), 0, RecvProxy_VectorDirtySurround),
		RecvPropVector(FIELD.OF(nameof(SpecifiedSurroundingMins)), 0, RecvProxy_VectorDirtySurround),
		RecvPropVector(FIELD.OF(nameof(SpecifiedSurroundingMaxs)), 0, RecvProxy_VectorDirtySurround),
	]);

	public static readonly ClientClass CC_CollisionProperty = new("CollisionProperty", null, null, DT_CollisionProperty);
#else
	public static SendTable DT_CollisionProperty = new([
		SendPropVector(FIELD.OF(nameof(MinsPreScaled)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(MaxsPreScaled)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Mins)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Maxs)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(SolidType)), 3, PropFlags.Unsigned, SendProxy_Solid),
		SendPropInt(FIELD.OF(nameof(SolidFlags)), (int)Source.SolidFlags.MaxBits, PropFlags.Unsigned, SendProxy_SolidFlags),
		SendPropInt(FIELD.OF(nameof(SurroundType)), (int)SurroundingBoundsType.BitCount, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(TriggerBloat)), 0, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(SpecifiedSurroundingMinsPreScaled)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(SpecifiedSurroundingMaxsPreScaled)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(SpecifiedSurroundingMins)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(SpecifiedSurroundingMaxs)), 0, PropFlags.NoScale),
	]);


	public static readonly ServerClass CC_CollisionProperty = new("CollisionProperty", DT_CollisionProperty);
#endif
}
#endif
