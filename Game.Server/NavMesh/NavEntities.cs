using Source.Common;

namespace Game.Server.NavMesh;


class FuncNavCost
{

}

class FuncNavAvoid
{

}

class FuncNavPrefer
{

}

class FuncNavBlocker
{

}

public class FuncNavObstruction : BaseEntity, INavAvoidanceObstacle
{
	public static readonly SendTable DT_FuncNavObstruction = new([ // todo

	]);

	public bool Disabled;

	int DrawDebugTextOverlays() {
		throw new NotImplementedException();
	}

	void UpdateOnRemove() { }

	public override void Spawn() {
		SetMoveType(Source.MoveType.None);
		SetModel(GetModelName());
		AddEffects(Source.EntityEffects.NoDraw);
		// SetCollisionGroup(CollisionGroup.None);
		SetSolid(Source.SolidType.None);
		// AddSolidFlags(Source.SolidFlags.NotSolid);

		if (!Disabled) {
			ObstructNavAreas();
			NavMesh.Instance!.RegisterAvoidanceObstacle(this);
		}
	}

	// void InputEnable(inputdata_t &inputdata ) { }

	// void InputDisable(inputdata_t &inputdata ) { }

	void ObstructNavAreas() {
		Extent extent = default;
		extent.Init(this);
		// NavMesh.Instance!.ForAllAreasOverlappingExtent(this, extent); todo
	}

	bool Invoke(NavArea area) {
		area.MarkObstacleToAvoid(GetNavObstructionHeight());
		return true;
	}

	public bool IsPotentiallyAbleToObstructNavAreas() => true;
	public float GetNavObstructionHeight() => Nav.JumpCrouchHeight;
	public bool CanObstructNavAreas() => Disabled;
	public BaseEntity GetObstructingEntity() => this;
	public void OnNavMeshLoaded() {
		if (!DisableTouchFuncs)
			ObstructNavAreas();
	}
}