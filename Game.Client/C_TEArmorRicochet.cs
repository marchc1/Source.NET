using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEArmorRicochet>;
public class C_TEArmorRicochet : C_TEMetalSparks
{
	public static readonly RecvTable DT_TEArmorRicochet = new(DT_TEMetalSparks, []);
	public static readonly new ClientClass ClientClass = new ClientClass("TEArmorRicochet", DT_TEArmorRicochet).WithManualClassID(StaticClassIndices.CTEArmorRicochet);
}
