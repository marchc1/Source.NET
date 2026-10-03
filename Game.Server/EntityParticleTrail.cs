using Game.Shared;
using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EntityParticleTrail>;

public class EntityParticleTrailInfo
{
	[NetworkName("m_flLifetime")]
	public float Lifetime;
	[NetworkName("m_flStartSize")]
	public float StartSize;
	[NetworkName("m_flEndSize")]
	public float EndSize;

	public static readonly SendTable DT_EntityParticleTrailInfo = new("DT_EntityParticleTrailInfo", [
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(Lifetime)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(EndSize)), 0, PropFlags.NoScale),
	]);
}

// Datatable-accurate stub (gmod DT_EntityParticleTrail, baseclass DT_BaseParticleEntity).
[LinkEntityToClass("env_particle_trail")]
[NetworkName("CEntityParticleTrail")]
public class EntityParticleTrail : BaseParticleEntity
{
	[NetworkName("m_iMaterialName")]
	public int MaterialName;
	[NetworkName("m_Info")]
	public EntityParticleTrailInfo Info = new();
	[NetworkName("m_hConstraintEntity")]
	public EHANDLE ConstraintEntity;

	public static readonly SendTable DT_EntityParticleTrail = new(DT_BaseParticleEntity, [
		SendPropInt(FIELD.OF(nameof(MaterialName)), 10, PropFlags.Unsigned),
		SendPropDataTable("m_Info", FIELD.OF(nameof(Info)), EntityParticleTrailInfo.DT_EntityParticleTrailInfo),
		SendPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_EntityParticleTrail);
}
