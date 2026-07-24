using Source.Common.GarrysMod;

using System.Numerics;

namespace Source.Common.GarrysMod;

public enum GModChannelFFT
{
	FFT_256 = 0,
	FFT_512 = 1,
	FFT_1024 = 2,
	FFT_2048 = 3,
	FFT_4096 = 4,
	FFT_8192 = 5,
	FFT_16384 = 6,
	FFT_32768 = 7,
}

public interface IGModAudioChannel
{
	void Destroy();
	void Stop();
	void Pause();
	void Play();
	void SetVolume(float unk1);
	float GetVolume();
	void SetPlaybackRate(float unk1);
	float GetPlaybackRate();
	void SetPos(in Vector3 unk1, in Vector3 unk2, in Vector3 unk3);
	void GetPos(out Vector3 unk1, out Vector3 unk2, out Vector3 unk3);
	void SetTime(TimeUnit_t time, bool unk2);
	TimeUnit_t GetTime();
#if WIN32
	TimeUnit_t GetBufferedTime();
#endif
	void Set3DFadeDistance(float unk1, float unk2);
	void Get3DFadeDistance(out float unk1, out float unk2);
	void Set3DCone(int unk1, int unk2, float unk3);
	void Get3DCone(out int unk1, out int unk2, out float unk3);
	int GetState();
	void SetLooping(bool looping);
	bool IsLooping();
	bool IsOnline();
	bool Is3D();
	bool IsBlockStreamed();
	bool IsValid();
	double GetLength();
	ReadOnlySpan<char> GetFileName();
	int GetSamplingRate();
	int GetBitsPerSample();
	float GetAverageBitRate();
	void GetLevel(out float unk1, out float unk2);
	void FFT(ref float unk1, GModChannelFFT unk2);
	void SetChannelPan(float unk1);
	float GetChannelPan();
	ReadOnlySpan<char> GetTags(int unk1);
	void Set3DEnabled(bool unk1);
	bool Get3DEnabled();
	void Restart();
}

public interface IBassAudioStream
{
	uint Decode(ReadOnlySpan<byte> data);
	int GetOutputBits();
	int GetOutputRate();
	int GetOutputChannels();
	uint GetPosition();
	void SetPosition(uint distance);
	uint GetHandle(); // unsigned long -> DWORD -> HSTREAM
	void MyFileCloseProc(object? unk1);
	ulong MyFileLenProc(object? unk1); // unsigned long long -> QWORD
	uint MyFileReadProc(object? unk1, uint unk2, object? unk3); // unsigned long -> DWORD
	bool MyFileSeekProc(ulong unk1, object? unk2);
}

public interface IGMod_Audio
{
	bool Init(IServiceProvider services);
	void Shutdown();
	void Update(uint unk1);
	IBassAudioStream? CreateAudioStream(IAudioStreamEvent? unk1);
	void SetEar(ref Vector3 unk1, ref Vector3 unk2, ref Vector3 unk3, ref Vector3 unk4);
	IGModAudioChannel? PlayURL(ReadOnlySpan<char> url, ReadOnlySpan<char> flags, Span<int> unk1);
	IGModAudioChannel? PlayFile(ReadOnlySpan<char> path, ReadOnlySpan<char> flags, Span<int> unk1);
	void SetGlobalVolume(float volume);
	void StopAllPlayback();
	ReadOnlySpan<char> GetErrorString(int unk1);
}
