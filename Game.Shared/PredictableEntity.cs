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
	CAI_BaseNPC,
	CAlyxEmpEffect,
	CBaseAnimating,
	CBaseAnimatingOverlay,
	CBaseCombatCharacter,
	CBaseCombatWeapon,
	CBaseDoor,
	CBaseEntity,
	CBaseFlex,
	CBaseGrenade,
	CBaseHelicopter,
	CBaseHelicopter_HL1,
	CBaseHL1CombatWeapon,
	CBaseHL1MPCombatWeapon,
	CBaseHL2MPBludgeonWeapon,
	CBaseHL2MPCombatWeapon,
	CBaseHLBludgeonWeapon,
	CBaseHLCombatWeapon,
	CBaseParticleEntity,
	CBasePlayer,
	CBasePropDoor,
	CBaseTempEntity,
	CBaseToggle,
	CBaseTrigger,
	CBaseViewModel,
	CBeam,
	CBeamSpotlight,
	CBoneFollower,
	CBoneManipulate,
	CBreakableProp,
	CBreakableSurface,
	CCitadelEnergyCore,
	CColorCorrection,
	CColorCorrectionVolume,
	CCrossbowBolt,
	CDynamicLight,
	CDynamicProp,
	CEmbers,
	CEntityDissolve,
	CEntityFlame,
	CEntityParticleTrail,
	CEnvAmbientLight,
	CEnvDetailController,
	CEnvHeadcrabCanister,
	CEnvParticleScript,
	CEnvProjectedTexture,
	CEnvQuadraticBeam,
	CEnvScreenEffect,
	CEnvScreenOverlay,
	CEnvStarfield,
	CEnvTonemapController,
	CEnvWind,
	CFireSmoke,
	CFireTrail,
	CFish,
	CFlare,
	CFleshEffectTarget,
	CFlexManipulate,
	CFogController,
	CFunc_Dust,
	CFunc_LOD,
	CFuncAreaPortalWindow,
	CFuncConveyor,
	CFuncLadder,
	CFuncMonitor,
	CFuncOccluder,
	CFuncReflectiveGlass,
	CFuncRotating,
	CFuncSmokeVolume,
	CFuncTrackTrain,
	CGameRulesProxy,
	CGMOD_Player,
	CGMODGameRulesProxy,
	CHL2_Player,
	CHL2MP_Player,
	CHL2MPGameRulesProxy,
	CHL2MPMachineGun,
	CHL2MPRagdoll,
	CHLMachineGun,
	CHLSelectFireMachineGun,
	CInfoLadderDismount,
	CInfoLightingRelative,
	CInfoOverlayAccessor,
	CInfoTeleporterCountdown,
	CLaserDot,
	CLaserDot_HL1,
	CLightGlow,
	CLuaNextBot,
	CMaterialModifyControl,
	CMortarShell,
	CNPC_AntlionGuard,
	CNPC_Barnacle,
	CNPC_Barney,
	CNPC_CombineGunship,
	CNPC_Manhack,
	CNPC_Portal_FloorTurret,
	CNPC_Puppet,
	CNPC_RocketTurret,
	CNPC_RollerMine,
	CNPC_Strider,
	CNPC_Vortigaunt,
	CParticlePerformanceMonitor,
	CParticleSystem,
	CPhysBeam,
	CPhysBox,
	CPhysBoxMultiplayer,
	CPhysicsProp,
	CPhysicsPropMultiplayer,
	CPhysMagnet,
	CPlasma,
	CPlayerResource,
	CPointCamera,
	CPointWorldText,
	CPoseController,
	CPrecipitation,
	CPrecipitationBlocker,
	CPredictedViewModel,
	CPropAirboat,
	CPropCombineBall,
	CPropCrane,
	CPropDoorRotating,
	CPropEnergyBall,
	CPropJeep,
	CPropJeepEpisodic,
	CPropScalable,
	CPropVehicleChoreoGeneric,
	CPropVehicleDriveable,
	CPropVehiclePrisonerPod,
	CRagdollManager,
	CRagdollProp,
	CRagdollPropAttached,
	CRopeKeyframe,
	CRotorWashEmitter,
	CRpgRocket,
	CSceneEntity,
	CScriptIntro,
	CSENT_AI,
	CSENT_anim,
	CSENT_point,
	CShadowControl,
	CSlideshowDisplay,
	CSmokeStack,
	CSpatialEntity,
	CSpotlightEnd,
	CSprite,
	CSpriteOriented,
	CSpriteTrail,
	CSteamJet,
	CSun,
	CTeam,
	CTEAntlionDust,
	CTEArmorRicochet,
	CTEBaseBeam,
	CTEBeamEntPoint,
	CTEBeamEnts,
	CTEBeamFollow,
	CTEBeamLaser,
	CTEBeamPoints,
	CTEBeamRing,
	CTEBeamRingPoint,
	CTEBeamSpline,
	CTEBloodSprite,
	CTEBloodStream,
	CTEBreakModel,
	CTEBSPDecal,
	CTEBubbles,
	CTEBubbleTrail,
	CTEClientProjectile,
	CTEConcussiveExplosion,
	CTEDecal,
	CTEDust,
	CTEDynamicLight,
	CTEEffectDispatch,
	CTEEnergySplash,
	CTEExplosion,
	CTEFizz,
	CTEFootprintDecal,
	CTEGaussExplosion,
	CTEGlowSprite,
	CTEHL2MPFireBullets,
	CTEKillPlayerAttachments,
	CTELargeFunnel,
	CTEMetalSparks,
	CTEMuzzleFlash,
	CTEParticleSystem,
	CTEPhysicsProp,
	CTEPlayerAnimEvent,
	CTEPlayerDecal,
	CTEProjectedDecal,
	CTEShatterSurface,
	CTEShowLine,
	CTesla,
	CTESmoke,
	CTESparks,
	CTESprite,
	CTESpriteSpray,
	CTEWorldDecal,
	CTriggerPlayerMovement,
	CVGuiScreen,
	CVortigauntChargeToken,
	CVortigauntEffectDispel,
	CWaterBullet,
	CWaterLODControl,
	CWeapon357,
	CWeapon357_HL1,
	CWeapon_SLAM,
	CWeaponAlyxGun,
	CWeaponAnnabelle,
	CWeaponAR2,
	CWeaponBugBait,
	CWeaponCitizenPackage,
	CWeaponCitizenSuitcase,
	CWeaponCrossbow,
	CWeaponCrossbow_HL1,
	CWeaponCrowbar,
	CWeaponCrowbar_HL1,
	CWeaponCubemap,
	CWeaponCycler,
	CWeaponEgon,
	CWeaponFrag,
	CWeaponGauss,
	CWeaponGlock,
	CWeaponHandGrenade,
	CWeaponHgun,
	CWeaponHL2MPBase,
	CWeaponMP5,
	CWeaponOldManHarpoon,
	CWeaponPhysCannon,
	CWeaponPhysGun,
	CWeaponPistol,
	CWeaponRPG,
	CWeaponRPG_HL1,
	CWeaponSatchel,
	CWeaponShotgun,
	CWeaponShotgun_HL1,
	CWeaponSMG1,
	CWeaponSnark,
	CWeaponStunStick,
	CWeaponSWEP,
	CWeaponTripMine,
	CWorld,
	DustTrail,
	MovieExplosion,
	NextBotCombatCharacter,
	ParticleSmokeGrenade,
	RocketTrail,
	SmokeTrail,
	SporeExplosion,
}
#endif
