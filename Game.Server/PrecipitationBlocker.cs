using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<PrecipitationBlocker>;
public class PrecipitationBlocker : BaseEntity
{
	public static readonly SendTable DT_PrecipitationBlocker = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("PrecipitationBlocker", DT_PrecipitationBlocker).WithManualClassID(StaticClassIndices.CPrecipitationBlocker);
}
