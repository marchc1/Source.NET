using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_RpgRocket>;
public class C_RpgRocket : C_BaseGrenade
{
	public static readonly RecvTable DT_RpgRocket = new(DT_BaseGrenade, []);
	public static readonly new ClientClass ClientClass = new ClientClass("RpgRocket", DT_RpgRocket).WithManualClassID(StaticClassIndices.CRpgRocket);
}
