using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Engine;

public class EngineTrace : IEngineTrace
{
	public void ClipRayToCollideable(in Ray ray, uint mask, ICollideable collide, ref Trace trace) {
		throw new NotImplementedException();
	}

	public void ClipRayToEntity(in Ray ray, uint mask, IHandleEntity ent, ref Trace trace) {
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

	public void SweepCollideable<Filter>(ICollideable? collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, uint mask, in Filter traceFilter, ref Trace trace) where Filter : ITraceFilter {
		throw new NotImplementedException();
	}

	public void TraceRay<Filter>(in Ray ray, uint mask, in Filter traceFilter, ref Trace trace) where Filter : ITraceFilter {
		throw new NotImplementedException();
	}

	public void TraceRayAgainstLeafAndEntityList<Filter>(in Ray ray, TraceListData traceData, uint mask, in Filter traceFilter, ref Trace trace) where Filter : ITraceFilter {
		throw new NotImplementedException();
	}
}
