
namespace Source.Common.Server;

/// <summary>
/// A server-side client.
/// </summary>
public interface IClient
{
	void Disconnect(ReadOnlySpan<char> reason);
}
