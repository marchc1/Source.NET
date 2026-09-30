using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Embers>;
[NetworkName("CEmbers")]
public partial class Embers : BaseEntity
{
	public static readonly SendTable DT_Embers = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.Density, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Lifetime, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Speed, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Emit, 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Embers);

	[NetworkName("m_nDensity")]
	[NetworkVar] public partial int Density { get; set; }
	[NetworkName("m_nLifetime")]
	[NetworkVar] public partial int Lifetime { get; set; }
	[NetworkName("m_nSpeed")]
	[NetworkVar] public new partial int Speed { get; set; }
	[NetworkName("m_bEmit")]
	[NetworkVar] public partial int Emit { get; set; }
}
