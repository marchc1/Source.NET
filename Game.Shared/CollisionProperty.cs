#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
using Game.Client;

using Source;

#endif

#if GAME_DLL
using Game.Server;

using Source;

#endif

using Source.Common;

using System.Numerics;

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

public class CollisionProperty
{
#if CLIENT_DLL

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
		throw new NotImplementedException();
	}

	private static void SendProxy_Solid(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		throw new NotImplementedException();
	}
#endif

	Vector3 MinsPreScaled;
	Vector3 MaxsPreScaled;
	Vector3 Mins;
	Vector3 Maxs;
	float Radius;
	ushort SolidFlags;
	SpatialPartitionHandle_t Partition;
	byte SurroundType;
	byte SolidType;

	byte TriggerBloat;
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
