using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Embers>;
public class Embers : BaseEntity
{
	public static readonly SendTable DT_Embers = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(Density)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Lifetime)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Speed)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Emit)), 2, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Embers", DT_Embers).WithManualClassID(StaticClassIndices.CEmbers);

	public int Density;
	public int Lifetime;
	public new int Speed;
	public int Emit;
}
