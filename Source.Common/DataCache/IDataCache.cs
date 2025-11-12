namespace Source.Common.DataCache;

public interface IDataCache
{
	nuint Purge(nuint bytes);
	nuint Flush(bool unlockedOnly = true, bool notify = true);
}
