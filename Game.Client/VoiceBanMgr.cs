using Source.Common.Filesystem;

using static Source.Constants;

namespace Game.Client;

// This class manages the (persistent) list of squelched players.
public class VoiceBanMgr
{
	const int BANMGR_FILEVERSION = 1;
	const string g_pBanMgrFilename = "voice_ban.dt";

	protected class BannedPlayer
	{
		public readonly byte[] PlayerID = new byte[SIGNED_GUID_LEN];
		public BannedPlayer Prev = null!, Next = null!;
	}

	protected readonly BannedPlayer[] PlayerHash = new BannedPlayer[256];

	// Hash a player ID to a byte.
	static byte HashPlayerID(ReadOnlySpan<byte> playerID) {
		byte curHash = 0;

		for (int i = 0; i < SIGNED_GUID_LEN; i++)
			curHash = (byte)(curHash + playerID[i]);

		return curHash;
	}

	public VoiceBanMgr() {
		for (int i = 0; i < 256; i++)
			PlayerHash[i] = new();
		Clear();
	}

	// Init loads the list of squelched players from disk.
	public bool Init(ReadOnlySpan<char> gameDir) {
		Term();

		// Load in the squelch file.
		using IFileHandle? fh = filesystem.Open(g_pBanMgrFilename, FileOpenOptions.Read | FileOpenOptions.Binary);
		if (fh != null) {
			Span<byte> versionBytes = stackalloc byte[sizeof(int)];
			fh.Stream.ReadAtLeast(versionBytes, sizeof(int), false);
			int version = BitConverter.ToInt32(versionBytes);
			if (version == BANMGR_FILEVERSION && fh.Stream.Length > 4) {
				int nIDs = ((int)fh.Stream.Length - sizeof(int)) / SIGNED_GUID_LEN;

				Span<byte> playerID = stackalloc byte[SIGNED_GUID_LEN];
				for (int i = 0; i < nIDs; i++) {
					fh.Stream.ReadAtLeast(playerID, SIGNED_GUID_LEN, false);
					AddBannedPlayer(playerID);
				}
			}
		}

		return true;
	}

	public void Term() {
		Clear();
	}

	// Saves the state into voice_squelch.dt.
	public void SaveState(ReadOnlySpan<char> gameDir) {
		// Save the file out.
		using IFileHandle? fh = filesystem.Open(g_pBanMgrFilename, FileOpenOptions.Write | FileOpenOptions.Binary);
		if (fh != null) {
			int version = BANMGR_FILEVERSION;
			Span<byte> versionBytes = stackalloc byte[sizeof(int)];
			BitConverter.TryWriteBytes(versionBytes, version);
			fh.Stream.Write(versionBytes);

			for (int i = 0; i < 256; i++) {
				BannedPlayer listHead = PlayerHash[i];
				for (BannedPlayer cur = listHead.Next; cur != listHead; cur = cur.Next)
					fh.Stream.Write(cur.PlayerID, 0, SIGNED_GUID_LEN);
			}
		}
	}

	public bool GetPlayerBan(ReadOnlySpan<byte> playerID) {
		return InternalFindPlayerSquelch(playerID) != null;
	}

	public void SetPlayerBan(ReadOnlySpan<byte> playerID, bool squelch) {
		if (squelch) {
			// Is this guy already squelched?
			if (GetPlayerBan(playerID))
				return;

			AddBannedPlayer(playerID);
		}
		else {
			BannedPlayer? player = InternalFindPlayerSquelch(playerID);
			if (player != null) {
				player.Prev.Next = player.Next;
				player.Next.Prev = player.Prev;
			}
		}
	}

	protected void Clear() {
		// Tie off the hash table entries.
		for (int i = 0; i < 256; i++)
			PlayerHash[i].Next = PlayerHash[i].Prev = PlayerHash[i];
	}

	protected BannedPlayer? InternalFindPlayerSquelch(ReadOnlySpan<byte> playerID) {
		int index = HashPlayerID(playerID);
		BannedPlayer listHead = PlayerHash[index];
		for (BannedPlayer cur = listHead.Next; cur != listHead; cur = cur.Next) {
			if (playerID[..SIGNED_GUID_LEN].SequenceEqual(cur.PlayerID))
				return cur;
		}

		return null;
	}

	protected BannedPlayer? AddBannedPlayer(ReadOnlySpan<byte> playerID) {
		BannedPlayer newPlayer = new();

		int index = HashPlayerID(playerID);
		playerID[..SIGNED_GUID_LEN].CopyTo(newPlayer.PlayerID);
		newPlayer.Next = PlayerHash[index];
		newPlayer.Prev = PlayerHash[index].Prev;
		newPlayer.Prev.Next = newPlayer.Next.Prev = newPlayer;
		return newPlayer;
	}
}
