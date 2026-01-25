global using static Game.Util_Globals;

using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game
{
	public static partial class Util_Globals {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ENTINDEX(Edict? edict) {
			int result = edict != null ? edict.EdictIndex : 0;
			Assert(result == engine.IndexOfEdict(edict));
			return result;
		}
	}
	public static partial class Util
	{
		public static void PrecacheOther(ReadOnlySpan<char> className, ReadOnlySpan<char> modelName = default) => throw new NotImplementedException();
	}
}
