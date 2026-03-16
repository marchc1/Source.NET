namespace Source.Engine;

public interface IChangeFrameList
{
	int GetNumProps();
	void SetChangeTick(ReadOnlySpan<int> propIndices, int iPropIndices, int tick);
	int GetPropsChangedAfterTick(int tick, Span<int> outProps, int maxOutProps);
	IChangeFrameList Copy();
}
