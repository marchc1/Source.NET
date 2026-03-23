using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Engine;

public abstract class EngineTrace : IEngineTrace
{
	public abstract ICollideable? GetWorldCollideable();
	public abstract void SetTraceEntity(ICollideable? collideable, ref Trace trace);

	public void ClipRayToCollideable(in Ray ray, Mask mask, ICollideable collide, ref Trace trace) {
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

	public void SweepCollideable<Filter>(ICollideable? collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, Mask mask, in Filter traceFilter, ref Trace trace) where Filter : ITraceFilter {
		throw new NotImplementedException();
	}

	static readonly TraceFilterHitAll hitAllTraceFilter = new();
	public void TraceRay<Filter>(in Ray ray, Mask mask, Filter inTraceFilter, ref Trace trace) where Filter : ITraceFilter {
		ITraceFilter traceFilter = inTraceFilter;
		traceFilter ??= hitAllTraceFilter;

		CM.ClearTrace(ref trace);

		if(traceFilter.GetTraceType() != TraceType.EntitiesOnly){
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
	}

	public void TraceRayAgainstLeafAndEntityList<Filter>(in Ray ray, TraceListData traceData, Mask mask, Filter traceFilter, ref Trace trace) where Filter : ITraceFilter {
		throw new NotImplementedException();
	}
}


public class EngineTraceClient : EngineTrace
{
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
