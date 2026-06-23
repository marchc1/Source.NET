using Source.Common.Engine;
using Source.Common.Input;

namespace Source.Common;

public interface IGameUIFuncs
{
	bool IsKeyDown(ReadOnlySpan<char> keyName, out bool isDown);
	ReadOnlySpan<char> GetBindingForButtonCode(ButtonCode code);
	ButtonCode GetButtonCodeForBind(ReadOnlySpan<char> bind);
	void SetFriendsID(uint friendsID, ReadOnlySpan<char> friendsName);
	void GetDesktopResolution(out int width, out int height);
	bool IsConnectedToVACSecureServer();
}
