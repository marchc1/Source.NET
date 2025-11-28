using Steamworks;

using System.Runtime.CompilerServices;

namespace Source.Common.Server;

public struct USERID
{
	public IDType IDType;
	public CSteamID SteamID;
}

class SteamIDRenderCtx {
	public char[][] rgchBuf;
	public SteamIDRenderCtx() {
		rgchBuf = new char[SteamIDExts.k_cBufs][];
		for (int i = 0; i < SteamIDExts.k_cBufs; i++) 
			rgchBuf[i] = new char[SteamIDExts.k_cBufLen];
	}
	public int buf;
	public Span<char> Alloc() {
		int b = buf;
		buf++;
		buf %= SteamIDExts.k_cBufs;
		return rgchBuf[buf];
	}
}
public static class SteamIDExts
{
	public const int k_cBufLen = 37;
	public const int k_cBufs = 8; 
	static ThreadLocal<SteamIDRenderCtx> rgchBuf = new(() => new());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<char> Render(this in CSteamID steamID) {
		Span<char> pchBuf = rgchBuf.Value!.Alloc();
		return Render(in steamID, pchBuf);
	}

	public static ReadOnlySpan<char> Render(this in CSteamID steamID, Span<char> buf) {
		uint universe = (uint)steamID.GetEUniverse();
		uint accountID = (uint)steamID.GetAccountID();
		uint accountInstance = steamID.GetUnAccountInstance();

		switch (steamID.GetEAccountType()) {
			case EAccountType.k_EAccountTypeAnonGameServer:
				sprintf(buf, "[A:%u:%u:%u]").U(universe).U(accountID).U(accountInstance);
				break;
			case EAccountType.k_EAccountTypeGameServer:
				sprintf(buf, "[G:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeMultiseat:
				sprintf(buf, "[M:%u:%u:%u]").U(universe).U(accountID).U(accountInstance);
				break;
			case EAccountType.k_EAccountTypePending:
				sprintf(buf, "[P:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeContentServer:
				sprintf(buf, "[C:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeClan:
				sprintf(buf, "[g:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeChat: {
					EChatSteamIDInstanceFlags accIFlags = (EChatSteamIDInstanceFlags)accountInstance;
					if ((accIFlags & EChatSteamIDInstanceFlags.k_EChatInstanceFlagClan) != 0)
						sprintf(buf, "[c:%u:%u]").U(universe).U(accountID);
					else if ((accIFlags & EChatSteamIDInstanceFlags.k_EChatInstanceFlagLobby) != 0)
						sprintf(buf, "[L:%u:%u]").U(universe).U(accountID);
					else
						sprintf(buf, "[T:%u:%u]").U(universe).U(accountID);
				}
				break;
			case EAccountType.k_EAccountTypeInvalid:
				sprintf(buf, "[I:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeIndividual:
				sprintf(buf, "[U:%u:%u]").U(universe).U(accountID);
				break;
			case EAccountType.k_EAccountTypeAnonUser:
				sprintf(buf, "[a:%u:%u]").U(universe).U(accountID);
				break;
			default:
				sprintf(buf, "[i:%u:%u]").U(universe).U(accountID);
				break;
		}
		
		return buf;
	}
}
