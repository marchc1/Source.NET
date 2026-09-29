using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EntityDissolve>;
[NetworkName("CEntityDissolve")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_EntityDissolve);

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
