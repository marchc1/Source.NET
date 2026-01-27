using Source.Common.Bitbuffers;

namespace Source.Common.Networking;

public interface INetChannel : INetChannelInfo
{
	void SetDataRate(float rate);
	bool RegisterMessage(INetMessage msg);
	bool StartStreaming(uint challengeNr);
	void ResetStreaming();
	void SetTimeout(TimeUnit_t seconds);
	void SetChallengeNr(uint chnr);

	void Reset();
	void Clear();
	void Shutdown(ReadOnlySpan<char> reason);

	void ProcessPlayback();
	bool ProcessStream();
	void ProcessPacket(NetPacket packet, bool hasHeader);

	bool SendNetMsg(INetMessage msg, bool forceReliable = false, bool voice = false);
	bool SendData(bf_write msg, bool reliable = true);
	bool SendFile(ReadOnlySpan<char> filename, uint transferID);
	void DenyFile(ReadOnlySpan<char> filename, uint transferID);
	void SetChoked();
	int SendDatagram(bf_write data);
	bool Transmit(bool onlyReliable = false);

	NetAddress? GetRemoteAddress();
	INetChannelHandler? GetMsgHandler();
	int GetDropNumber();
	NetSocketType GetSocket();
	uint GetChallengeNr();
	void GetSequenceData(out int outSequenceNr, out int inSequenceNr, out int outSequenceNrAck);
	void SetSequenceData(int outSequenceNr, int inSequenceNr, int outSequenceNrAck);

	void UpdateMessageStats(NetChannelGroup msggroup, int bits);
	bool CanPacket();
	bool IsOverflowed();
	bool IsTimedOut();
	bool HasPendingReliableData();
	void SetFileTransmissionMode(bool backgroundMode);
	void SetCompressionMode(bool useCompression);
	uint RequestFile(ReadOnlySpan<char> filename);
	TimeUnit_t GetTimeSinceLastReceived();
	void SetMaxBufferSize(bool reliable, int bytes, bool voice = false);
	bool IsNull();
	int GetNumBitsWritten(bool reliable);
	void SetInterpolationAmount(TimeUnit_t interpolationAmount);
	void SetRemoteFramerate(TimeUnit_t frameTime, TimeUnit_t frameTimeStdDeviation);
	void SetMaxRoutablePayloadSize(int splitSize);
	int GetMaxRoutablePayloadSize();
	int GetProtocolVersion();
}
