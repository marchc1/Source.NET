using Source.Common.Audio;

namespace Source.Engine;

public interface IWaveData
{
	AudioSource? Source();
	bool IsReadyToMix();
}

public interface IWaveStreamSource
{
	int UpdateLoopingSamplePosition(int samplePosition);
	void UpdateSamples(Span<char> pData, int sampleCount);
	int GetLoopingInfo(out int pLoopBlock, out int pNumLeadingSamples, out int pNumTrailingSamples);
}
