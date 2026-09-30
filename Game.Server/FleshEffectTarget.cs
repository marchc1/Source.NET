using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FleshEffectTarget>;
[NetworkName("CFleshEffectTarget")]
public partial class FleshEffectTarget : BaseEntity
{
	public static readonly SendTable DT_FleshEffectTarget = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Radius, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScaleTime, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FleshEffectTarget);

	[NetworkName("m_flRadius")]
	[NetworkVar] public partial float Radius { get; set; }
	[NetworkName("m_flScaleTime")]
	[NetworkVar] public partial float ScaleTime { get; set; }
}
