using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Flare>;
[NetworkName("CFlare")]
public partial class Flare : BaseCombatCharacter
{
	public static readonly SendTable DT_Flare = new(DT_BaseCombatCharacter, [
		SendPropFloat(NetworkVarFields.TimeBurnOut, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Scale, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Light),
		SendPropBool(NetworkVarFields.Smoke),
		SendPropBool(NetworkVarFields.PropFlare),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Flare);

	[NetworkName("m_flTimeBurnOut")]
	[NetworkVar] public partial float TimeBurnOut { get; set; }
	[NetworkName("m_flScale")]
	[NetworkVar] public partial float Scale { get; set; }
	[NetworkName("m_bLight")]
	[NetworkVar] public partial bool Light { get; set; }
	[NetworkName("m_bSmoke")]
	[NetworkVar] public partial bool Smoke { get; set; }
	[NetworkName("m_bPropFlare")]
	[NetworkVar] public partial bool PropFlare { get; set; }
}
