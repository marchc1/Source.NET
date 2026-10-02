using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<EntityDissolve>;
[LinkEntityToClass("env_entity_dissolver")]
[NetworkName("CEntityDissolve")]
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_EntityDissolve);

	[NetworkName("m_flStartTime")]
	public float StartTime;
	[NetworkName("m_flFadeInStart")]
	public float FadeInStart;
	[NetworkName("m_flFadeInLength")]
	public float FadeInLength;
	[NetworkName("m_flFadeOutModelStart")]
	public float FadeOutModelStart;
	[NetworkName("m_flFadeOutModelLength")]
	public float FadeOutModelLength;
	[NetworkName("m_flFadeOutStart")]
	public float FadeOutStart;
	[NetworkName("m_flFadeOutLength")]
	public float FadeOutLength;
	[NetworkName("m_nDissolveType")]
	public int DissolveType;
	[NetworkName("m_vDissolverOrigin")]
	public Vector3 DissolverOrigin;
	[NetworkName("m_nMagnitude")]
	public int Magnitude;
}
