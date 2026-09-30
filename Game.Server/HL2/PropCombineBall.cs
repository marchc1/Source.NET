using Game.Shared;

using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

using FIELD = Source.FIELD<Game.Server.HL2.PropCombineBall>;
namespace Game.Server.HL2;

[NetworkName("CPropCombineBall")]
public partial class PropCombineBall : BaseAnimating
{
	public static readonly SendTable DT_PropCombineBall = new(DT_BaseAnimating, [
		SendPropBool(NetworkVarFields.Emit),
		SendPropFloat(NetworkVarFields.Radius, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Held),
		SendPropBool(NetworkVarFields.Launched),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropCombineBall);

	[NetworkName("m_bEmit")]
	[NetworkVar] public partial bool Emit { get; set; }
	[NetworkName("m_flRadius")]
	[NetworkVar] public partial float Radius { get; set; }
	[NetworkName("m_bHeld")]
	[NetworkVar] public partial bool Held { get; set; }
	[NetworkName("m_bLaunched")]
	[NetworkVar] public partial bool Launched { get; set; }
}
