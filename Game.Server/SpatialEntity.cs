using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SpatialEntity>;
[NetworkName("CSpatialEntity")]
public class SpatialEntity : BaseEntity
{
	public static readonly SendTable DT_SpatialEntity = new([
		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(CurWeight)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Enabled))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpatialEntity);

	[NetworkName("m_MinFalloff")]
	public float MinFalloff;
	[NetworkName("m_MaxFalloff")]
	public float MaxFalloff;
	[NetworkName("m_flCurWeight")]
	public float CurWeight;
	[NetworkName("m_bEnabled")]
	public bool Enabled;
}
