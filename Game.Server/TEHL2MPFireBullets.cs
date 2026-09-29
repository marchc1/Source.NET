using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEHL2MPFireBullets>;
[NetworkName("CTEHL2MPFireBullets")]
public class TEHL2MPFireBullets(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEHL2MPFireBullets = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(AmmoID)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Seed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Shots)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Player)), 6, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Spread)), 10, 0, 0, 1),
		SendPropInt(FIELD.OF(nameof(DoImpacts)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(DoTracers)), 1, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(TracerType)), 10, 0),
		SendPropFloat(FIELD.OF(nameof(SpreadY)), 10, 0, 0, 1),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEHL2MPFireBullets);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
	[NetworkName("m_iAmmoID")]
	public int AmmoID;
	[NetworkName("m_iSeed")]
	public int Seed;
	[NetworkName("m_iShots")]
	public int Shots;
	[NetworkName("m_iPlayer")]
	public int Player;
	[NetworkName("m_flSpread")]
	public float Spread;
	[NetworkName("m_bDoImpacts")]
	public int DoImpacts;
	[NetworkName("m_bDoTracers")]
	public int DoTracers;
	[NetworkName("m_TracerType")]
	public InlineArray256<char> TracerType;
	[NetworkName("m_flSpreadY")]
	public float SpreadY;
}
