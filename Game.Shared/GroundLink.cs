using Source.Common;

namespace Game.Shared;

public class GroundLink
{
	public BaseHandle Entity = new();
	public GroundLink NextLink = null!;
	public GroundLink PrevLink = null!;
}

[Flags]
public enum TouchLinkFlags
{
	StartTouch = 0x00000001,
}

public class TouchLink
{
	public const int TOUCHSTAMP_EVENT_DRIVEN = -1;

	public BaseHandle EntityTouched = new();
	public int TouchStamp;
	public TouchLink NextLink = null!;
	public TouchLink PrevLink = null!;
	public TouchLinkFlags Flags;
}
