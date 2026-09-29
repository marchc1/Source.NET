using Source.Common;
using Source;

namespace Game.Client;

using FIELD = FIELD<C_EntityParticleTrail>;

public class C_EntityParticleTrailInfo
{
	[NetworkName("m_flLifetime")]
	public float Lifetime;
	[NetworkName("m_flStartSize")]
	public float StartSize;
	[NetworkName("m_flEndSize")]
	public float EndSize;

	public static readonly RecvTable DT_EntityParticleTrailInfo = new("DT_EntityParticleTrailInfo", [
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(Lifetime))),
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(StartSize))),
		RecvPropFloat(Source.FIELD<C_EntityParticleTrailInfo>.OF(nameof(EndSize))),
	]);
}

[NetworkName("CEntityParticleTrail")]
public class C_EntityParticleTrail : C_BaseParticleEntity
{
	[NetworkName("m_iMaterialName")]
	public int MaterialName;
	[NetworkName("m_Info")]
	public C_EntityParticleTrailInfo Info = new();
	[NetworkName("m_hConstraintEntity")]
	public EHANDLE ConstraintEntity;

	public static readonly RecvTable DT_EntityParticleTrail = new(DT_BaseParticleEntity, [
		RecvPropInt(FIELD.OF(nameof(MaterialName))),
		RecvPropDataTable("m_Info", FIELD.OF(nameof(Info)), C_EntityParticleTrailInfo.DT_EntityParticleTrailInfo),
		RecvPropEHandle(FIELD.OF(nameof(ConstraintEntity))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_EntityParticleTrail);
}
