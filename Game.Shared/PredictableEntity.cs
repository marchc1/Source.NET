using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Networking;

using System.Drawing;
using System.Runtime.CompilerServices;

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

		//HolylibDumpDtReplica();
	}

	/// <summary>
	/// This tries to replicate https://github.com/RaphaelIT7/gmod-holylib/blob/efa27b047b93f2ff014289aa08f2c9003118418c/source/modules/networking.cpp#L1902
	/// It should result in exact output so it can be used in diff checks.
	/// </summary>
	public static void HolylibDumpDtReplica() {
		string dumpFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source.NET Datatables");
		Directory.CreateDirectory(dumpFilePath);
		HashSet<SendTable> writtenTables = [];
		int classIndex = 0;
		for (ServerClass? svc = ServerClass.Head; svc != null; svc = svc.Next) {
			string fileName = Path.Combine(dumpFilePath, $"{classIndex++}_{svc.NetworkName}-{svc.Table.GetName()}.txt");
			using FileStream file = File.Open(fileName, FileMode.Create, FileAccess.Write);
			using StreamWriter fileWriter = new(file);
			WriteSendTable(svc.Table, fileWriter, writtenTables);
		}

		using FileStream fullList = File.Open(Path.Combine(dumpFilePath, "fulllist.dt"), FileMode.Create, FileAccess.Write);
		using StreamWriter fullListWriter = new(fullList);
		classIndex = 0;
		for (ServerClass? svc = ServerClass.Head; svc != null; svc = svc.Next)
			fullListWriter.Write($"{svc.NetworkName} = {classIndex++}\n");
	}

	private static void WriteSendTable(SendTable table, StreamWriter file, HashSet<SendTable> writtenTables) {
		for (int i = 0; i < table.GetNumProps(); ++i) {
			SendProp prop = table.GetProp(i);

			WriteSendProp(prop, i, 0, file, writtenTables);
			file.Write("\n");
			writtenTables.Add(table);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void APPEND_IF_PFLAGS_CONTAINS_SPROP(ref string pFlags, PropFlags flags, PropFlags prop) {
		if ((flags & prop) != 0)
			pFlags += " " + prop switch {
				PropFlags.Unsigned => "UNSIGNED",
				PropFlags.Coord => "COORD",
				PropFlags.NoScale => "NOSCALE",
				PropFlags.RoundDown => "ROUNDDOWN",
				PropFlags.RoundUp => "ROUNDUP",
				PropFlags.Normal => "NORMAL",
				PropFlags.Exclude => "EXCLUDE",
				PropFlags.XYZExponent => "XYZE",
				PropFlags.InsideArray => "INSIDEARRAY",
				PropFlags.ProxyAlwaysYes => "PROXY_ALWAYS_YES",
				PropFlags.ChangesOften => "CHANGES_OFTEN",
				PropFlags.IsAVectorElem => "IS_A_VECTOR_ELEM",
				PropFlags.CoordMP => "COORD_MP",
				PropFlags.CoordMPLowPrecision => "COORD_MP_LOWPRECISION",
				PropFlags.CoordMPIntegral => "COORD_MP_INTEGRAL",
				// PropFlags.VarInt => "VARINT",
				PropFlags.EncodedAgainstTickCount => "ENCODED_AGAINST_TICKCOUNT",
				_ => throw new NotImplementedException()
			};
	}

	private static void WriteString(string str, int nIndent, StreamWriter pHandle) {
		for (int i = 0; i < nIndent; ++i)
			pHandle.Write("\t");

		pHandle.Write(str);
		pHandle.Write("\n");
	}



	private static void WriteSendTable(SendTable pTable, HashSet<SendTable> pWrittenTables) {
		if (pWrittenTables.Contains(pTable))
			return; // Already wrote it. Skipping...

		string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source.NET Datatables", $"{pTable.GetName()}.txt");

		using FileStream stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
		using StreamWriter pHandle = new(stream);

		for (int i = 0; i < pTable.GetNumProps(); ++i) {
			SendProp pProp = pTable.GetProp(i);

			WriteSendProp(pProp, i, 0, pHandle, pWrittenTables);
			pHandle.Write("\n");

			pWrittenTables.Add(pTable);
		}
	}

	private static void WriteSendProp(SendProp pProp, int nIndex, int nIndent, StreamWriter pHandle, HashSet<SendTable> pWrittenTables) {
		WriteString($"PropName: {pProp.GetName()}", nIndent, pHandle);
		WriteString($"ExcludeName: {pProp.GetExcludeDTName()}", nIndent, pHandle);

		string pFlags = "Flags:";
		var flags = pProp.GetFlags();
		if (flags == 0) {
			pFlags += " None";
		}
		else {
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.Unsigned);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.Coord);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.NoScale);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.RoundDown);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.RoundUp);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.Normal);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.Exclude);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.XYZExponent);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.InsideArray);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.ProxyAlwaysYes);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.ChangesOften);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.IsAVectorElem);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.CoordMP);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.CoordMPLowPrecision);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.CoordMPIntegral);
			APPEND_IF_PFLAGS_CONTAINS_SPROP(ref pFlags, flags, PropFlags.EncodedAgainstTickCount);
		}
		WriteString(pFlags, nIndent, pHandle);
		var pDataTableName = ($"Inherited: {((pProp.GetPropType() == SendPropType.DataTable && pProp.GetDataTable() != null) ? pProp.GetDataTable()!.GetName() : "NONE")}");

		if (pProp.GetDataTable() != null)
			WriteSendTable(pProp.GetDataTable(), pWrittenTables);
		WriteString(pDataTableName, nIndent, pHandle);

		string pType = "Type: ";
		switch (pProp.GetPropType()) {
			case SendPropType.Int:
				pType += ("DPT_Int");
				break;
			case SendPropType.Float:
				pType += ("DPT_Float");
				break;
			case SendPropType.Vector:
				pType += ("DPT_Vector");
				break;
			case SendPropType.VectorXY:
				// IMPORTANT: GMod uses this to network doubles! See SendPropTime64 which just redirects to SendPropVectorXY
				pType += ("DPT_VectorXY");
				break;
			case SendPropType.String:
				pType += ("DPT_String");
				break;
			case SendPropType.Array:
				pType += ("DPT_Array");
				break;
			case SendPropType.DataTable:
				pType += ("DPT_DataTable");
				break;
			case SendPropType.GModTable:
				pType += ("DPT_GMODTable");
				break;
			default:
				pType += ("UNKNOWN(");
				pType += (pProp.GetPropType());
				pType += (")");
				break;
		}
		WriteString(pType, nIndent, pHandle);

		WriteString($"NumElement: {pProp.GetNumElements()}", nIndent, pHandle);
		WriteString($"Bits: {pProp.Bits}", nIndent, pHandle);
		WriteString($"HighValue: {pProp.HighValue}", nIndent, pHandle);
		WriteString($"LowValue: {pProp.LowValue}", nIndent, pHandle);

		if (pProp.GetArrayProp() != null) {
			string pArrayProp = "ArrayProp: ";

			WriteString(pArrayProp, nIndent, pHandle);
			WriteSendProp(pProp.GetArrayProp()!, nIndex, nIndent + 1, pHandle, pWrittenTables);
		}
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
