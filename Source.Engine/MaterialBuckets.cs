using Source.Common;

namespace Source.Engine;

public class MaterialsBuckets<Element_t> where Element_t : struct
{
	struct MaterialSortInfo_t
	{
		public int FlushCount;
		public int Head;
	}

	readonly List<int> UsedSortIDs = [];

	readonly List<MaterialSortInfo_t> MaterialSortInfoArray = [];

	readonly PooledLinkedList<Element_t> Elements = new();

	int FlushCount = -1;

	public void SetNumMaterialSortIDs(int n) {
		MaterialSortInfoArray.Clear();
		for (int i = 0; i < n; i++)
			MaterialSortInfoArray.Add(new MaterialSortInfo_t { FlushCount = -1, Head = PooledLinkedList<Element_t>.INVALID_INDEX });
		Elements.Clear();

		UsedSortIDs.Clear();
	}

	public void Flush() {
		FlushCount++;
		Elements.Clear();
		UsedSortIDs.Clear();
	}

	public int GetFirstUsedSortID() => UsedSortIDs.Count > 0 ? 0 : InvalidSortIDHandle();

	public int GetNextUsedSortID(int prevSortID) => prevSortID + 1 < UsedSortIDs.Count ? prevSortID + 1 : InvalidSortIDHandle();

	public int GetSortID(int handle) => UsedSortIDs[handle];

	public int InvalidSortIDHandle() => -1;

	public int GetElementListHead(int sortID) => MaterialSortInfoArray[sortID].Head;

	public int GetElementListNext(int h) => Elements.Next(h);

	public Element_t GetElement(int h) => Elements[h];

	public int InvalidElementHandle() => PooledLinkedList<Element_t>.INVALID_INDEX;

	public void AddElement(short sortID, Element_t elem) {
		int elemID = Elements.Alloc();
		Elements[elemID] = elem;

		MaterialSortInfo_t sortInfo = MaterialSortInfoArray[sortID];
		if (sortInfo.FlushCount != FlushCount) {
			sortInfo.FlushCount = FlushCount;

			UsedSortIDs.Add(sortID);

			sortInfo.Head = elemID;
		}
		else {
			Elements.LinkBefore(sortInfo.Head, elemID);
			sortInfo.Head = elemID;
		}
		MaterialSortInfoArray[sortID] = sortInfo;
	}
}
