using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EntityDissolve>;
public class C_EntityDissolve : C_BaseEntity
{
	public static readonly RecvTable DT_EntityDissolve = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(StartTime))),
		RecvPropFloat(FIELD.OF(nameof(FadeInStart))),
		RecvPropFloat(FIELD.OF(nameof(FadeInLength))),
		RecvPropFloat(FIELD.OF(nameof(FadeOutModelStart))),
		RecvPropFloat(FIELD.OF(nameof(FadeOutModelLength))),
		RecvPropFloat(FIELD.OF(nameof(FadeOutStart))),
		RecvPropFloat(FIELD.OF(nameof(FadeOutLength))),
		RecvPropInt(FIELD.OF(nameof(DissolveType))),
		RecvPropVector(FIELD.OF(nameof(DissolverOrigin))),
		RecvPropInt(FIELD.OF(nameof(Magnitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("EntityDissolve", DT_EntityDissolve).WithManualClassID(StaticClassIndices.CEntityDissolve);

	public float StartTime;
	public float FadeInStart;
	public float FadeInLength;
	public float FadeOutModelStart;
	public float FadeOutModelLength;
	public float FadeOutStart;
	public float FadeOutLength;
	public int DissolveType;
	public Vector3 DissolverOrigin;
	public int Magnitude;
}
