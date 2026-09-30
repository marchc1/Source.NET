using Game.Shared;
using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EntityParticleTrail>;

public partial class EntityParticleTrailInfo
{
	[NetworkName("m_flLifetime")]
	[NetworkVar] public partial float Lifetime { get; set; }
	[NetworkName("m_flStartSize")]
	[NetworkVar] public partial float StartSize { get; set; }
	[NetworkName("m_flEndSize")]
	[NetworkVar] public partial float EndSize { get; set; }

	public static readonly SendTable DT_EntityParticleTrailInfo = new("DT_EntityParticleTrailInfo", [
		SendPropFloat(NetworkVarFields.Lifetime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, 0, PropFlags.NoScale),
	]);
}

// Datatable-accurate stub (gmod DT_EntityParticleTrail, baseclass DT_BaseParticleEntity).
[NetworkName("CEntityParticleTrail")]
public partial class EntityParticleTrail : BaseParticleEntity
{
	[NetworkName("m_iMaterialName")]
	[NetworkVar] public partial int MaterialName { get; set; }
	[NetworkName("m_Info")]
	[NetworkVarEmbedded] public partial EntityParticleTrailInfo Info { get; }
	[NetworkName("m_hConstraintEntity")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> ConstraintEntity { get; }

	public static readonly SendTable DT_EntityParticleTrail = new(DT_BaseParticleEntity, [
		SendPropInt(NetworkVarFields.MaterialName, 10, PropFlags.Unsigned),
		SendPropDataTable("m_Info", FIELD.OF(nameof(Info)), EntityParticleTrailInfo.DT_EntityParticleTrailInfo),
		SendPropEHandle(NetworkVarFields.ConstraintEntity),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EntityParticleTrail);
}
