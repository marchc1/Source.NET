using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_FireSmoke>;
public class C_FireSmoke : C_BaseEntity
{
	public static readonly RecvTable DT_FireSmoke = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(StartScale))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropFloat(FIELD.OF(nameof(ScaleTime))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(FlameModelIndex))),
		RecvPropInt(FIELD.OF(nameof(FlameFromAboveModelIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FireSmoke", DT_FireSmoke).WithManualClassID(StaticClassIndices.CFireSmoke);

	public float StartScale;
	public float Scale;
	public float ScaleTime;
	public int Flags;
	public int FlameModelIndex;
	public int FlameFromAboveModelIndex;
}
