namespace Source.Common.Audio;

public interface IVoiceRecord
{
	// Start/stop capturing.
	bool RecordStart();
	void RecordStop();

	// Idle processing.
	void Idle();

	// Get the most recent N samples. If nSamplesWanted is less than the number of
	// available samples, it discards the first samples and gives you the last ones.
	int GetRecordedData(Span<short> samples);
}
