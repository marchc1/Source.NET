using Source.Common;
using Source.Common.Engine;
using Source.Common.Input;

namespace Source.Engine;

public class GameUIFuncs(Key Key, IInputSystem inputsystem, IGame game) : IGameUIFuncs
{
	public ReadOnlySpan<char> GetBindingForButtonCode(ButtonCode code) => Key.BindingForKey(code);

	public ButtonCode GetButtonCodeForBind(ReadOnlySpan<char> bind) {
		ReadOnlySpan<char> keyName = Key.NameForBinding(bind);
		if (keyName.IsStringEmpty)
			return ButtonCode.KeyNone;
		return inputsystem.StringToButtonCode(keyName);
	}

	public void GetDesktopResolution(out int width, out int height) {
		game.GetDesktopInfo(out uint uwidth, out uint uheight, out _);
		width = (int)uwidth;
		height = (int)uheight;
	}

	public bool IsConnectedToVACSecureServer() {
		if (cl.IsConnected())
			return false; // todo
		return false;
	}

	public bool IsKeyDown(ReadOnlySpan<char> keyName, out bool isDown) {
		isDown = false;
		if (g_ClientDLL == null)
			return false;

		return g_ClientDLL.IN_IsKeyDown(keyName, out isDown);
	}

	public void SetFriendsID(uint friendsID, ReadOnlySpan<char> friendsName) {
		cl.SetFriendsID(friendsID, friendsName);
	}
}
