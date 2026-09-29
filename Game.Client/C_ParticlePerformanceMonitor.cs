using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ParticlePerformanceMonitor>;
[NetworkName("CParticlePerformanceMonitor")]
public class C_ParticlePerformanceMonitor : C_BaseEntity
{
	public static readonly RecvTable DT_ParticlePerformanceMonitor = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(DisplayPerf))),
		RecvPropBool(FIELD.OF(nameof(MeasurePerf))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ParticlePerformanceMonitor);

	[NetworkName("m_bDisplayPerf")]
	public bool DisplayPerf;
	[NetworkName("m_bMeasurePerf")]
	public bool MeasurePerf;
}
