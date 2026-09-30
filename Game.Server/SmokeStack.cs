using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SmokeStack>;

public struct SmokeStackLightInfo{
	[NetworkName("m_vPos")]
	public Vector3 Pos;
	[NetworkName("m_vColor")]
	public Vector3 Color;
	[NetworkName("m_flIntensity")]
	public float Intensity;
}

[NetworkName("CSmokeStack")]
public partial class SmokeStack : BaseParticleEntity
{
	public static readonly SendTable DT_SmokeStack = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpreadSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Speed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Rate, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.JetLength, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Emit),
		SendPropFloat(NetworkVarFields.BaseSpread, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.RollSpeed, 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("DirLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("AmbientLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.Wind, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Twist, 0, PropFlags.NoScale),
		SendPropIntWithMinusOneFlag(NetworkVarFields.MaterialModel, 16),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SmokeStack);

	[NetworkName("m_SpreadSpeed")]
	[NetworkVar] public partial float SpreadSpeed { get; set; }
	[NetworkName("m_Speed")]
	[NetworkVar] public new partial float Speed { get; set; }
	[NetworkName("m_StartSize")]
	[NetworkVar] public partial float StartSize { get; set; }
	[NetworkName("m_EndSize")]
	[NetworkVar] public partial float EndSize { get; set; }
	[NetworkName("m_Rate")]
	[NetworkVar] public partial float Rate { get; set; }
	[NetworkName("m_JetLength")]
	[NetworkVar] public partial float JetLength { get; set; }
	[NetworkName("m_bEmit")]
	[NetworkVar] public partial bool Emit { get; set; }
	[NetworkName("m_flBaseSpread")]
	[NetworkVar] public partial float BaseSpread { get; set; }
	[NetworkName("m_flRollSpeed")]
	[NetworkVar] public partial float RollSpeed { get; set; }

	[NetworkName("m_AmbientLight")]
	public SmokeStackLightInfo AmbientLight;
	[NetworkName("m_DirLight")]
	public SmokeStackLightInfo DirLight;

	[NetworkName("m_vWind")]
	[NetworkVar] public partial Vector3 Wind { get; set; }
	[NetworkName("m_flTwist")]
	[NetworkVar] public partial float Twist { get; set; }
	[NetworkName("m_iMaterialModel")]
	[NetworkVar] public partial int MaterialModel { get; set; }
}
