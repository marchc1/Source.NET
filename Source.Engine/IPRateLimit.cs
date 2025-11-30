using Source.Common.Commands;
using Source.Common.Networking;

namespace Source.Engine;

public class IPRateLimit(ConVar maxSec, ConVar maxWindow, ConVar maxSecGlobal)
{
	const int FLUSH_TIMEOUT = 120;
	readonly Dictionary<uint, IPRateLimit> IP = [];
	int globalCount;
	long lastTime;
	double flushTime;

	struct IPRate
	{
		public long LastTime;
		public int Count;
	}

	public bool CheckIP(NetAddress ip) {
		return true;
		// ^^^^^^^^^^^ TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO 
	}
}
