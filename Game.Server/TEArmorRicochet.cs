using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEArmorRicochet>;
public class TEArmorRicochet : TEMetalSparks
{
	public static readonly SendTable DT_TEArmorRicochet = new(DT_TEMetalSparks, [
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEArmorRicochet", DT_TEArmorRicochet).WithManualClassID(StaticClassIndices.CTEArmorRicochet);
}
