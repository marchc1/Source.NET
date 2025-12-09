using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EnvAmbientLight>;
public class C_EnvAmbientLight : C_SpatialEntity
{
	public static readonly RecvTable DT_EnvAmbientLight = new(DT_SpatialEntity, [
		RecvPropVector(FIELD.OF(nameof(Color))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EnvAmbientLight", DT_EnvAmbientLight).WithManualClassID(StaticClassIndices.CEnvAmbientLight);

	public Vector3 Color;
}
