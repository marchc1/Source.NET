#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Game.Shared;

using FIELD = Source.FIELD<EffectData>;

public class EffectData
{
	public const int SUBINCH_PRECISION = 3;
	public const int MAX_EFFECT_FLAG_BITS = 8;
	public const int CUSTOM_COLOR_CP1 = 9;
	public const int CUSTOM_COLOR_CP2 = 10;
	public const int MAX_EFFECT_DISPATCH_STRING_BITS = 10;
	public const int MAX_EFFECT_DISPATCH_STRINGS = 1 << MAX_EFFECT_DISPATCH_STRING_BITS;

	public Vector3 Origin;
	public Vector3 Start;
	public Vector3 Normal;
	public QAngle Angles;
	public int Flags;
#if CLIENT_DLL
	public readonly BaseHandle Entity = new();
#else
#endif
	public int EntIndex;

	public float Scale;
	public float Magnitude;
	public float Radius;
	public int AttachmentIndex;
	public short SurfaceProp;
	public int EffectName;
	public int Material;
	public int DamageType;
	public int HitBox;
	public byte Color;

	public bool HasCustomColors;
	public ParticleEffectsColors CustomColors;
	public bool HasControlPoint1;
	public ParticleEffectsControlPoint ControlPoint1;
	public bool AllowOverride;

#if CLIENT_DLL
	public static readonly RecvTable DT_EffectData = new(nameof(DT_EffectData), [
		RecvPropFloat(FIELD.OF($"{nameof(Origin)}[0]")),
		RecvPropFloat(FIELD.OF($"{nameof(Origin)}[1]")),
		RecvPropFloat(FIELD.OF($"{nameof(Origin)}[2]")),
		RecvPropFloat(FIELD.OF($"{nameof(Start)}[0]")),
		RecvPropFloat(FIELD.OF($"{nameof(Start)}[1]")),
		RecvPropFloat(FIELD.OF($"{nameof(Start)}[2]")),
		RecvPropQAngles(FIELD.OF(nameof(Angles))),
		RecvPropVector(FIELD.OF(nameof(Normal))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropFloat(FIELD.OF(nameof(Magnitude))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(AttachmentIndex))),
		RecvPropIntWithMinusOneFlag(FIELD.OF(nameof(SurfaceProp))),
		RecvPropInt(FIELD.OF(nameof(EffectName))),
		RecvPropInt(FIELD.OF(nameof(Material))),
		RecvPropInt(FIELD.OF(nameof(DamageType))),
		RecvPropInt(FIELD.OF(nameof(HitBox))),
		RecvPropInt(FIELD.OF(nameof(EntIndex))), // << TODO: SIZEOF_IGNORE to force this and then we dont need the field on our end
		RecvPropInt(FIELD.OF(nameof(Color))),

		RecvPropFloat(FIELD.OF(nameof(Radius))),

		RecvPropBool(FIELD.OF(nameof(HasCustomColors))),
		RecvPropVector(FIELD.OF("CustomColors.Color1")),
		RecvPropVector(FIELD.OF("CustomColors.Color2")),
		
		RecvPropBool(FIELD.OF(nameof(HasControlPoint1))),
		RecvPropInt(FIELD.OF("ControlPoint1.ParticleAttachment")),
		RecvPropFloat(FIELD.OF("ControlPoint1.Offset[0]")),
		RecvPropFloat(FIELD.OF("ControlPoint1.Offset[1]")),
		RecvPropFloat(FIELD.OF("ControlPoint1.Offset[2]")),
		RecvPropBool(FIELD.OF(nameof(AllowOverride)))
	]);

#else
	public static readonly SendTable DT_EffectData = new(nameof(DT_EffectData), [
		SendPropFloat(FIELD.OF($"{nameof(Origin)}[0]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropFloat(FIELD.OF($"{nameof(Origin)}[1]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropFloat(FIELD.OF($"{nameof(Origin)}[2]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropFloat(FIELD.OF($"{nameof(Start)}[0]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropFloat(FIELD.OF($"{nameof(Start)}[1]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropFloat(FIELD.OF($"{nameof(Start)}[2]"), (int)BitBuffer.COORD_INTEGER_BITS + SUBINCH_PRECISION, 0, WorldSize.MIN_COORD_INTEGER, WorldSize.MAX_COORD_INTEGER),
		SendPropQAngles(FIELD.OF(nameof(Angles)), 7),
		SendPropVector(FIELD.OF(nameof(Normal)), 0, PropFlags.Normal | PropFlags.VarInt),
		SendPropInt(FIELD.OF(nameof(Flags)), MAX_EFFECT_FLAG_BITS, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Magnitude)), 12, PropFlags.RoundDown, 0.0f, 1023.0f),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(AttachmentIndex)), 5, PropFlags.Unsigned),
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(SurfaceProp)), 8, SendProxy_ShortAddOne),
		SendPropInt(FIELD.OF(nameof(EffectName)), MAX_EFFECT_DISPATCH_STRING_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Material)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(DamageType)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(HitBox)), 12, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EntIndex)), Constants.MAX_EDICT_BITS, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Color)), 8, PropFlags.Unsigned),

		SendPropFloat(FIELD.OF(nameof(Radius)), 10, PropFlags.RoundDown, 0.0f, 1023.0f),

		SendPropBool(FIELD.OF(nameof(HasCustomColors))),
		SendPropVector(FIELD.OF("CustomColors.Color1"), 8, 0, 0, 1),
		SendPropVector(FIELD.OF("CustomColors.Color2"), 8, 0, 0, 1),

		SendPropBool(FIELD.OF(nameof(HasControlPoint1))),
		SendPropInt(FIELD.OF("ControlPoint1.ParticleAttachment"), 5, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF("ControlPoint1.Offset[0]"), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropFloat(FIELD.OF("ControlPoint1.Offset[1]"), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropFloat(FIELD.OF("ControlPoint1.Offset[2]"), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(AllowOverride)))
	]);

#endif
}
#endif
