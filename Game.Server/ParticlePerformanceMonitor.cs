using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticlePerformanceMonitor>;
public class ParticlePerformanceMonitor : PointEntity
{
	public static readonly SendTable DT_ParticlePerformanceMonitor = new(DT_BaseEntity, [
		SendPropBool(FIELD.OF(nameof(DisplayPerf))),
		SendPropBool(FIELD.OF(nameof(MeasurePerf))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("ParticlePerformanceMonitor", DT_ParticlePerformanceMonitor).WithManualClassID(StaticClassIndices.CParticlePerformanceMonitor);

	public bool DisplayPerf;
	public bool MeasurePerf;
}
