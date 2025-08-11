namespace Source.Common.Engine;

using Source.Common.Entity;
using System.Collections.Generic;
using System.Numerics;

public class Ray { }
public class GameTrace { } // RaphaelIT7: Just as a note, a Ray & a GameTrace are two completely different things.
public class IClientRenderable { }
public class IPhysicsEnvironment { }
public class IVPhysicsKeyHandler { }
public class ICollideable { }

public interface IStaticPropMgr
{
	void CreateVPhysicsRepresentations(IPhysicsEnvironment physenv, IVPhysicsKeyHandler defaults, object gameData);

	void TraceRayAgainstStaticProp(Ray ray, int staticPropIndex, GameTrace tr);

	bool IsStaticProp(IHandleEntity handleEntity);
	bool IsStaticProp(BaseHandle handle);
	ICollideable GetStaticPropByIndex(int propIndex);
}

public interface IStaticPropMgrClient : IStaticPropMgr
{
	void ComputePropOpacity(Vector3 viewOrigin, float factor);
	void AddDecalToStaticProp(Vector3 rayStart, Vector3 rayEnd, int staticPropIndex, int decalIndex, bool doTrace, GameTrace tr);
	void AddShadowToStaticProp(ushort shadowHandle, IClientRenderable renderable);
	void RemoveAllShadowsFromStaticProp(IClientRenderable renderable);
	void GetStaticPropMaterialColorAndLighting(GameTrace trace, int staticPropIndex, out Vector3 lighting, out Vector3 matColor);
	void GetAllStaticProps(List<ICollideable> output);
	void GetAllStaticPropsInAABB(Vector3 mins, Vector3 maxs, List<ICollideable> output);
	void GetAllStaticPropsInOBB(Vector3 origin, Vector3 extent1, Vector3 extent2, Vector3 extent3, List<ICollideable> output);
	void DrawStaticProps(IClientRenderable[] props, int count, bool shadowDepth, bool drawVCollideWireframe);
}

public interface IStaticPropMgrServer : IStaticPropMgr
{
	void GetAllStaticProps(List<ICollideable> output);
	void GetAllStaticPropsInAABB(Vector3 mins, Vector3 maxs, List<ICollideable> output);
	void GetAllStaticPropsInOBB(Vector3 origin, Vector3 extent1, Vector3 extent2, Vector3 extent3, List<ICollideable> output);
}