using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEWorldDecal>;
public class TEWorldDecal : BaseTempEntity
{
	public static readonly SendTable DT_TEWorldDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEWorldDecal", DT_TEWorldDecal).WithManualClassID(StaticClassIndices.CTEWorldDecal);

	public Vector3 Origin;
	public int Index;
}
