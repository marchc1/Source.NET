using Game.Shared;

using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

using FIELD = Source.FIELD<Game.Server.HL2.PropCombineBall>;
namespace Game.Server.HL2;

[LinkEntityToClass("prop_combine_ball")]
[NetworkName("CPropCombineBall")]
public class PropCombineBall : BaseAnimating
{
	public static readonly SendTable DT_PropCombineBall = new(DT_BaseAnimating, [
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Held))),
		SendPropBool(FIELD.OF(nameof(Launched))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropCombineBall);

	[NetworkName("m_bEmit")]
	public bool Emit;
	[NetworkName("m_flRadius")]
	public float Radius;
	[NetworkName("m_bHeld")]
	public bool Held;
	[NetworkName("m_bLaunched")]
	public bool Launched;
}
