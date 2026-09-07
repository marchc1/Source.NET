using Source.Common;
using Source;

namespace Game.Server;

using FIELD = FIELD<EntityParticleTrail>;

public class EntityParticleTrailInfo
{
	public float Lifetime;
	public float StartSize;
	public float EndSize;

	public static readonly SendTable DT_EntityParticleTrailInfo = new([
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(Lifetime)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(Source.FIELD<EntityParticleTrailInfo>.OF(nameof(EndSize)), 0, PropFlags.NoScale),
	]);
}

// Datatable-accurate stub (gmod DT_EntityParticleTrail, baseclass DT_BaseParticleEntity).
public class EntityParticleTrail : BaseParticleEntity
{
	public int MaterialName;
	public EntityParticleTrailInfo Info = new();
	public EHANDLE ConstraintEntity;

	public static readonly SendTable DT_EntityParticleTrail = new(DT_BaseParticleEntity, [
		SendPropInt(FIELD.OF(nameof(MaterialName)), 10, PropFlags.Unsigned),
		SendPropDataTable(nameof(Info), FIELD.OF(nameof(Info)), EntityParticleTrailInfo.DT_EntityParticleTrailInfo),
		SendPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("EntityParticleTrail", DT_EntityParticleTrail).WithManualClassID(Game.Shared.StaticClassIndices.CEntityParticleTrail);
}
