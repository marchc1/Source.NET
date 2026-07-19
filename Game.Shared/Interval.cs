global using static Game.Shared.IntervalGlobals;

using Source.Common;

namespace Game.Shared;

public static class IntervalGlobals
{
	public static Interval ReadInterval(ReadOnlySpan<char> str) {
		Interval tmp;
		tmp.Start = 0;
		tmp.Range = 0;

		int comma = str.IndexOf(',');
		if (comma >= 0) {
			float.TryParse(str[..comma], out tmp.Start);
			float.TryParse(str[(comma + 1)..], out float range);
			tmp.Range = range - tmp.Start;
		}
		else if (!str.IsEmpty)
			float.TryParse(str, out tmp.Start);

		return tmp;
	}

	public static float RandomInterval(in Interval interval) {
		float ret = interval.Start;
		if (interval.Range != 0)
			ret += RandomFloat(0, interval.Range);

		return ret;
	}
}
