namespace Source.Common.Networking;

public interface INetChannelHandler
{
	void ConnectionStart(NetChannel channel);
	void ConnectionClosing(ReadOnlySpan<char> reason);
	void ConnectionCrashed(ReadOnlySpan<char> reason);
	void PacketStart(int incomingSequence, int outgoingAcknowledged);
	void PacketEnd();
	void FileRequested(ReadOnlySpan<char> fileName, uint transferID);
	void FileReceived(ReadOnlySpan<char> fileName, uint transferID);
	void FileDenied(ReadOnlySpan<char> fileName, uint transferID);
	void FileSent(ReadOnlySpan<char> fileName, uint transferID);

	bool ProcessMessage<T>(T message) where T : INetMessage;
}
