#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared;
using FIELD = Source.FIELD<Sprite>;
public class Sprite : SharedBaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_Sprite = new(DT_BaseEntity, [
#if CLIENT_DLL
		RecvPropEHandle(FIELD.OF(nameof(AttachedToEntity))),
		RecvPropInt(FIELD.OF(nameof(Attachment))),
		RecvPropFloat(FIELD.OF(nameof(ScaleTime))),
		RecvPropFloat(FIELD.OF(nameof(SpriteScale))),
		RecvPropFloat(FIELD.OF(nameof(GlowProxySize))),
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
		RecvPropFloat(FIELD.OF(nameof(SpriteFramerate))),
		RecvPropFloat(FIELD.OF(nameof(Frame))),
		RecvPropFloat(FIELD.OF(nameof(BrightnessTime))),
		RecvPropInt(FIELD.OF(nameof(Brightness))),
		RecvPropBool(FIELD.OF(nameof(WorldSpaceScale)))
#else
		SendPropEHandle(FIELD.OF(nameof(AttachedToEntity))),
		SendPropInt(FIELD.OF(nameof(Attachment)), 8),
		SendPropFloat(FIELD.OF(nameof(ScaleTime)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpriteScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(GlowProxySize)), 6, PropFlags.RoundUp, 1, 64),
		SendPropFloat(FIELD.OF(nameof(HDRColorScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(SpriteFramerate)), 8, PropFlags.RoundUp, 0.2f, 60f),
		SendPropFloat(FIELD.OF(nameof(Frame)), 20, PropFlags.RoundDown, 0, 256),
		SendPropFloat(FIELD.OF(nameof(BrightnessTime)), 0, PropFlags.NoScale, 0, 0),
		SendPropInt(FIELD.OF(nameof(Brightness)), 8, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(WorldSpaceScale)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("Sprite", null, null, DT_Sprite).WithManualClassID(StaticClassIndices.CSprite);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("Sprite", DT_Sprite).WithManualClassID(StaticClassIndices.CSprite);
#endif
	public readonly EHANDLE AttachedToEntity = new();
	public int Attachment;
	public TimeUnit_t ScaleTime;
	public float SpriteScale;
	public float GlowProxySize;
	public float HDRColorScale;
	public TimeUnit_t SpriteFramerate;
	public TimeUnit_t Frame;
	public TimeUnit_t BrightnessTime;
	public int Brightness;
	public bool WorldSpaceScale;
}
public class SpriteOriented : Sprite
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_SpriteOriented = new(DT_Sprite, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("SpriteOriented", null, null, DT_SpriteOriented).WithManualClassID(StaticClassIndices.CSpriteOriented);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("SpriteOriented", DT_SpriteOriented).WithManualClassID(StaticClassIndices.CSpriteOriented);
#endif
}
#endif
