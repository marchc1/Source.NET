using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.Bitbuffers;
using Source.Common.GUI;
using Source.GUI.Controls;
using System.Numerics;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudHealth")]
public class HudHealth : HudNumericDisplay, IHudElement
{
	int Health;
	int BitsDamage;

	public HudHealth(string? panelName) : base(null, "HudHealth") {
		var parent = clientMode.GetViewport();
		SetParent(parent);
	}

	public void Init() {
		IHudElement.HookMessage("Damage", Damage);
		Reset();
	}

	public void VidReset() {
		Reset();
	}
	public void Reset() {

	}

	private void Damage(bf_read msg) {
		int armor = msg.ReadByte(); 
		int damageTaken = msg.ReadByte();   
		long bitsDamage = msg.ReadLong();

		Vector3 vecFrom = new() {
			X = msg.ReadBitCoord(),
			Y = msg.ReadBitCoord(),
			Z = msg.ReadBitCoord()
		};

		if (damageTaken > 0 || armor > 0) 
			if (damageTaken > 0)
				clientMode.GetViewportAnimationController()!.StartAnimationSequence("HealthDamageTaken");
	}
}
