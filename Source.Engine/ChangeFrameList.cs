namespace Source.Engine;

class ChangeFrameList : IChangeFrameList
{
	readonly List<int> ChangeTicks = [];

	public void Init(int properties, int curTick) {
		ChangeTicks.Clear();
		for (int i = 0; i < properties; i++)
			ChangeTicks.Add(curTick);
	}

	public void Release() { }

	public IChangeFrameList Copy() {
		ChangeFrameList ret = new();
		int numProps = ChangeTicks.Count;
		ret.Init(numProps, 0);
		for (int i = 0; i < numProps; i++)
			ret.ChangeTicks[i] = ChangeTicks[i];
		return ret;
	}

	public int GetNumProps() => ChangeTicks.Count;

	public void SetChangeTick(ReadOnlySpan<int> propIndices, int iPropIndices, int tick) {
		for (int i = 0; i < iPropIndices; i++)
			ChangeTicks[propIndices[i]] = tick;
	}

	public int GetPropsChangedAfterTick(int tick, Span<int> iOutProps, int maxOutProps) {
		int outProps = 0;
		int count = ChangeTicks.Count;

		Assert(count <= maxOutProps);

		for (int i = 0; i < count; i++) {
			if (ChangeTicks[i] > tick) {
				iOutProps[outProps] = i;
				outProps++;
			}
		}

		return outProps;
	}

	public static IChangeFrameList AllocChangeFrameList(int properties, int curTick) {
		ChangeFrameList ret = new();
		ret.Init(properties, curTick);
		return ret;
	}
}