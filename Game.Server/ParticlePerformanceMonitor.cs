using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticlePerformanceMonitor>;
[LinkEntityToClass("env_particle_performance_monitor")]
[NetworkName("CParticlePerformanceMonitor")]
public class ParticlePerformanceMonitor : PointEntity
{
	public static readonly SendTable DT_ParticlePerformanceMonitor = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(DisplayPerf))),
		SendPropBool(FIELD.OF(nameof(MeasurePerf))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ParticlePerformanceMonitor);

	[NetworkName("m_bDisplayPerf")]
	public bool DisplayPerf;
	[NetworkName("m_bMeasurePerf")]
	public bool MeasurePerf;
}
