namespace Source.Common;

public class BidirectionalSet<BucketHandle, ElementHandle>
{
	public delegate ref uint FirstElementFunc(BucketHandle bucket);
	public delegate ref uint FirstBucketFunc(ElementHandle element);

	public const int InvalidIndex = PooledLinkedList<BucketListInfo>.INVALID_INDEX;
	const uint InvalidHead = unchecked((uint)InvalidIndex);

	public struct BucketListInfo
	{
		public ElementHandle Element;
		public int BucketListIndex;
	}

	public struct ElementListInfo
	{
		public BucketHandle Bucket;
		public int ElementListIndex;
	}

	readonly PooledLinkedList<BucketListInfo> ElementsInBucket = new();
	readonly PooledLinkedList<ElementListInfo> BucketsUsedByElement = new();

	FirstBucketFunc FirstBucket = null!;
	FirstElementFunc FirstElement = null!;

	public void Init(FirstElementFunc elemFunc, FirstBucketFunc bucketFunc) {
		FirstBucket = bucketFunc;
		FirstElement = elemFunc;
	}

	public void AddElementToBucket(BucketHandle bucket, ElementHandle element) {
		Assert(FirstBucket != null && FirstElement != null);

		int idx = ElementsInBucket.Alloc();
		int list = BucketsUsedByElement.Alloc();

		ElementsInBucket[idx].Element = element;
		ElementsInBucket[idx].BucketListIndex = list;

		BucketsUsedByElement[list].Bucket = bucket;
		BucketsUsedByElement[list].ElementListIndex = idx;

		ref uint firstElementInBucket = ref FirstElement(bucket);
		if (firstElementInBucket != InvalidHead)
			ElementsInBucket.LinkBefore((int)firstElementInBucket, idx);
		firstElementInBucket = (uint)idx;

		ref uint firstBucketInElement = ref FirstBucket(element);
		if (firstBucketInElement != InvalidHead)
			BucketsUsedByElement.LinkBefore((int)firstBucketInElement, list);
		firstBucketInElement = (uint)list;
	}

	public void RemoveElement(ElementHandle element) {
		Assert(FirstBucket != null && FirstElement != null);

		int i = (int)FirstBucket(element);
		while (i != InvalidIndex) {
			BucketHandle bucket = BucketsUsedByElement[i].Bucket;
			int elementListIndex = BucketsUsedByElement[i].ElementListIndex;

			ref uint firstElementInBucket = ref FirstElement(bucket);
			if (elementListIndex == (int)firstElementInBucket)
				firstElementInBucket = (uint)ElementsInBucket.Next(elementListIndex);
			ElementsInBucket.Remove(elementListIndex);

			int prevNode = i;
			i = BucketsUsedByElement.Next(i);
			BucketsUsedByElement.Remove(prevNode);
		}

		FirstBucket(element) = InvalidHead;
	}

	public void RemoveBucket(BucketHandle bucket) {
		int i = (int)FirstElement(bucket);
		while (i != InvalidIndex) {
			ElementHandle element = ElementsInBucket[i].Element;
			int bucketListIndex = ElementsInBucket[i].BucketListIndex;

			ref uint firstBucketInElement = ref FirstBucket(element);
			if (bucketListIndex == (int)firstBucketInElement)
				firstBucketInElement = (uint)BucketsUsedByElement.Next(bucketListIndex);
			BucketsUsedByElement.Remove(bucketListIndex);

			int prevNode = i;
			i = ElementsInBucket.Next(i);
			ElementsInBucket.Remove(prevNode);
		}

		FirstElement(bucket) = InvalidHead;
	}

	public int FirstElementInBucket(BucketHandle bucket) => (int)FirstElement(bucket);
	public int NextElement(int idx) => ElementsInBucket.Next(idx);
	public ElementHandle Element(int idx) => ElementsInBucket[idx].Element;
}
