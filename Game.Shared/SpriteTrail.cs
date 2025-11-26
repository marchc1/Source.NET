#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared;
using FIELD = Source.FIELD<SpriteTrail>;
public class SpriteTrail : Sprite
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_SpriteTrail = new(DT_Sprite, [
#if CLIENT_DLL
		RecvPropFloat(FIELD.OF(nameof(LifeTime))),
		RecvPropFloat(FIELD.OF(nameof(StartWidth))),
		RecvPropFloat(FIELD.OF(nameof(EndWidth))),
		RecvPropFloat(FIELD.OF(nameof(StartWidthVariance))),
		RecvPropFloat(FIELD.OF(nameof(TextureRes))),
		RecvPropFloat(FIELD.OF(nameof(FadeLength))),
		RecvPropVector(FIELD.OF(nameof(SkyboxOrigin))),
		RecvPropFloat(FIELD.OF(nameof(SkyboxScale))),
#else
		SendPropFloat(FIELD.OF(nameof(LifeTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartWidthVariance)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(TextureRes)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeLength)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(SkyboxOrigin)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SkyboxScale)), 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("SpriteTrail", null, null, DT_SpriteTrail).WithManualClassID(StaticClassIndices.CSpriteTrail);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("SpriteTrail", DT_SpriteTrail).WithManualClassID(StaticClassIndices.CSpriteTrail);
#endif

	public TimeUnit_t LifeTime;
	public float StartWidth;
	public float EndWidth;
	public float StartWidthVariance;
	public float TextureRes;
	public float FadeLength;
	public Vector3 SkyboxOrigin;
	public float SkyboxScale;
}
#endif
