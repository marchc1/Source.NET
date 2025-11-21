using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Common.Client;

/// <summary>
/// Engine player info. (replica of player_info_s)
/// </summary>
public struct PlayerInfo
{
	public InlineArray32<char> Name;
	public int UserID;
	public InlineArray33<byte> GUID;
	public uint FriendsID;
	public InlineArray32<char> FriendsName;
	public bool FakePlayer;
	public bool IsHLTV;
	public bool IsReplay;
	public InlineArray4<CRC32_t> CustomFiles;
	public byte FilesDownloaded;

	public static void FromBytes(ReadOnlySpan<byte> bytes, out PlayerInfo info) {
		info = new();

		ReadOnlySpan<byte> asciiName = bytes[0..32];
		int asciiNull = asciiName.IndexOf<byte>(0);
		if (asciiNull != -1)
			asciiName = asciiName[..asciiNull];
		Encoding.ASCII.GetChars(asciiName, info.Name);

		info.UserID = MemoryMarshal.Cast<byte, int>(bytes[32..36])[0];
		bytes[36..69].CopyTo(info.GUID);
		info.FriendsID = MemoryMarshal.Cast<byte, uint>(bytes[72..76])[0];

		ReadOnlySpan<byte> friendsName = bytes[76..108];
		int friendsNull = friendsName.IndexOf<byte>(0);
		if (friendsNull != -1)
			friendsName = friendsName[..];
		Encoding.ASCII.GetChars(friendsName, info.FriendsName);

		info.FakePlayer = bytes[108] != 0;
		info.IsHLTV = bytes[109] != 0;
		info.IsReplay = bytes[110] != 0;

		MemoryMarshal.Cast<byte, CRC32_t>(bytes[112..(112+16)]).CopyTo(info.CustomFiles);
		info.FilesDownloaded = bytes[128];
	}
}

public struct AudioState {
	public Vector3 Origin;
	public QAngle Angles;
	public bool IsUnderwater;
}
public enum SkyboxVisibility
{
	NotVisible,
	Skybox3D,
	Skybox2D
}

public enum ClientFrameStage
{
	Undefined = -1,
	Start,
	NetUpdateStart,
	NetUpdatePostDataUpdateStart,
	NetUpdatePostDataUpdateEnd,
	NetUpdateEnd,
	RenderStart,
	RenderEnd
}

/// <summary>
/// Interface the engine exposes to the client DLL
/// </summary>
public interface IEngineClient
{
	ReadOnlySpan<char> Key_LookupBinding(ReadOnlySpan<char> binding);
	void GetMainMenuBackgroundName(Span<char> dest);
	int GetMaxClients();
	ReadOnlySpan<char> GetLevelName();
	void ClientCmd_Unrestricted(ReadOnlySpan<char> cmdString);
	void ExecuteClientCmd(ReadOnlySpan<char> v);
	bool IsLevelMainMenuBackground();
	bool IsPaused();
	bool IsDrawingLoadingImage();
	bool GetPlayerInfo(int playerIndex, out PlayerInfo playerInfo);
	bool Con_IsVisible();
	void GetViewAngles(out QAngle viewangles);
	void SetViewAngles(in QAngle viewangles);
	void GetScreenSize(out int w, out int h);
	void GetMouseDelta(out int dx, out int dy);
	bool IsConnected();
	bool IsInGame();
	int GetLocalPlayer();
	double GetLastTimeStamp();
	uint GetProtocolVersion();
	SkyboxVisibility IsSkyboxVisibleFromPoint(in Vector3 origin);
	float GetScreenAspectRatio();
	bool IsPlayingDemo();
	bool IsPlayingTimeDemo();
	INetChannelInfo? GetNetChannelInfo();
	void FireEvents();
	Model? LoadModel(ReadOnlySpan<char> name, bool prop);
}
