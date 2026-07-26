namespace Source.Common.Audio;

public enum VoiceCodecQuality
{
	/// <summary>
	/// Vocoder (mostly for comfortable noise).
	/// </summary>
	Noise = 0,
	/// <summary>
	/// Very noticeable artifacts/noise, good intelligibility.
	/// </summary>
	Lowest = 1,
	/// <summary>
	/// Artifacts/noise sometimes noticeable with / without headphones.
	/// </summary>
	Average = 2,
	/// <summary>
	/// Need good headphones to tell the difference or hard to tell even with good headphones.
	/// </summary>
	Good = 3,
	/// <summary>
	/// Completely transparent for voice, good quality music.
	/// </summary>
	Perfect = 4
}


public interface IVoiceCodec
{
	bool Init(VoiceCodecQuality quality);
	void Release();

	int Compress(ReadOnlySpan<byte> @in, Span<byte> @out, bool final);
	int Decompress(ReadOnlySpan<byte> @in, Span<byte> @out);
	bool ResetState();
}
