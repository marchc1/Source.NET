using Source.Common.Bitbuffers;
using Source.Common.Input;

namespace Source.Common.Client;

public interface IInput
{
	public void CreateMove(int sequenceNumber, double inputSampleFrametime, bool active);
	public bool WriteUsercmdDeltaToBuffer(bf_write buf, int from, int to, bool isNewCommand);
	public void EncodeUserCmdToBuffer(bf_write buf, int slot);
	public void DecodeUserCmdFromBuffer(bf_read buf, int slot);
	public void MakeWeaponSelection(BaseCombatWeapon? weapon);
	void Init();
	int KeyEvent(int eventcode, ButtonCode keynum, ReadOnlySpan<char> currentBinding);
	void ExtraMouseSample(double frametime, bool active);
	void ActivateMouse();
	void DeactivateMouse();
	void ClearStates();
	void CAM_Think();
}