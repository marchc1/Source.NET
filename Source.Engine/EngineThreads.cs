namespace Source.Engine;

public static class EngineThreads
{
	public static bool ThreadedEngine = false;
	public static int MaterialSystemThread = 0;
	public static int ServerThread = 1;

	public static bool IsEngineThreaded() => ThreadedEngine;
}
