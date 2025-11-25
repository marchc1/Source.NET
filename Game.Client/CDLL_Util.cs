global using static Game.Client.CDLL_Util;

namespace Game.Client
{
	public static class CDLL_Util
	{
		public static int ScreenWidth() {
			GetHudSize(out int w, out _);
			return w;
		}

		public static int ScreenHeight() {
			GetHudSize(out _, out int h);
			return h;
		}
	}
}

namespace Game
{
	public static partial class Util
	{

	}
}
