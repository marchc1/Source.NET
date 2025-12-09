using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<WaterBullet>;
public class WaterBullet : BaseAnimating
{
	public static readonly SendTable DT_WaterBullet = new(DT_BaseAnimating, []);
	public static readonly new ServerClass ServerClass = new ServerClass("WaterBullet", DT_WaterBullet).WithManualClassID(StaticClassIndices.CWaterBullet);
}
