using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_EntityParticleTrail>;

public class C_EntityParticleTrailInfo
{
	public float Lifetime;
	public float StartSize;
	public float EndSize;

	public static readonly RecvTable DT_EntityParticleTrailInfo = new("DT_EntityParticleTrailInfo", [
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(Lifetime))),
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(StartSize))),
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(EndSize))),
	]);
}

public class C_EntityParticleTrail : C_BaseParticleEntity
{
	public int MaterialName;
	public C_EntityParticleTrailInfo Info = new();
	public EHANDLE ConstraintEntity;

	public static readonly RecvTable DT_EntityParticleTrail = new(DT_BaseParticleEntity, [
		RecvPropInt(FIELD.OF(nameof(MaterialName))),
		RecvPropDataTable(nameof(Info), FIELD.OF(nameof(Info)), C_EntityParticleTrailInfo.DT_EntityParticleTrailInfo),
		RecvPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass("EntityParticleTrail", DT_EntityParticleTrail).WithManualClassID(Game.Shared.StaticClassIndices.CEntityParticleTrail);
}
