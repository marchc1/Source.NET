using Source.Common;
using Source.Common.Commands;

namespace Game.Shared;

/// <summary>
/// Links a class type to a hammer name
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class LinkEntityToClassAttribute : Attribute
{
	public string LocalName;
	public LinkEntityToClassAttribute(string localName) => LocalName = localName;
}

public static class StaticClassIndicesHelpers
{
	public static void DumpDatatablesCompleted() {
		if (Singleton<ICommandLine>().FindParm("-dumpdatatablescompleted") == 0)
			return;
		// Check if the file exists. If it doesn't, our path traversal probably got messed up, so don't write a file somewhere totally random.

		string backPath = Path.Combine(AppContext.BaseDirectory, "../../../../DATATABLES_COMPLETED.md");

		if (!File.Exists(backPath))
			return;

		using FileStream stream = File.Open(backPath, FileMode.Truncate, FileAccess.Write);
		using StreamWriter writer = new(stream);

		writer.WriteLine("This is a list of all of the important SendClasses in " +
#if GMOD_DLL
		"Garry's Mod"
#else
		"(UNKNOWN)
#endif
		+ ", and which ones have completed datatables.");

		writer.WriteLine();

		StaticClassIndices[] values = Enum.GetValues<StaticClassIndices>();
		string[] names = new string[values.Length];
		for (int i = 0; i < values.Length; i++)
			names[i] = Enum.GetName(values[i])!;

		values.Sort();
		Span<bool> implemented = stackalloc bool[values.Length];
		for (ClientClass? clc = ClientClass.Head; clc != null; clc = clc.Next)
			if (clc.ClassID != -1)
				implemented[clc.ClassID] = true;
		for (ServerClass? svc = ServerClass.Head; svc != null; svc = svc.Next)
			if (svc.ClassID != -1)
				implemented[svc.ClassID] = true;

		for (int i = 0; i < values.Length; i++)
			writer.WriteLine($"- [{(implemented[i] ? 'x' : ' ')}] Class #{i}: {names[i]}");

		Msg("-dumpdatatablescompleted present, dumped all datatables to " + backPath + "\n");
		Msg($"  - {implemented.Count(true)}/{values.Length} datatables were marked as completed.\n");
		Msg($"  - Around {Math.Round((implemented.Count(true) / (float)values.Length) * 100, 2)}% have been completed!\n");
	}
}

#if GMOD_DLL
public enum StaticClassIndices
{
	AR2Explosion = 0,
	CAI_BaseNPC = 1,
	CAlyxEmpEffect = 2,
	CBaseAnimating = 3,
	CBaseAnimatingOverlay = 4,
	CBaseCombatCharacter = 5,
	CBaseCombatWeapon = 6,
	CBaseDoor = 7,
	CBaseEntity = 8,
	CBaseFlex = 9,
	CBaseGrenade = 10,
	CBaseHelicopter = 11,
	CBaseHelicopter_HL1 = 12,
	CBaseHL1CombatWeapon = 13,
	CBaseHL1MPCombatWeapon = 14,
	CBaseHL2MPBludgeonWeapon = 15,
	CBaseHL2MPCombatWeapon = 16,
	CBaseHLBludgeonWeapon = 17,
	CBaseHLCombatWeapon = 18,
	CBaseParticleEntity = 19,
	CBasePlayer = 20,
	CBasePropDoor = 21,
	CBaseTempEntity = 22,
	CBaseToggle = 23,
	CBaseTrigger = 24,
	CBaseViewModel = 25,
	CBeam = 26,
	CBeamSpotlight = 27,
	CBoneFollower = 28,
	CBoneManipulate = 29,
	CBreakableProp = 30,
	CBreakableSurface = 31,
	CCitadelEnergyCore = 32,
	CColorCorrection = 33,
	CColorCorrectionVolume = 34,
	CCrossbowBolt = 35,
	CDynamicLight = 36,
	CDynamicProp = 37,
	CEmbers = 38,
	CEntityDissolve = 39,
	CEntityFlame = 40,
	CEntityParticleTrail = 41,
	CEnvAmbientLight = 42,
	CEnvDetailController = 43,
	CEnvHeadcrabCanister = 44,
	CEnvParticleScript = 45,
	CEnvProjectedTexture = 46,
	CEnvQuadraticBeam = 47,
	CEnvScreenEffect = 48,
	CEnvScreenOverlay = 49,
	CEnvStarfield = 50,
	CEnvTonemapController = 51,
	CEnvWind = 52,
	CFireSmoke = 53,
	CFireTrail = 54,
	CFish = 55,
	CFlare = 56,
	CFleshEffectTarget = 57,
	CFlexManipulate = 58,
	CFogController = 59,
	CFunc_Dust = 60,
	CFunc_LOD = 61,
	CFuncAreaPortalWindow = 62,
	CFuncConveyor = 63,
	CFuncLadder = 64,
	CFuncMonitor = 65,
	CFuncOccluder = 66,
	CFuncReflectiveGlass = 67,
	CFuncRotating = 68,
	CFuncSmokeVolume = 69,
	CFuncTrackTrain = 70,
	CGameRulesProxy = 71,
	CGMOD_Player = 72,
	CGMODGameRulesProxy = 73,
	CHL2_Player = 74,
	CHL2MP_Player = 75,
	CHL2MPGameRulesProxy = 76,
	CHL2MPMachineGun = 77,
	CHL2MPRagdoll = 78,
	CHLMachineGun = 79,
	CHLSelectFireMachineGun = 80,
	CInfoLadderDismount = 81,
	CInfoLightingRelative = 82,
	CInfoOverlayAccessor = 83,
	CInfoTeleporterCountdown = 84,
	CLaserDot = 85,
	CLaserDot_HL1 = 86,
	CLightGlow = 87,
	CLuaNextBot = 88,
	CMaterialModifyControl = 89,
	CMortarShell = 90,
	CNPC_AntlionGuard = 91,
	CNPC_Barnacle = 92,
	CNPC_Barney = 93,
	CNPC_CombineGunship = 94,
	CNPC_Manhack = 95,
	CNPC_Portal_FloorTurret = 96,
	CNPC_Puppet = 97,
	CNPC_RocketTurret = 98,
	CNPC_RollerMine = 99,
	CNPC_Strider = 100,
	CNPC_Vortigaunt = 101,
	CParticleFire = 102,
	CParticlePerformanceMonitor = 103,
	CParticleSystem = 104,
	CPhysBeam = 105,
	CPhysBox = 106,
	CPhysBoxMultiplayer = 107,
	CPhysicsProp = 108,
	CPhysicsPropMultiplayer = 109,
	CPhysMagnet = 110,
	CPlasma = 111,
	CPlayerResource = 112,
	CPointCamera = 113,
	CPointWorldText = 114,
	CPoseController = 115,
	CPrecipitation = 116,
	CPrecipitationBlocker = 117,
	CPredictedViewModel = 118,
	CPropAirboat = 119,
	CPropCombineBall = 120,
	CPropCrane = 121,
	CPropDoorRotating = 122,
	CPropEnergyBall = 123,
	CPropJeep = 124,
	CPropJeepEpisodic = 125,
	CPropScalable = 126,
	CPropVehicleChoreoGeneric = 127,
	CPropVehicleDriveable = 128,
	CPropVehiclePrisonerPod = 129,
	CRagdollManager = 130,
	CRagdollProp = 131,
	CRagdollPropAttached = 132,
	CRopeKeyframe = 133,
	CRotorWashEmitter = 134,
	CRpgRocket = 135,
	CSceneEntity = 136,
	CScriptIntro = 137,
	CSENT_AI = 138,
	CSENT_anim = 139,
	CSENT_point = 140,
	CShadowControl = 141,
	CSlideshowDisplay = 142,
	CSmokeStack = 143,
	CSpatialEntity = 144,
	CSpotlightEnd = 145,
	CSprite = 146,
	CSpriteOriented = 147,
	CSpriteTrail = 148,
	CSteamJet = 149,
	CSun = 150,
	CTeam = 151,
	CTEAntlionDust = 152,
	CTEArmorRicochet = 153,
	CTEBaseBeam = 154,
	CTEBeamEntPoint = 155,
	CTEBeamEnts = 156,
	CTEBeamFollow = 157,
	CTEBeamLaser = 158,
	CTEBeamPoints = 159,
	CTEBeamRing = 160,
	CTEBeamRingPoint = 161,
	CTEBeamSpline = 162,
	CTEBloodSprite = 163,
	CTEBloodStream = 164,
	CTEBreakModel = 165,
	CTEBSPDecal = 166,
	CTEBubbles = 167,
	CTEBubbleTrail = 168,
	CTEClientProjectile = 169,
	CTEConcussiveExplosion = 170,
	CTEDecal = 171,
	CTEDust = 172,
	CTEDynamicLight = 173,
	CTEEffectDispatch = 174,
	CTEEnergySplash = 175,
	CTEExplosion = 176,
	CTEFizz = 177,
	CTEFootprintDecal = 178,
	CTEGaussExplosion = 179,
	CTEGlowSprite = 180,
	CTEHL2MPFireBullets = 181,
	CTEImpact = 182,
	CTEKillPlayerAttachments = 183,
	CTELargeFunnel = 184,
	CTEMetalSparks = 185,
	CTEMuzzleFlash = 186,
	CTEParticleSystem = 187,
	CTEPhysicsProp = 188,
	CTEPlayerAnimEvent = 189,
	CTEPlayerDecal = 190,
	CTEProjectedDecal = 191,
	CTEShatterSurface = 192,
	CTEShowLine = 193,
	CTesla = 194,
	CTESmoke = 195,
	CTESparks = 196,
	CTESprite = 197,
	CTESpriteSpray = 198,
	CTEWorldDecal = 199,
	CTriggerPlayerMovement = 200,
	CVGuiScreen = 201,
	CVortigauntChargeToken = 202,
	CVortigauntEffectDispel = 203,
	CWaterBullet = 204,
	CWaterLODControl = 205,
	CWeapon357 = 206,
	CWeapon357_HL1 = 207,
	CWeapon_SLAM = 208,
	CWeaponAlyxGun = 209,
	CWeaponAnnabelle = 210,
	CWeaponAR2 = 211,
	CWeaponBugBait = 212,
	CWeaponCitizenPackage = 213,
	CWeaponCitizenSuitcase = 214,
	CWeaponCrossbow = 215,
	CWeaponCrossbow_HL1 = 216,
	CWeaponCrowbar = 217,
	CWeaponCrowbar_HL1 = 218,
	CWeaponCubemap = 219,
	CWeaponCycler = 220,
	CWeaponEgon = 221,
	CWeaponFrag = 222,
	CWeaponGauss = 223,
	CWeaponGlock = 224,
	CWeaponHandGrenade = 225,
	CWeaponHgun = 226,
	CWeaponHL2MPBase = 227,
	CWeaponMP5 = 228,
	CWeaponOldManHarpoon = 229,
	CWeaponPhysCannon = 230,
	CWeaponPhysGun = 231,
	CWeaponPistol = 232,
	CWeaponRPG = 233,
	CWeaponRPG_HL1 = 234,
	CWeaponSatchel = 235,
	CWeaponShotgun = 236,
	CWeaponShotgun_HL1 = 237,
	CWeaponSMG1 = 238,
	CWeaponSnark = 239,
	CWeaponStunStick = 240,
	CWeaponSWEP = 241,
	CWeaponTripMine = 242,
	CWorld = 243,
	DustTrail = 244,
	MovieExplosion = 245,
	NextBotCombatCharacter = 246,
	ParticleSmokeGrenade = 247,
	RocketTrail = 248,
	SmokeTrail = 249,
	SporeExplosion = 250,
}
#endif
