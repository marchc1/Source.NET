using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<RpgRocket>;
public class RpgRocket : BaseGrenade
{
	public static readonly SendTable DT_RpgRocket = new(DT_BaseGrenade, []);
	public static readonly new ServerClass ServerClass = new ServerClass("RpgRocket", DT_RpgRocket).WithManualClassID(StaticClassIndices.CRpgRocket);
}
