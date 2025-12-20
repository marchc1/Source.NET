using Game.Client.HUD;
using Game.Shared;

using Source.Common.Commands;

using System.Net;

namespace Game.Client;

[DeclareHudElement(Name = "CBaseHudWeaponSelection")]
public class BaseHudWeaponSelection : EditableHudElement
{
	public static ConVar hud_drawhistory_time = new("hud_drawhistory_time", "5", 0);
	public static ConVar hud_fastswitch = new("hud_fastswitch", "0", FCvar.Archive);
	public double SelectionTime;
	static BaseHudWeaponSelection? Instance;
	public bool SelectionVisible;
	public BaseCombatWeapon? SelectedWeapon;

	public BaseHudWeaponSelection(string elementName) : base(null, elementName) {
		Instance = this;
		((IHudElement)this).SetHiddenBits(HideHudBits.WeaponSelection | HideHudBits.NeedSuit | HideHudBits.PlayerDead | HideHudBits.InVehicle);
	}

	public override void Init() {
		Reset();
		// weapons resource todo
		SelectionTime = gpGlobals.CurTime;
	}

	void Reset() {
		// gwr.Reset();
		SelectionVisible = false;
		SelectionTime = gpGlobals.CurTime;
		// UnlockRenderGroup todo
	}

	void UpdateSelectionTime() => SelectionTime = gpGlobals.CurTime;
	BaseHudWeaponSelection GetInstance() => Instance;
	BaseHudWeaponSelection GetHudWeaponSelection() => GetInstance();


	void VidInit() { }

	public override void OnThink() { }

	void ProcessInput() { }

	public bool IsInSelectionMode() => SelectionVisible;

	void OpenSelection() { }

	void HideSelection() { }

	// bool CanBeSelectedInHUD(BaseCombatWeapon pWeapon) { }

	// int KeyInput(int down, ButtonCode keynum, ReadOnlySpan<char> currentBinding) { }

	void OnWeaponPickup(BaseCombatWeapon pWeapon) { }

	private void UserCmd_Slot(int slot) {
		int fastSwitchMode = hud_fastswitch.GetInt();
		if (3 == fastSwitchMode) //HUDTYPE_CAROUSEL
			UserCmd_LastWeapon();
		else
			Instance?.SelectSlot(slot);
	}
	[ConCommand("slot1", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot1() => Instance?.SelectSlot(1);
	[ConCommand("slot2", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot2() => Instance?.SelectSlot(2);
	[ConCommand("slot3", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot3() => Instance?.SelectSlot(3);
	[ConCommand("slot4", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot4() => Instance?.SelectSlot(4);
	[ConCommand("slot5", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot5() => Instance?.SelectSlot(5);
	[ConCommand("slot6", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot6() => Instance?.SelectSlot(6);
	[ConCommand("slot7", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot7() => Instance?.SelectSlot(7);
	[ConCommand("slot8", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot8() => Instance?.SelectSlot(8);
	[ConCommand("slot9", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot9() => Instance?.SelectSlot(9);
	[ConCommand("slot0", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot0() => Instance?.SelectSlot(0);
	[ConCommand("slot10", flags: FCvar.ServerCanExecute)]
	static void UserCmd_Slot10() => Instance?.SelectSlot(10);

	// bool IsHudMenuTakingInput() { }

	// bool HandleHudMenuInput(int slot) { }

	// bool IsHudMenuPreventingWeaponSelection() { }

	void SelectSlot(int slot) { }

	void UserCmd_Close() { }

	[ConCommand("invnext", flags: FCvar.ServerCanExecute)]
	static void UserCmd_NextWeapon() { }

	[ConCommand("invprev", flags: FCvar.ServerCanExecute)]
	static void UserCmd_PrevWeapon() { }

	[ConCommand("lastinv", flags: FCvar.ServerCanExecute)]
	static void UserCmd_LastWeapon() { }

	void SwitchToLastWeapon() { }

	void SetWeaponSelected() { }

	void SelectWeapon() { }

	void CancelWeaponSelection() { }

	// BaseCombatWeapon GetFirstPos(int slot) { }

	// BaseCombatWeapon GetNextActivePos(int slot, int slotPos) { }
}