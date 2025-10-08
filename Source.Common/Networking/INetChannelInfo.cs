namespace Source.Common.Networking;

public interface INetChannelInfo
{
	public virtual string? GetName() => null;
	public virtual string? GetAddress() => null;
	public virtual TimeUnit_t GetTime() => 0;
	public virtual TimeUnit_t GetTimeConnected() => 0;
	public virtual int GetBufferSize() => 0;
	public virtual int GetDataRate() => 0;
	public virtual bool IsLoopback() => false;
	public virtual bool IsTimingOut() => false;
	public virtual float GetLatency(int flow) => 0;
	public virtual float GetAverageLatency(int flow) => 0;
	public virtual float GetAverageLoss(int flow) => 0;
	public virtual float GetAverageChoke(int flow) => 0;
	public virtual float GetAverageData(int flow) => 0;
	public virtual float GetAveragePackets(int flow) => 0;
	public virtual int GetTotalData(int flow) => 0;
	public virtual int GetSequenceNumber(int flow) => 0;
	public virtual bool IsValidPacket(int flow, int frameNumber) => false;
	public virtual TimeUnit_t GetPacketTime(int flow, int frameNumber) => 0;
	public virtual int GetPacketBytes(int flow, int frameNumber, NetChannelGroup group) => 0;
	public virtual bool GetStreamProgress(int flow, out int received, out int total) { received = 0; total = 0; return false; }
	public virtual TimeUnit_t GetTimeSinceLastReceived() => 0;
	public virtual TimeUnit_t GetCommandInterpolationAmount(int flow, int frameNumber) => 0;
	public virtual void GetPacketResponseLatency(int flow, int frameNumber, out int latencyMsecs, out int choke) { latencyMsecs = 0; choke = 0; return; }
	public virtual void GetRemoteFramerate(out TimeUnit_t frameTime, out TimeUnit_t frameTimeStdDeviation) { frameTimeStdDeviation = 0; frameTime = 0; }
	public virtual TimeUnit_t GetTimeoutSeconds() => 0;
}
