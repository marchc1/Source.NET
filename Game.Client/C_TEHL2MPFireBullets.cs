using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEHL2MPFireBullets>;
[NetworkName("CTEHL2MPFireBullets")]
public class C_TEHL2MPFireBullets : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEHL2MPFireBullets = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
		RecvPropInt(FIELD.OF(nameof(AmmoID))),
		RecvPropInt(FIELD.OF(nameof(Seed))),
		RecvPropInt(FIELD.OF(nameof(Shots))),
		RecvPropInt(FIELD.OF(nameof(Player))),
		RecvPropFloat(FIELD.OF(nameof(Spread))),
		RecvPropInt(FIELD.OF(nameof(DoImpacts))),
		RecvPropInt(FIELD.OF(nameof(DoTracers))),
		RecvPropString(FIELD.OF(nameof(TracerType))),
		RecvPropFloat(FIELD.OF(nameof(SpreadY))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEHL2MPFireBullets);

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
	public InlineArray512<char> TracerType;
	[NetworkName("m_flSpreadY")]
	public float SpreadY;
}
