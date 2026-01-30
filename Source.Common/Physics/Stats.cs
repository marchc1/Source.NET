namespace Source.Common.Physics;

public struct PhysicsStats {
	public float MaxRescueSpeed;
	public float MaxSpeedGain;
	public int ImpactSysNum;
	public int ImpactCounter;
	public int ImpactSumSys;
	public int ImpactHardRescueCount;
	public int ImpactRescueAfterCount;
	public int ImpactDelayedCount;
	public int ImpactCollisionChecks;
	public int ImpactStaticCount;
	public double TotalEnergyDestroyed;
	public int CollisionPairsTotal;
	public int CollisionPairsCreated;
	public int CollisionPairsDestroyed;
	public int PotentialCollisionsObjectVsObject;
	public int PotentialCollisionsObjectVsWorld;
	public int FrictionEventsProcessed;
}
