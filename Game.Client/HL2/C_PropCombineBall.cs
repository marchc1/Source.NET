using Game.Shared;

using Source;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using FIELD = Source.FIELD<Game.Client.HL2.C_PropCombineBall>;
namespace Game.Client.HL2;

public class C_PropCombineBall : C_BaseAnimating
{
	public static readonly RecvTable DT_PropCombineBall = new(DT_BaseAnimating, [
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
		RecvPropBool(FIELD.OF(nameof(Held))),
		RecvPropBool(FIELD.OF(nameof(Launched))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropCombineBall", DT_PropCombineBall).WithManualClassID(StaticClassIndices.CPropCombineBall);

	public Vector3 LastOrigin;
	public bool Emit;
	public float Radius;
	public bool Held;
	public bool Launched;
}
