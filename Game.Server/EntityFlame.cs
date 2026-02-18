using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityFlame>;
public class EntityFlame : BaseEntity
{
	public static readonly SendTable DT_EntityFlame = new(DT_BaseEntity, [
		SendPropEHandle(FIELD.OF(nameof(EntAttached))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EntityFlame", DT_EntityFlame).WithManualClassID(StaticClassIndices.CEntityFlame);

	public EHANDLE EntAttached = new();
}
