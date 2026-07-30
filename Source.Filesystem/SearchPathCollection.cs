// TODO: Logging calls when things go wrong, ie. try/catches


using Source.Common.Filesystem;

using System.Collections;

namespace Source.FileSystem;

public class SearchPathCollection 
{
	/// <summary>
	/// Defines whether the search path ID is searchable when pathID == null in queries.
	/// </summary>
	public bool RequestOnly { get; set; } = false;
	bool IsDirty = true;


	readonly List<ISearchPath> addOrder = [];
	readonly List<ISearchPath> sortOrder = [];

	public ISearchPath? AtAdded(int index) {
		if (index >= Count)
			return null;
		return addOrder[index];
	}

	public ISearchPath? AtSorted(int index) {
		if (index >= Count)
			return null;
		return sortOrder[index];
	}

	public int Count => addOrder.Count;

	public List<ISearchPath> GetAddOrder() => addOrder;
	public List<ISearchPath> GetSortOrder(){
		ValidateOrder();
		return sortOrder;
	}

	public void ValidateOrder() {
		if (!IsDirty) return;

		IsDirty = false;
		sortOrder.Clear();
		sortOrder.EnsureCapacity(addOrder.Count);
		for (PathGroupName i = 0; i < PathGroupName.Fallbacks + 1; i++) {
			foreach (var item in addOrder){
				if (item.GetGroupName() == i)
					sortOrder.Add(item);
			}
		}
	}

	public void InvalidateOrder() => IsDirty = true;
}
