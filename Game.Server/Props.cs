using Game.Server;
using Game.Shared;

using Source.Common;


namespace Game.Server;
using FIELD_PP = Source.FIELD<PhysicsProp>;
using FIELD_DP = Source.FIELD<DynamicProp>;

public class BaseProp : BaseAnimating
{
	public void Spawn() { }
	public void Precache() { }
	public void Activate() { }
	public void KeyValue(ReadOnlySpan<char> name, ReadOnlySpan<char> value) { }
	public void CalculateBlockLOS() { }
	public void ParsePropData() { }
	public virtual new bool IsAlive() => false;
	public virtual bool OverridePropdata() => true;
}

public class BreakableProp : BaseProp
{
	public static readonly SendTable DT_BreakableProp = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass("BreakableProp", DT_BreakableProp).WithManualClassID(StaticClassIndices.CBreakableProp);
}

public class PhysicsProp : BreakableProp
{
	public static readonly SendTable DT_PhysicsProp = new(DT_BreakableProp, [
		SendPropBool(FIELD_PP.OF(nameof(Awake)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysicsProp", DT_PhysicsProp).WithManualClassID(StaticClassIndices.CPhysicsProp);
	public bool Awake;
}

public class DynamicProp : BreakableProp
{
	public static readonly SendTable DT_DynamicProp = new(DT_BreakableProp, [
		SendPropBool(FIELD_DP.OF(nameof(UseHitboxesForRenderBox)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("DynamicProp", DT_DynamicProp).WithManualClassID(StaticClassIndices.CDynamicProp);
	public bool UseHitboxesForRenderBox;
}

public class PhysicsPropMultiplayer : PhysicsProp
{
	public static readonly SendTable DT_PhysicsPropMultiplayer = new(DT_PhysicsProp, [

	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysicsPropMultiplayer", DT_PhysicsPropMultiplayer).WithManualClassID(StaticClassIndices.CPhysicsPropMultiplayer);
}
