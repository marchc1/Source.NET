#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared;
using FIELD = Source.FIELD<Sprite>;
[NetworkName("CSprite")]
public partial class Sprite : BaseEntity
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
		// todo: RecvProxy_SpriteScale
		RecvPropFloat(FIELD.OF(nameof(SpriteScale))),
		RecvPropFloat(FIELD.OF(nameof(GlowProxySize))),
		RecvPropFloat(FIELD.OF(nameof(HDRColorScale))),
		RecvPropFloat(FIELD.OF(nameof(SpriteFramerate))),
		RecvPropFloat(FIELD.OF(nameof(Frame))),
		RecvPropFloat(FIELD.OF(nameof(BrightnessTime))),
		RecvPropInt(FIELD.OF(nameof(Brightness))),
		RecvPropBool(FIELD.OF(nameof(WorldSpaceScale)))
#else
		SendPropEHandle(Sprite.NetworkVarFields.AttachedToEntity),
		SendPropInt(NetworkVarFields.Attachment, 8),
		SendPropFloat(NetworkVarFields.ScaleTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpriteScale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.GlowProxySize, 6, PropFlags.RoundUp, 0.0f, 64.0f),
		SendPropFloat(NetworkVarFields.HDRColorScale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SpriteFramerate, 8, PropFlags.RoundUp, 0, 60.0f),
		SendPropFloat(NetworkVarFields.Frame, 20, PropFlags.RoundDown, 0, 256),
		SendPropFloat(NetworkVarFields.BrightnessTime, 0, PropFlags.NoScale, 0, 0),
		SendPropInt(NetworkVarFields.Brightness, 8, PropFlags.Unsigned),
		SendPropBool(NetworkVarFields.WorldSpaceScale)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_Sprite);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Sprite);
#endif
	[NetworkName("m_hAttachedToEntity")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> AttachedToEntity { get; }
	[NetworkName("m_nAttachment")]
	[NetworkVar] public partial int Attachment { get; set; }
	[NetworkName("m_flScaleTime")]
	[NetworkVar] public partial TimeUnit_t ScaleTime { get; set; }
	[NetworkName("m_flSpriteScale")]
	[NetworkVar] public partial float SpriteScale { get; set; }
	[NetworkName("m_flGlowProxySize")]
	[NetworkVar] public partial float GlowProxySize { get; set; }
	[NetworkName("m_flHDRColorScale")]
	[NetworkVar] public partial float HDRColorScale { get; set; }
	[NetworkName("m_flSpriteFramerate")]
	[NetworkVar] public partial TimeUnit_t SpriteFramerate { get; set; }
	[NetworkName("m_flFrame")]
	[NetworkVar] public partial TimeUnit_t Frame { get; set; }
	[NetworkName("m_flBrightnessTime")]
	[NetworkVar] public partial TimeUnit_t BrightnessTime { get; set; }
	[NetworkName("m_nBrightness")]
	[NetworkVar] public partial int Brightness { get; set; }
	[NetworkName("m_bWorldSpaceScale")]
	[NetworkVar] public partial bool WorldSpaceScale { get; set; }
}
[NetworkName("CSpriteOriented")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_SpriteOriented);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpriteOriented);
#endif
}
#endif
