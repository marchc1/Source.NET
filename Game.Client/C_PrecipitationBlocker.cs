using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PrecipitationBlocker>;
public class C_PrecipitationBlocker : C_BaseEntity
{
	public static readonly RecvTable DT_PrecipitationBlocker = new(DT_BaseEntity, [	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PrecipitationBlocker", DT_PrecipitationBlocker).WithManualClassID(StaticClassIndices.CPrecipitationBlocker);
}
