using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

public struct EntityListAlongRay : IPartitionEnumerator
{
	public const int MAX_ENTITIES_ALONGRAY = 1024;
	[InlineArray(MAX_ENTITIES_ALONGRAY)]
	public struct InlineArrayMaxEntitiesAlongRay<T> { public T? first; }

	public int Length;
	public InlineArrayMaxEntitiesAlongRay<IHandleEntity?> EntityHandles;

	public void Reset() => Length = 0;
	public int Count() => Length;

	public IterationRetval EnumElement(IHandleEntity? handleEntity) {
		if (Length < MAX_ENTITIES_ALONGRAY)
			EntityHandles[Length++] = handleEntity;
		else
			DevMsg(1, "Max entity count along ray exceeded!\n");

		return IterationRetval.Continue;
	}
}

public abstract class EngineTrace : IEngineTrace
{
	public abstract ICollideable? GetWorldCollideable();
	public abstract void SetTraceEntity(ICollideable? collideable, ref Trace trace);

	public void ClipRayToCollideable(in Ray ray, Mask mask, ICollideable? collide, ref Trace trace) {
		throw new NotImplementedException();
	}

	public void ClipRayToEntity(in Ray ray, Mask mask, IHandleEntity ent, ref Trace trace) {
		throw new NotImplementedException();
	}

	public void EnumerateEntities(in Ray ray, bool triggers, IEntityEnumerator enumerator) {
		throw new NotImplementedException();
	}

	public void EnumerateEntities(in Vector3 absMins, in Vector3 absMaxs, IEntityEnumerator enumerator) {
		throw new NotImplementedException();
	}

	public void GetBrushesInAABB(in Vector3 mins, in Vector3 maxs, List<int> output, Contents contentsMask = (Contents)(-1)) {
		throw new NotImplementedException();
	}

	public bool GetBrushInfo(int iBrush, ref List<Vector4> planesOut, out Contents contents) {
		throw new NotImplementedException();
	}

	public IPhysCollide? GetCollidableFromDisplacementsInAABB(in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public ICollideable? GetCollideable(IHandleEntity? entity) {
		throw new NotImplementedException();
	}

	public int GetLeafContainingPoint(in Vector3 test) {
		throw new NotImplementedException();
	}

	public Contents GetPointContents(in Vector3 absPosition, out IHandleEntity? entity) {
		entity = null;
		return 0; // todo
	}

	public Contents GetPointContents_Collideable(ICollideable? collide, in Vector3 absPosition) {
		throw new NotImplementedException();
	}

	public int GetStatByIndex(int index, bool clear) {
		throw new NotImplementedException();
	}

	public void SetupLeafAndEntityListBox(in Vector3 boxMin, in Vector3 boxMax, TraceListData traceData) {
		throw new NotImplementedException();
	}

	public void SetupLeafAndEntityListRay(in Ray ray, TraceListData traceData) {
		throw new NotImplementedException();
	}

	public void SweepCollideable<Filter>(ICollideable? collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, Mask mask, scoped ref Filter traceFilter, ref Trace trace) where Filter : struct, ITraceFilter {
		throw new NotImplementedException();
	}

	protected abstract int SpatialPartitionMask();
	protected abstract int SpatialPartitionTriggerMask();

	public void TraceRay<Filter>(in Ray ray, Mask mask, scoped ref Filter traceFilter, out Trace trace) where Filter : struct, ITraceFilter {
		trace = default;
		CM.ClearTrace(ref trace);

		if (traceFilter.GetTraceType() != TraceType.EntitiesOnly) {
			ICollideable? collide = GetWorldCollideable();
			Assert(collide != null);

			// Make sure the world entity is unrotated
			// FIXME: BAH! The !pCollide test here is because of
			// CStaticProp::PrecacheLighting.. it's occurring too early
			// need to fix that later
			Assert(collide == null || collide.GetCollisionOrigin() == vec3_origin);
			Assert(collide == null || collide.GetCollisionAngles() == vec3_angle);

			CM.BoxTrace(in ray, 0, mask, true, ref trace);
			SetTraceEntity(collide, ref trace);

			// inside world, no need to check being inside anything else
			if (trace.StartSolid)
				return;

			if (traceFilter.GetTraceType() == TraceType.WorldOnly)
				return;
		}
		else {
			// Set initial start + endpos, necessary if the world isn't traced against 
			// because we may not trace against *anything* below.
			MathLib.VectorAdd(ray.Start, ray.StartOffset, out trace.StartPos);
			MathLib.VectorAdd(trace.StartPos, ray.Delta, out trace.EndPos);
		}

		// Save the world collision fraction.
		float flWorldFraction = trace.Fraction;
		float flWorldFractionLeftSolidScale = flWorldFraction;

		// Create a ray that extends only until we hit the world
		// and adjust the trace accordingly
		Ray entityRay = ray;

		if (trace.Fraction == 0) {
			entityRay.Delta.Init();
			flWorldFractionLeftSolidScale = trace.FractionLeftSolid;
			trace.FractionLeftSolid = 1.0f;
			trace.Fraction = 1.0f;
		}
		else {
			// Explicitly compute end so that this computation happens at the quantization of
			// the output (endpos).  That way we won't miss any intersections we would get
			// by feeding these results back in to the tracer
			// This is not the same as entityRay.m_Delta *= pTrace->fraction which happens 
			// at a quantization that is more precise as m_Start moves away from the origin
			Vector3 end;
			MathLib.VectorMA(entityRay.Start, trace.Fraction, entityRay.Delta, out end);
			MathLib.VectorSubtract(end, entityRay.Start, out entityRay.Delta);
			// We know this is safe because pTrace->fraction != 0
			trace.FractionLeftSolid /= trace.Fraction;
			trace.Fraction = 1.0f;
		}

		// Collide with entities along the ray
		// FIXME: Hitbox code causes this to be re-entrant for the IK stuff.
		// If we could eliminate that, this could be static and therefore
		// not have to reallocate memory all the time
		EntityListAlongRay enumerator = default;
		enumerator.Reset();
		SpatialPartition().EnumerateElementsAlongRay(SpatialPartitionMask(), entityRay, false, ref enumerator);

		bool noStaticProps = traceFilter.GetTraceType() == TraceType.EntitiesOnly;
		bool filterStaticProps = traceFilter.GetTraceType() == TraceType.EverythingFilterProps;

		Trace tr = default;
		ICollideable? collideable;
		ReadOnlySpan<char> debugName;
		int nCount = enumerator.Count();
		for (int i = 0; i < nCount; ++i) {
			// Generate a collideable
			IHandleEntity? handleEntity = enumerator.EntityHandles[i];
			HandleEntityToCollideable(handleEntity, out collideable, out debugName);

			if (!StaticPropMgr().IsStaticProp(handleEntity)) {
				if (!traceFilter.ShouldHitEntity(handleEntity!, (Contents)mask))
					continue;
			}
			else {
				// FIXME: Could remove this check here by
				// using a different spatial partition mask. Look into it
				// if we want more speedups here.
				if (noStaticProps)
					continue;

				if (filterStaticProps) {
					if (!traceFilter.ShouldHitEntity(handleEntity!, (Contents)mask))
						continue;
				}
			}

			ClipRayToCollideable(entityRay, mask, collideable, ref tr);

			// Make sure the ray is always shorter than it currently is
			ClipTraceToTrace(ref tr, ref trace);

			// Stop if we're in allsolid
			if (trace.AllSolid)
				break;
		}

		// Fix up the fractions so they are appropriate given the original
		// unclipped-to-world ray
		trace.Fraction *= flWorldFraction;
		trace.FractionLeftSolid *= flWorldFractionLeftSolidScale;
	}

	public void HandleEntityToCollideable(IHandleEntity? handleEntity, out ICollideable? collide, out ReadOnlySpan<char> debugName){
		collide = StaticPropMgr().GetStaticProp(handleEntity);
		if (collide != null) {
			debugName = "static prop";
			return;
		}

		IServerUnknown? serverUnknown = (IServerUnknown?)handleEntity;
		if (serverUnknown == null || serverUnknown.GetNetworkable() == null) {
			collide = null;
			debugName = "<null>";
			return;
		}

		collide = serverUnknown.GetCollideable();
		debugName = serverUnknown.GetNetworkable()!.GetClassName();
	}

	public void TraceRayAgainstLeafAndEntityList<Filter>(in Ray ray, TraceListData traceData, Mask mask, scoped ref Filter traceFilter, ref Trace trace) where Filter : struct, ITraceFilter {
		throw new NotImplementedException();
	}

	public bool ClipTraceToTrace(ref Trace clipTrace, ref Trace finalTrace) {
		if (clipTrace.AllSolid || clipTrace.StartSolid || (clipTrace.Fraction < finalTrace.Fraction)) {
			if (finalTrace.StartSolid) {
				float flFractionLeftSolid = finalTrace.FractionLeftSolid;
				Vector3 vecStartPos = finalTrace.StartPos;

				finalTrace = clipTrace;
				finalTrace.StartSolid = true;

				if (flFractionLeftSolid > clipTrace.FractionLeftSolid) {
					finalTrace.FractionLeftSolid = flFractionLeftSolid;
					finalTrace.StartPos = vecStartPos;
				}
			}
			else
				finalTrace = clipTrace;
			return true;
		}

		if (clipTrace.StartSolid) {
			finalTrace.StartSolid = true;

			if (clipTrace.FractionLeftSolid > finalTrace.FractionLeftSolid) {
				finalTrace.FractionLeftSolid = clipTrace.FractionLeftSolid;
				finalTrace.StartPos = clipTrace.StartPos;
			}
		}

		return false;
	}
}


public class EngineTraceClient : EngineTrace
{
	protected override int SpatialPartitionMask() => (int)PartitionListMask.ClientSolidEdicts;
	protected override int SpatialPartitionTriggerMask() => 0;
	public override ICollideable? GetWorldCollideable() {
		IClientEntity? pUnk = entitylist.GetClientEntity(0);
		return pUnk?.GetCollideable();
	}

	public override void SetTraceEntity(ICollideable? collideable, ref Trace trace) {
		if (!trace.DidHit())
			return;

		// FIXME: This is only necessary because of traces occurring during
		// LevelInit (a suspect time to be tracing)
		if (collideable == null) {
			trace.EntHandle = null;
			return;
		}

		IClientUnknown? unk = (IClientUnknown?)collideable.GetEntityHandle();
		// TODO: Static props have logic here but no static prop manager yet
		trace.EntHandle = unk?.GetIClientEntity();
	}
}

public class EngineTraceServer : EngineTrace
{
	protected override int SpatialPartitionMask() => (int)PartitionListMask.EngineSolidEdicts;
	protected override int SpatialPartitionTriggerMask() => (int)PartitionListMask.EngineTriggerEdicts;

	public override ICollideable? GetWorldCollideable() {
		if (sv.Edicts == null)
			return null;
		return sv.Edicts[0].GetCollideable();
	}

	public override void SetTraceEntity(ICollideable? collideable, ref Trace trace) {
		if (!trace.DidHit())
			return;

		// FIXME: This is only necessary because of traces occurring during
		// LevelInit (a suspect time to be tracing)
		if (collideable == null) {
			trace.EntHandle = null;
			return;
		}

		IHandleEntity? handleEntity = collideable.GetEntityHandle();
		// TODO: Static props have logic here but no static prop manager yet
		trace.EntHandle = handleEntity;
	}
}
