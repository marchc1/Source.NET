using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityFlame>;
[LinkEntityToClass("entityflame")]
[LinkEntityToClass("env_entity_igniter")]
[NetworkName("CEntityFlame")]
public class EntityFlame : BaseEntity
{
	public static readonly SendTable DT_EntityFlame = new(DT_BaseEntity, [
		SendPropEHandle(FIELD.OF(nameof(EntAttached))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EntityFlame);

	[NetworkName("m_hEntAttached")]
	public EHANDLE EntAttached = new();
}
