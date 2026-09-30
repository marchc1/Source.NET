using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityFlame>;
[NetworkName("CEntityFlame")]
public partial class EntityFlame : BaseEntity
{
	public static readonly SendTable DT_EntityFlame = new(DT_BaseEntity, [
		SendPropEHandle(EntityFlame.NetworkVarFields.EntAttached),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EntityFlame);

	[NetworkName("m_hEntAttached")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> EntAttached { get; }
}
