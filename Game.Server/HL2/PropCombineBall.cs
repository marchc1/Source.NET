using Game.Shared;

using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

using FIELD = Source.FIELD<Game.Server.HL2.PropCombineBall>;
namespace Game.Server.HL2;

public class PropCombineBall : BaseAnimating
{
	public static readonly SendTable DT_PropCombineBall = new(DT_BaseAnimating, [
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Held))),
		SendPropBool(FIELD.OF(nameof(Launched))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropCombineBall", DT_PropCombineBall).WithManualClassID(StaticClassIndices.CPropCombineBall);

	public bool Emit;
	public float Radius;
	public bool Held;
	public bool Launched;
}
