using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Engine;

public static partial class CM
{
	public static int BoxLeafnums(in Vector3 mins, in Vector3 maxs, Span<int> list, out int topnode) {
		LeafNums context = default;
		context.LeafList = list;
		context.LeafTopNode = -1;
		context.LeafMaxCount = list.Length;
		// get the current collision bsp -- there is only one!
		context.BSPData = GetCollisionBSPData();
		Vector3 center = (mins + maxs) * 0.5f;
		Vector3 extents = maxs - center;
		int leafCount = BoxLeafnums(ref context, center, extents, context.BSPData.MapCollisionModels[0].HeadNode);

		topnode = context.LeafTopNode;

		return leafCount;
	}

	//-----------------------------------------------------------------------------
	// Purpose: returns true if the box is in a cluster that is visible in the visbits
	// Input  : mins - box extents
	//			maxs -
	//			*visbits - pvs or pas of some cluster
	// Output : true if visible, false if not
	//-----------------------------------------------------------------------------
	const int MAX_BOX_LEAVES = 256;
	public static bool BoxVisible(in Vector3 mins, in Vector3 maxs, ReadOnlySpan<byte> visbits) {
		Span<int> leafList = stackalloc int[MAX_BOX_LEAVES];

		// FIXME: Could save a loop here by traversing the tree in this routine like the code above
		int count = BoxLeafnums(mins, maxs, leafList, out _);
		for (int i = 0; i < count; i++) {
			int cluster = LeafCluster(leafList[i]);
			int offset = cluster >> 3;

			if (offset == -1)
				return false;

			if (offset > visbits.Length || offset < 0)
				Sys.Error($"CM_BoxVisible:  cluster {cluster}, offset {offset} out of bounds {visbits.Length}\n");

			if ((visbits[cluster >> 3] & (1 << (cluster & 7))) != 0)
				return true;
		}
		return false;
	}

	public static void WorldSpaceBounds(ICollideable collideable, out Vector3 mins, out Vector3 maxs) {
		if (collideable.GetCollisionAngles() == vec3_angle) {
			MathLib.VectorAdd(collideable.GetCollisionOrigin(), collideable.OBBMins(), out mins);
			MathLib.VectorAdd(collideable.GetCollisionOrigin(), collideable.OBBMaxs(), out maxs);
		}
		else
			MathLib.TransformAABB(collideable.CollisionToWorldTransform(), collideable.OBBMins(), collideable.OBBMaxs(), out mins, out maxs);
	}
}

public static class WorldTouch
{
	//-----------------------------------------------------------------------------
	// Trigger world-space bounds
	//-----------------------------------------------------------------------------
	static void CM_TriggerWorldSpaceBounds(ICollideable collideable, out Vector3 mins, out Vector3 maxs) {
		if ((collideable.GetSolidFlags() & (int)SolidFlags.UseTriggerBounds) != 0)
			collideable.WorldSpaceTriggerBounds(out mins, out maxs);
		else
			CM.WorldSpaceBounds(collideable, out mins, out maxs);
	}

	static void CM_GetCollideableTriggerTestBox(ICollideable collide, out Vector3 mins, out Vector3 maxs, bool useAccurateBbox) {
		if (useAccurateBbox && collide.GetSolid() == SolidType.BBox) {
			mins = collide.OBBMins();
			maxs = collide.OBBMaxs();
		}
		else {
			Vector3 vecStart = collide.GetCollisionOrigin();
			collide.WorldSpaceSurroundingBounds(out mins, out maxs);
			mins -= vecStart;
			maxs -= vecStart;
		}
	}

	static Edict? EdictFromHandleEntity(IHandleEntity? handleEntity) {
		IServerUnknown? unk = handleEntity as IServerUnknown;
		Assert(unk != null);
		return unk?.GetNetworkable()?.GetEdict();
	}

	//-----------------------------------------------------------------------------
	// Little enumeration class used to try touching all triggers
	//-----------------------------------------------------------------------------
	struct TouchLinks : IPartitionEnumerator
	{
		public Ray Ray;

		readonly Edict Ent;
		readonly ICollideable Collide;
		readonly List<Edict> TouchedEntities;

		public TouchLinks(Edict ent, Vector3? prevAbsOrigin, bool accurateBboxTriggerChecks) {
			TouchedEntities = new(8);
			Ent = ent;
			Collide = ent.GetCollideable()!;
			Assert(Collide != null);

			CM_GetCollideableTriggerTestBox(Collide, out Vector3 vecMins, out Vector3 vecMaxs, accurateBboxTriggerChecks);
			Vector3 vecStart = Collide.GetCollisionOrigin();

			if (prevAbsOrigin.HasValue)
				Ray.Init(prevAbsOrigin.Value, vecStart, vecMins, vecMaxs);
			else
				Ray.Init(vecStart, vecStart, vecMins, vecMaxs);
		}

		public IterationRetval EnumElement(IHandleEntity? handleEntity) {
			// Static props should never be in the trigger list
			Assert(!StaticPropMgr().IsStaticProp(handleEntity));

			// Convert the IHandleEntity to an edict_t*...
			// Context is the thing we're testing everything against
			Edict? touch = EdictFromHandleEntity(handleEntity);

			// Can't bump against itself
			if (touch == null || touch == Ent)
				return IterationRetval.Continue;

			IServerEntity? triggerEntity = touch.GetIServerEntity();
			if (triggerEntity == null)
				return IterationRetval.Continue;

			// Hmmm.. everything in this list should be a trigger....
			ICollideable triggerCollideable = triggerEntity.GetCollideable()!;
			if (!Collide.ShouldTouchTrigger(triggerCollideable.GetSolidFlags()))
				return IterationRetval.Continue;

			Assert((triggerCollideable.GetSolidFlags() & (int)SolidFlags.Trigger) != 0);

			if ((triggerCollideable.GetSolidFlags() & (int)SolidFlags.UseTriggerBounds) != 0) {
				triggerCollideable.WorldSpaceTriggerBounds(out Vector3 vecTriggerMins, out Vector3 vecTriggerMaxs);
				if (!CollisionUtils.IsBoxIntersectingRay(vecTriggerMins, vecTriggerMaxs, Ray))
					return IterationRetval.Continue;
			}
			else {
				Trace tr = default;
				g_pEngineTraceServer.ClipRayToCollideable(Ray, Mask.Solid, triggerCollideable, ref tr);
				if ((tr.Contents & (Contents)Mask.Solid) == 0)
					return IterationRetval.Continue;
			}

			TouchedEntities.Add(touch);

			return IterationRetval.Continue;
		}

		public readonly void HandleTouchedEntities() {
			for (int i = 0; i < TouchedEntities.Count; ++i)
				SV.ServerGameEnts!.MarkEntitiesAsTouching(TouchedEntities[i], Ent);
		}
	}

	// enumerator class that's used to update touch links for a trigger when
	// it moves or changes solid type
	struct TriggerMovedEnum : IPartitionEnumerator
	{
		Edict TriggerEntity;
		ICollideable Trigger;
		int TriggerSolidFlags;
		readonly List<Edict> TouchedEntities;
		readonly bool AccurateBBoxCheck;

		public TriggerMovedEnum(bool accurateBboxTriggerChecks) {
			TouchedEntities = new(8);
			AccurateBBoxCheck = accurateBboxTriggerChecks;
			TriggerEntity = null!;
			Trigger = null!;
		}

		public void TriggerMoved(Edict triggerEntity) {
			TriggerEntity = triggerEntity;
			Trigger = triggerEntity.GetCollideable()!;
			TriggerSolidFlags = Trigger.GetSolidFlags();
			CM_TriggerWorldSpaceBounds(Trigger, out Vector3 vecAbsMins, out Vector3 vecAbsMaxs);
			SpatialPartition().EnumerateElementsInBox((SpatialPartitionListMask_t)PartitionListMask.EngineSolidEdicts,
				vecAbsMins, vecAbsMaxs, false, ref this);
		}

		public IterationRetval EnumElement(IHandleEntity? handleEntity) {
			// skip static props, the game DLL doesn't care about them
			if (StaticPropMgr().IsStaticProp(handleEntity))
				return IterationRetval.Continue;

			IServerUnknown? unk = handleEntity as IServerUnknown;
			Assert(unk != null);

			// Convert the user ID to and edict_t*...
			Edict? touch = unk?.GetNetworkable()?.GetEdict();
			Assert(touch != null);
			ICollideable? touchCollide = unk?.GetCollideable();
			if (touch == null || touchCollide == null)
				return IterationRetval.Continue;

			// Can't ever touch itself because it's in the other list
			if (touchCollide == Trigger)
				return IterationRetval.Continue;

			if (!touchCollide.ShouldTouchTrigger(TriggerSolidFlags))
				return IterationRetval.Continue;

			IServerEntity? serverEntity = touch.GetIServerEntity();
			if (serverEntity == null)
				return IterationRetval.Continue;

			// FIXME: Should we be using the surrounding bounds here?
			CM_GetCollideableTriggerTestBox(touchCollide, out Vector3 vecMins, out Vector3 vecMaxs, AccurateBBoxCheck);

			Vector3 vecStart = touchCollide.GetCollisionOrigin();
			Ray ray = default;
			ray.Init(vecStart, vecStart, vecMins, vecMaxs);

			if ((Trigger.GetSolidFlags() & (int)SolidFlags.UseTriggerBounds) != 0) {
				Trigger.WorldSpaceTriggerBounds(out Vector3 vecTriggerMins, out Vector3 vecTriggerMaxs);
				if (!CollisionUtils.IsBoxIntersectingRay(vecTriggerMins, vecTriggerMaxs, ray))
					return IterationRetval.Continue;
			}
			else {
				Trace tr = default;
				g_pEngineTraceServer.ClipRayToCollideable(ray, Mask.Solid, Trigger, ref tr);
				if ((tr.Contents & (Contents)Mask.Solid) == 0)
					return IterationRetval.Continue;
			}

			TouchedEntities.Add(touch);

			return IterationRetval.Continue;
		}

		public readonly void HandleTouchedEntities() {
			for (int i = 0; i < TouchedEntities.Count; ++i)
				SV.ServerGameEnts!.MarkEntitiesAsTouching(TouchedEntities[i], TriggerEntity);
		}
	}

	//-----------------------------------------------------------------------------
	// Touches triggers. Or, if it is a trigger, causes other things to touch it
	// returns true if untouch needs to be checked
	//-----------------------------------------------------------------------------
	public static void SV_TriggerMoved(Edict triggerEnt, bool accurateBboxTriggerChecks) {
		TriggerMovedEnum triggerEnum = new(accurateBboxTriggerChecks);
		triggerEnum.TriggerMoved(triggerEnt);
		triggerEnum.HandleTouchedEntities();
	}

	public static void SV_SolidMoved(Edict solidEnt, ICollideable solidCollide, Vector3? prevAbsOrigin, bool accurateBboxTriggerChecks) {
		if (!prevAbsOrigin.HasValue) {
			TouchLinks touchEnumerator = new(solidEnt, null, accurateBboxTriggerChecks);

			solidCollide.WorldSpaceSurroundingBounds(out Vector3 vecWorldMins, out Vector3 vecWorldMaxs);

			SpatialPartition().EnumerateElementsInBox((SpatialPartitionListMask_t)PartitionListMask.EngineTriggerEdicts,
				vecWorldMins, vecWorldMaxs, false, ref touchEnumerator);

			touchEnumerator.HandleTouchedEntities();
		}
		else {
			TouchLinks touchEnumerator = new(solidEnt, prevAbsOrigin, accurateBboxTriggerChecks);

			// A version that checks against an extruded ray indicating the motion
			SpatialPartition().EnumerateElementsAlongRay((SpatialPartitionListMask_t)PartitionListMask.EngineTriggerEdicts,
				touchEnumerator.Ray, false, ref touchEnumerator);

			touchEnumerator.HandleTouchedEntities();
		}
	}
}
