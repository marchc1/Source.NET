using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Common.Engine;

public enum TraceType
{
	Everything = 0,
	WorldOnly,
	EntitiesOnly,
	EverythingFilterProps,
}

public interface ITraceFilter
{
	bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask);
	TraceType GetTraceType();
}

public class TraceFilter : ITraceFilter
{
	public virtual bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask) => throw new NotImplementedException();
	public TraceType GetTraceType() => TraceType.Everything;
}

public class TraceFilterEntitiesOnly : ITraceFilter
{
	public bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask) => throw new NotImplementedException();
	public TraceType GetTraceType() => TraceType.EntitiesOnly;
}

public class TraceFilterWorldOnly : ITraceFilter
{
	public bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask) => false;
	public TraceType GetTraceType() => TraceType.WorldOnly;
}

public class TraceFilterWorldAndPropsOnly : ITraceFilter
{
	public bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask) => false;
	public TraceType GetTraceType() => TraceType.Everything;
}

public class TraceFilterHitAll : TraceFilter
{
	public override bool ShouldHitEntity(IHandleEntity entity, Contents contentsMask) => true;
}

public interface IEntityEnumerator; // todo
public interface IEngineTrace
{
	Contents GetPointContents(in Vector3 absPosition, out IHandleEntity? entity);
	Contents GetPointContents_Collideable(ICollideable? collide, in Vector3 absPosition);
	void ClipRayToEntity(in Ray ray, Mask mask, IHandleEntity ent, ref Trace trace);
	void ClipRayToCollideable(in Ray ray, Mask mask, ICollideable collide, ref Trace trace);
	void TraceRay<Filter>(in Ray ray, Mask mask, Filter traceFilter, ref Trace trace) where Filter : ITraceFilter;
	void SetupLeafAndEntityListRay(in Ray ray, TraceListData traceData);
	void SetupLeafAndEntityListBox(in Vector3 boxMin, in Vector3 boxMax, TraceListData traceData);
	void TraceRayAgainstLeafAndEntityList<Filter>(in Ray ray, TraceListData traceData, Mask mask, Filter traceFilter, ref Trace trace) where Filter : ITraceFilter;
	void SweepCollideable<Filter>(ICollideable? collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, Mask mask, in Filter traceFilter, ref Trace trace) where Filter : ITraceFilter;
	void EnumerateEntities(in Ray ray, bool triggers, IEntityEnumerator enumerator);
	void EnumerateEntities(in Vector3 absMins, in Vector3 absMaxs, IEntityEnumerator enumerator);
	ICollideable? GetCollideable(IHandleEntity? entity);
	int GetStatByIndex(int index, bool clear);
	void GetBrushesInAABB(in Vector3 mins, in Vector3 maxs, List<int> output, Contents contentsMask = unchecked((Contents)0xFFFFFFFF));
	IPhysCollide? GetCollidableFromDisplacementsInAABB(in Vector3 mins, in Vector3 maxs);
	bool GetBrushInfo(int iBrush, ref List<Vector4> planesOut, out Contents contents);
	int GetLeafContainingPoint(in Vector3 test);
}
