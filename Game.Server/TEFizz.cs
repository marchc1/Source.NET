using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEFizz>;
[NetworkName("CTEFizz")]
public class TEFizz(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEFizz = new(DT_BaseTempEntity, [
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(Density)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Current)), 16, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEFizz);

	[NetworkName("m_nEntity")]
	public int Entity;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nDensity")]
	public int Density;
	[NetworkName("m_nCurrent")]
	public int Current;
}
