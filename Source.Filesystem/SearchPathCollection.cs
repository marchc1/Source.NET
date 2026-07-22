// TODO: Logging calls when things go wrong, ie. try/catches


using Source.Common.Filesystem;

namespace Source.FileSystem;

public class SearchPathCollection : List<ISearchPath>
{
	/// <summary>
	/// Defines whether the search path ID is searchable when pathID == null in queries.
	/// </summary>
	public bool RequestOnly { get; set; } = false;

	public ISearchPath? At(int index) {
		if (index >= Count)
			return null;
		return this[index];
	}
}
