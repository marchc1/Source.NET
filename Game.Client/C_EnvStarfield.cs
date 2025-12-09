using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvStarfield>;
public class C_EnvStarfield : C_BaseEntity
{
	public static readonly RecvTable DT_EnvStarfield = new(DT_BaseEntity, [
		RecvPropBool(FIELD.OF(nameof(On))),
		RecvPropFloat(FIELD.OF(nameof(Density))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvStarfield", DT_EnvStarfield).WithManualClassID(StaticClassIndices.CEnvStarfield);

	public bool On;
	public float Density;
}
