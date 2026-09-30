#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared;
using FIELD = Source.FIELD<SpriteTrail>;
[NetworkName("CSpriteTrail")]
public partial class SpriteTrail : Sprite
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
		SendPropFloat(NetworkVarFields.LifeTime, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartWidth, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndWidth, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartWidthVariance, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.TextureRes, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeLength, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.SkyboxOrigin, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.SkyboxScale, 0, PropFlags.NoScale),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_SpriteTrail);
#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpriteTrail);
#endif

	[NetworkName("m_flLifeTime")]
	[NetworkVar] public partial TimeUnit_t LifeTime { get; set; }
	[NetworkName("m_flStartWidth")]
	[NetworkVar] public partial float StartWidth { get; set; }
	[NetworkName("m_flEndWidth")]
	[NetworkVar] public partial float EndWidth { get; set; }
	[NetworkName("m_flStartWidthVariance")]
	[NetworkVar] public partial float StartWidthVariance { get; set; }
	[NetworkName("m_flTextureRes")]
	[NetworkVar] public partial float TextureRes { get; set; }
	[NetworkName("m_flMinFadeLength")]
	[NetworkVar] public partial float FadeLength { get; set; }
	[NetworkName("m_vecSkyboxOrigin")]
	[NetworkVar] public partial Vector3 SkyboxOrigin { get; set; }
	[NetworkName("m_flSkyboxScale")]
	[NetworkVar] public partial float SkyboxScale { get; set; }
}
#endif
