using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Embers>;
[LinkEntityToClass("env_embers")]
[NetworkName("CEmbers")]
public class Embers : BaseEntity
{
	public static readonly SendTable DT_Embers = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(Density)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Lifetime)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Speed)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Emit)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Embers);

	[NetworkName("m_nDensity")]
	public int Density;
	[NetworkName("m_nLifetime")]
	public int Lifetime;
	[NetworkName("m_nSpeed")]
	public new int Speed;
	[NetworkName("m_bEmit")]
	public int Emit;
}
