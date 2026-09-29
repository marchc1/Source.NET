using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_WaterBullet>;
[NetworkName("CWaterBullet")]
public class C_WaterBullet : C_BaseAnimating
{
	public static readonly RecvTable DT_WaterBullet = new(DT_BaseAnimating, []);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_WaterBullet);
}
