using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_RpgRocket>;
[NetworkName("CRpgRocket")]
public class C_RpgRocket : C_BaseGrenade
{
	public static readonly RecvTable DT_RpgRocket = new(DT_BaseGrenade, []);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_RpgRocket);
}
