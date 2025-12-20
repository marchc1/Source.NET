using Game.Client.HUD;
using Game.Shared;

[DeclareHudElement(Name = "CBaseHudWeaponSelection")]
public class BaseHudWeaponSelection : EditableHudElement
{
	double SelectionTime;
	BaseHudWeaponSelection Instance;
	bool SelectionVisible;
	BaseCombatWeapon? SelectedWeapon;

	public BaseHudWeaponSelection(string elementName) : base(null, elementName) {
		Instance = this;
		((IHudElement)this).SetHiddenBits(HideHudBits.WeaponSelection | HideHudBits.NeedSuit | HideHudBits.PlayerDead | HideHudBits.InVehicle);
	}

	public override void Init() {
		// Reset();
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

	// bool IsInSelectionMode() { }

	void OpenSelection() { }

	void HideSelection() { }

	// bool CanBeSelectedInHUD(BaseCombatWeapon pWeapon) { }

	// int KeyInput(int down, ButtonCode keynum, ReadOnlySpan<char> currentBinding) { }

	void OnWeaponPickup(BaseCombatWeapon pWeapon) { }

	void UserCmd_Slot1() { }

	void UserCmd_Slot2() { }

	void UserCmd_Slot3() { }

	void UserCmd_Slot4() { }

	void UserCmd_Slot5() { }

	void UserCmd_Slot6() { }

	void UserCmd_Slot7() { }

	void UserCmd_Slot8() { }

	void UserCmd_Slot9() { }

	void UserCmd_Slot0() { }

	void UserCmd_Slot10() { }

	// bool IsHudMenuTakingInput() { }

	// bool HandleHudMenuInput(int slot) { }

	// bool IsHudMenuPreventingWeaponSelection() { }

	void SelectSlot(int slot) { }

	void UserCmd_Close() { }

	void UserCmd_NextWeapon() { }

	void UserCmd_PrevWeapon() { }

	void UserCmd_LastWeapon() { }

	void SwitchToLastWeapon() { }

	void SetWeaponSelected() { }

	void SelectWeapon() { }

	void CancelWeaponSelection() { }

	// BaseCombatWeapon GetFirstPos(int slot) { }

	// BaseCombatWeapon GetNextActivePos(int slot, int slotPos) { }
}