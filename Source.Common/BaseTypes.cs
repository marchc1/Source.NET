namespace Source.Common;

public struct Interval{
	public float Start;
	public float Range;
}

public static class BaseTypesGlobals {
	public static int PAD_NUMBER(int number, int boundary) => (number + (boundary - 1)) / boundary * boundary;
}
