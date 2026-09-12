using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Server;

public struct DamageEvent
{
	public BaseEntity? Entity;
	public IPhysicsObject? InflictorPhysics;
	public TakeDamageInfo Info;
	public bool RestoreVelocity;
}

public struct InflictorState
{
	public Vector3 SavedVelocity;
	public Vector3 SavedAngularVelocity;
	public IPhysicsObject? InflictorPhysics;
	public float OtherMassMax;
	public short NextIndex;
	public bool Restored;
}

public enum CollState
{
	Enabled,
	TryDisable,
	TryNPCSolver,
	TryEntitySolver,
	Disabled
}

public struct PenetrateEvent
{
	public EHANDLE Entity0;
	public EHANDLE Entity1;
	public TimeUnit_t StartTime;
	public TimeUnit_t TimeStamp;
	public CollState CollisionState;
}

public class CollisionEvent : IPhysicsCollisionEvent, IPhysicsCollisionSolver, IPhysicsObjectEvent
{
	readonly Friction[] Current = new Friction[4];
	readonly GameVCollisionEvent gameEvent = default;
	readonly List<TriggerEvent> triggerEvents = [];
	readonly TriggerEvent currentTriggerEvent = default;
	readonly List<TouchEvent> touchEvents = [];
	readonly List<DamageEvent> damageEvents = [];
	readonly List<InflictorState> damageInflictors = [];
	readonly List<PenetrateEvent> penetrateEvents = [];
	readonly List<FluidEvent> fluidEvents = [];
	readonly List<IServerNetworkable> removeObjects = [];
	int inCallback;
	int lastTickFrictionError;
	bool bufferTouchEvents;


	public bool IsInCallback() => inCallback > 0;


	public virtual void AddDamageEvent(BaseEntity entity, in TakeDamageInfo info, IPhysicsObject inflictorPhysics, bool restoreVelocity, in Vector3 savedVel, in Vector3 savedAngVel) {
		// todo
	}

	public int AdditionalCollisionChecksThisTick(int currentChecksDone) {
		throw new NotImplementedException();
	}

	public void EndTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData toichData) {
		throw new NotImplementedException();
	}

	public void FluidEndTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		throw new NotImplementedException();
	}

	public void FluidStartTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		throw new NotImplementedException();
	}

	public void Friction(IPhysicsObject obj, float energy, int surfaceProps, int surfacePropsHit, IPhysicsCollisionData data) {
		throw new NotImplementedException();
	}

	public void ObjectSleep(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void ObjectWake(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public void PostCollision(ref VCollisionEvent ev) {
		throw new NotImplementedException();
	}

	public void PostSimulationFrame() {
		UpdateDamageEvents();
		while (g_PostSimulationQueue.TryDequeue(out Action? a))
			a.Invoke();
		UpdateRemoveObjects();
	}

	private void UpdateRemoveObjects() {
		Assert(!PhysIsInCallback());
		for (int i = 0; i < removeObjects.Count; i++) 
			Util.Remove(removeObjects[i]);
		
		removeObjects.Clear();
	}

	private void UpdateDamageEvents() {
		Span<DamageEvent> damageEvents = this.damageEvents.AsSpan();
		for (int i = 0; i < damageEvents.Length; i++) {
			ref DamageEvent ev = ref damageEvents[i];

			// Track changes in the entity's life state
			int iEntBits = ev.Entity!.IsAlive() ? 0x0001 : 0;
			iEntBits |= ev.Entity.IsMarkedForDeletion() ? 0x0002 : 0;
			iEntBits |= (ev.Entity.GetSolidFlags() & Source.SolidFlags.NotSolid) != 0 ? 0x0004 : 0;

			ev.Entity.TakeDamage(ev.Info);
			int iEntBits2 = ev.Entity.IsAlive() ? 0x0001 : 0;
			iEntBits2 |= ev.Entity.IsMarkedForDeletion() ? 0x0002 : 0;
			iEntBits2 |= (ev.Entity.GetSolidFlags() & Source.SolidFlags.NotSolid) != 0 ? 0x0004 : 0;

			if (ev.RestoreVelocity && iEntBits != iEntBits2) {
				// UNDONE: Use ratio of masses to blend in a little of the collision response?
				// UNDONE: Damage for future events is already computed - it would be nice to
				//			go back and recompute it now that the values have
				//			been adjusted
				RestoreDamageInflictorState(ev.InflictorPhysics);
			}
		}
		this.damageEvents.Clear();
		this.damageInflictors.Clear();
	}

	private void RestoreDamageInflictorState(int inflictorStateIndex, float velocityBlend) {
		ref InflictorState state = ref damageInflictors.AsSpan()[inflictorStateIndex];
		if (state.Restored)
			return;

		// so we only restore this guy once
		state.Restored = true;

		if (velocityBlend > 0) {
			state.InflictorPhysics!.GetVelocity(out Vector3 velocity, out Vector3 angVel);
			state.SavedVelocity = state.SavedVelocity * velocityBlend + velocity * (1 - velocityBlend);
			state.SavedAngularVelocity = state.SavedAngularVelocity * velocityBlend + angVel * (1 - velocityBlend);
			state.InflictorPhysics.SetVelocity(state.SavedVelocity, state.SavedAngularVelocity);
		}

		if (state.NextIndex >= 0) 
			RestoreDamageInflictorState(state.NextIndex, velocityBlend);
	}

	public int FindDamageInflictor(IPhysicsObject inflictorPhysics){
		Span<InflictorState> damageInflictors = this.damageInflictors.AsSpan();
		for (int i = damageInflictors.Length - 1; i >= 0; --i) {
			ref InflictorState state = ref damageInflictors[i];
			if (state.InflictorPhysics == inflictorPhysics)
				return i;
		}

		return -1;
	}

	private void RestoreDamageInflictorState(IPhysicsObject? inflictor) {
		if (inflictor == null)
			return;

		int index = FindDamageInflictor(inflictor);
		if (index >= 0) {
			ref InflictorState state = ref damageInflictors.AsSpan()[index];
			if (!state.Restored) {
				float velocityBlend = 1.0F;
				float inflictorMass = state.InflictorPhysics!.GetMass();
				if (inflictorMass < VPHYSICS_LARGE_OBJECT_MASS && 0 == (state.InflictorPhysics.GetGameFlags() & PhysicsFlags.DamageSlice)) {
					float otherMass = state.OtherMassMax > 0 ? state.OtherMassMax : 1;
					float massRatio = inflictorMass / otherMass;
					massRatio = Math.Clamp(massRatio, 0.1f, 10.0f);
					if (massRatio < 1)
						velocityBlend = MathLib.RemapVal(massRatio, 0.1f, 1f, 0f, 0.5f);
					else
						velocityBlend = MathLib.RemapVal(massRatio, 1.0f, 10f, 0.5f, 1f);
				}
				RestoreDamageInflictorState(index, velocityBlend);
			}
		}
	}

	public void PreCollision(ref VCollisionEvent ev) {
		throw new NotImplementedException();
	}

	public int ShouldCollide(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1) {
		throw new NotImplementedException();
	}

	public bool ShouldFreezeContacts(Span<IPhysicsObject> objectList) {
		throw new NotImplementedException();
	}

	public bool ShouldFreezeObject(IPhysicsObject obj) {
		throw new NotImplementedException();
	}

	public int ShouldSolvePenetration(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1, double dt) {
		throw new NotImplementedException();
	}

	public void StartTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData touchData) {
		throw new NotImplementedException();
	}

	internal void AddRemoveObject(IServerNetworkable? remove) {
		if (remove != null && !removeObjects.Contains(remove))
			removeObjects.Add(remove);
	}
}
