global using static Game.Client.CDLL_Util;

using Game.Client;

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

	public enum LightIndex
	{
		TEDynamic = 0x10000000,
		PlayerBright = 0x20000000,
		MuzzleFlash = 0x30000000
	}
}

namespace Game
{
	public static partial class Util
	{
		public static void PrecacheOther(ReadOnlySpan<char> classname) {
			C_BaseEntity? entity = C_BaseEntity.CreateEntityByName(classname);
			if (entity == null) {
				Warning("NULL Ent in UTIL_PrecacheOther\n");
				return;
			}

			entity.Precache();
			// Bye bye
			entity.Release();
		}
	}
}
