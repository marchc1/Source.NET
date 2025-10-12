using Game.Client;
using Game.Shared;

using Source.Common;

namespace Game.Server;

public class C_BreakableProp : C_BaseAnimating
{
	public static readonly RecvTable DT_BreakableProp = new(DT_BaseAnimating, []);
	public static readonly new ClientClass ClientClass = new ClientClass("BreakableProp", DT_BreakableProp).WithManualClassID(StaticClassIndices.CBreakableProp);
}
