global using static Source.Engine.StaticPropMgrGlobals;

using Source.Common;
using Source.Common.Engine;
using Source.Common.Physics;

namespace Source.Engine;


public static class StaticPropMgrGlobals
{
	public static readonly StaticPropMgrImpl g_StaticPropMgr = new();
	public static IStaticPropMgrEngine StaticPropMgr() => g_StaticPropMgr;
}


public class StaticPropMgrImpl : IStaticPropMgrEngine, IStaticPropMgrClient, IStaticPropMgrServer
{
	public void AddDecalToStaticProp(AngularImpulse rayStart, AngularImpulse rayEnd, int staticPropIndex, int decalIndex, bool doTrace, Trace tr) {
		throw new NotImplementedException();
	}

	public void AddShadowToStaticProp(ushort shadowHandle, IClientRenderable renderable) {
		throw new NotImplementedException();
	}

	public void ComputePropOpacity(AngularImpulse viewOrigin, float factor) {
		throw new NotImplementedException();
	}

	public void CreateVPhysicsRepresentations(IPhysicsEnvironment physenv, IVPhysicsKeyHandler defaults, object gameData) {
		throw new NotImplementedException();
	}

	public void DrawStaticProps(IClientRenderable[] props, int count, bool shadowDepth, bool drawVCollideWireframe) {
		throw new NotImplementedException();
	}

	public void GetAllStaticProps(List<ICollideable> output) {
		throw new NotImplementedException();
	}

	public void GetAllStaticPropsInAABB(AngularImpulse mins, AngularImpulse maxs, List<ICollideable> output) {
		throw new NotImplementedException();
	}

	public void GetAllStaticPropsInOBB(AngularImpulse origin, AngularImpulse extent1, AngularImpulse extent2, AngularImpulse extent3, List<ICollideable> output) {
		throw new NotImplementedException();
	}

	public nint GetLightCacheHandleForStaticProp(IHandleEntity? handleEntity) {
		throw new NotImplementedException();
	}

	public ICollideable? GetStaticProp(IHandleEntity? handleEntity) {
		throw new NotImplementedException();
	}

	public ICollideable GetStaticPropByIndex(int propIndex) {
		throw new NotImplementedException();
	}

	public int GetStaticPropIndex(IHandleEntity? handleEntity) {
		throw new NotImplementedException();
	}

	public void GetStaticPropMaterialColorAndLighting(Trace trace, int staticPropIndex, out AngularImpulse lighting, out AngularImpulse matColor) {
		throw new NotImplementedException();
	}

	public bool Init() {
		return true;
	}

	public bool IsPropInPVS(IHandleEntity? handleEntity, ReadOnlySpan<byte> vis) {
		throw new NotImplementedException();
	}

	public bool IsStaticProp(IHandleEntity? handleEntity) {
		return false; // todo
	}

	public bool IsStaticProp(ClientEntityHandle handle) {
		return false; // todo
	}

	public bool IsStaticProp(in ClientEntityHandle handle) {
		return false; // todo
	}

	public void LevelInit() {

	}

	public void LevelInitClient() {

	}

	public void LevelShutdown() {

	}

	public void LevelShutdownClient() {

	}

	public bool PropHasBakedLightingDisabled(IHandleEntity? handleEntity) {
		throw new NotImplementedException();
	}

	public void RecomputeStaticLighting() {
		throw new NotImplementedException();
	}

	public void RemoveAllShadowsFromStaticProp(IClientRenderable renderable) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}

	public void TraceRayAgainstStaticProp(Ray ray, int staticPropIndex, Trace tr) {
		throw new NotImplementedException();
	}
}
