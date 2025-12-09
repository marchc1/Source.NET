using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEFizz>;
public class TEFizz : BaseTempEntity
{
	public static readonly SendTable DT_TEFizz = new(DT_BaseTempEntity, [
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Density)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Current)), 16, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEFizz", DT_TEFizz).WithManualClassID(StaticClassIndices.CTEFizz);

	public int Entity;
	public int ModelIndex;
	public int Density;
	public int Current;
}
