using Source.Common.DataCache;

namespace Source.DataCache;

public class DataCache : IDataCache
{
	public nuint Flush(bool unlockedOnly = true, bool notify = true) {
		throw new NotImplementedException();
	}

	public nuint Purge(nuint bytes) {
		throw new NotImplementedException();
	}
}
