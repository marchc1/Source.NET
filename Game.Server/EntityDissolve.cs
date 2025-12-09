using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityDissolve>;
public class EntityDissolve : BaseEntity
{
	public static readonly SendTable DT_EntityDissolve = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(StartTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeInStart)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeInLength)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeOutModelStart)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeOutModelLength)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeOutStart)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeOutLength)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(DissolveType)), 3, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(DissolverOrigin)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Magnitude)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("EntityDissolve", DT_EntityDissolve).WithManualClassID(StaticClassIndices.CEntityDissolve);

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
