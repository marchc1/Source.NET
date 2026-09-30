using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<ParticlePerformanceMonitor>;
[NetworkName("CParticlePerformanceMonitor")]
public partial class ParticlePerformanceMonitor : PointEntity
{
	public static readonly SendTable DT_ParticlePerformanceMonitor = new(DT_BaseEntity, [
		SendPropBool(NetworkVarFields.DisplayPerf),
		SendPropBool(NetworkVarFields.MeasurePerf),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ParticlePerformanceMonitor);

	[NetworkName("m_bDisplayPerf")]
	[NetworkVar] public partial bool DisplayPerf { get; set; }
	[NetworkName("m_bMeasurePerf")]
	[NetworkVar] public partial bool MeasurePerf { get; set; }
}
