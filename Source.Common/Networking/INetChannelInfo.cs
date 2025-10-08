namespace Source.Common.Networking;

public interface INetChannelInfo
{
	ReadOnlySpan<char> GetName();
	ReadOnlySpan<char> GetAddress();
	TimeUnit_t GetTime();
	TimeUnit_t GetTimeConnected();
	int GetBufferSize();
	int GetDataRate();
	bool IsLoopback();
	bool IsTimingOut();
	TimeUnit_t GetLatency(int flow);
	TimeUnit_t GetAverageLatency(int flow);
	TimeUnit_t GetAverageLoss(int flow);
	TimeUnit_t GetAverageChoke(int flow);
	TimeUnit_t GetAverageData(int flow);
	TimeUnit_t GetAveragePackets(int flow);
	int GetTotalData(int flow);
	int GetSequenceNumber(int flow);
	bool IsValidPacket(int flow, int frameNumber) => false;
	TimeUnit_t GetPacketTime(int flow, int frameNumber);
	int GetPacketBytes(int flow, int frameNumber, NetChannelGroup group);
	bool GetStreamProgress(int flow, out int received, out int total) { received = 0; total = 0; return false; }
	TimeUnit_t GetTimeSinceLastReceived();
	TimeUnit_t GetCommandInterpolationAmount(int flow, int frameNumber);
	void GetPacketResponseLatency(int flow, int frameNumber, out int latencyMsecs, out int choke) { latencyMsecs = 0; choke = 0; return; }
	void GetRemoteFramerate(out TimeUnit_t frameTime, out TimeUnit_t frameTimeStdDeviation) { frameTimeStdDeviation = 0; frameTime = 0; }
	TimeUnit_t GetTimeoutSeconds();
}
