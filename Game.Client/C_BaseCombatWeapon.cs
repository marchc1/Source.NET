namespace Game.Client;

public partial class C_BaseCombatWeapon : C_BaseAnimating
{
	public override bool IsBaseCombatWeapon() => true;
	public static C_BaseCombatWeapon? GetActiveWeapon() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		return player?.GetActiveWeapon();
	}
	public bool IsCarriedByLocalPlayer() {
		C_BaseEntity? owner = GetOwner();
		if (owner == null)
			return false;
		return owner == C_BasePlayer.GetLocalPlayer();
	}
	public bool ShouldDrawUsingViewModel() => IsCarriedByLocalPlayer() && !C_BasePlayer.ShouldDrawLocalPlayer();
}
