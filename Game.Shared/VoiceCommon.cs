using static Source.Constants;

namespace Game.Shared;

public struct PlayerBitVec : IEquatable<PlayerBitVec>
{
	PlayerBitVecDWords DWords;

	[System.Runtime.CompilerServices.InlineArray(VOICE_MAX_PLAYERS_DW)]
	struct PlayerBitVecDWords { uint dw; }

	public bool this[int bit] {
		readonly get => (DWords[bit >> 5] & (1u << (bit & 31))) != 0;
		set {
			if (value)
				DWords[bit >> 5] |= 1u << (bit & 31);
			else
				DWords[bit >> 5] &= ~(1u << (bit & 31));
		}
	}

	public void Init(int val = 0) {
		for (int i = 0; i < VOICE_MAX_PLAYERS_DW; i++)
			DWords[i] = val != 0 ? uint.MaxValue : 0;
	}

	public readonly uint GetDWord(int i) => DWords[i];
	public void SetDWord(int i, uint val) => DWords[i] = val;

	public readonly bool Equals(PlayerBitVec other) {
		for (int i = 0; i < VOICE_MAX_PLAYERS_DW; i++)
			if (DWords[i] != other.DWords[i])
				return false;
		return true;
	}
	public override readonly bool Equals(object? obj) => obj is PlayerBitVec other && Equals(other);
	public override readonly int GetHashCode() {
		HashCode hash = new();
		for (int i = 0; i < VOICE_MAX_PLAYERS_DW; i++)
			hash.Add(DWords[i]);
		return hash.ToHashCode();
	}
	public static bool operator ==(PlayerBitVec a, PlayerBitVec b) => a.Equals(b);
	public static bool operator !=(PlayerBitVec a, PlayerBitVec b) => !a.Equals(b);
}

public static class VoiceCommon
{
	public const int VOICE_DEFAULT_PROXIMITY_RANGE = 1200; //100 feet
}
