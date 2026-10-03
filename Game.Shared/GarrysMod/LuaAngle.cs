#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Common.Mathematics.MathLib;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class LuaAngle
{
	public static readonly LuaClass LC_Angle = new("Angle", LuaType.Angle, null, null);

	static readonly LuaClassFunction Angle___newindex__Factory = LC_Angle.Add("__newindex", Angle____newindex);
	static readonly LuaClassFunction Angle___index__Factory = LC_Angle.Add("__index", Angle____index);
	static readonly LuaClassFunction Angle___tostring__Factory = LC_Angle.Add("__tostring", Angle____tostring);
	static readonly LuaClassFunction Angle___add__Factory = LC_Angle.Add("__add", Angle____add);
	static readonly LuaClassFunction Angle___sub__Factory = LC_Angle.Add("__sub", Angle____sub);
	static readonly LuaClassFunction Angle___unm__Factory = LC_Angle.Add("__unm", Angle____unm);
	static readonly LuaClassFunction Angle___mul__Factory = LC_Angle.Add("__mul", Angle____mul);
	static readonly LuaClassFunction Angle___div__Factory = LC_Angle.Add("__div", Angle____div);
	static readonly LuaClassFunction Angle_Add__Factory = LC_Angle.Add("Add", Angle__Add);
	static readonly LuaClassFunction Angle_Sub__Factory = LC_Angle.Add("Sub", Angle__Sub);
	static readonly LuaClassFunction Angle_Mul__Factory = LC_Angle.Add("Mul", Angle__Mul);
	static readonly LuaClassFunction Angle_Div__Factory = LC_Angle.Add("Div", Angle__Div);
	static readonly LuaClassFunction Angle_IsEqualTol__Factory = LC_Angle.Add("IsEqualTol", Angle__IsEqualTol);
	static readonly LuaClassFunction Angle_Forward__Factory = LC_Angle.Add("Forward", Angle__Forward);
	static readonly LuaClassFunction Angle_Right__Factory = LC_Angle.Add("Right", Angle__Right);
	static readonly LuaClassFunction Angle_Up__Factory = LC_Angle.Add("Up", Angle__Up);
	static readonly LuaClassFunction Angle_RotateAroundAxis__Factory = LC_Angle.Add("RotateAroundAxis", Angle__RotateAroundAxis);
	static readonly LuaClassFunction Angle___eq__Factory = LC_Angle.Add("__eq", Angle____eq);
	static readonly LuaClassFunction Angle_Normalize__Factory = LC_Angle.Add("Normalize", Angle__Normalize);
	static readonly LuaClassFunction Angle_Set__Factory = LC_Angle.Add("Set", Angle__Set);
	static readonly LuaClassFunction Angle_Zero__Factory = LC_Angle.Add("Zero", Angle__Zero);
	static readonly LuaClassFunction Angle_IsZero__Factory = LC_Angle.Add("IsZero", Angle__IsZero);
	static readonly LuaClassFunction Angle_Unpack__Factory = LC_Angle.Add("Unpack", Angle__Unpack);
	static readonly LuaClassFunction Angle_Random__Factory = LC_Angle.Add("Random", Angle__Random);
	static readonly LuaClassFunction Angle_SetUnpacked__Factory = LC_Angle.Add("SetUnpacked", Angle__SetUnpacked);
	static readonly LuaClassFunction Angle_ToTable__Factory = LC_Angle.Add("ToTable", Angle__ToTable);

	static readonly LuaLibraryFunction worker__GLobal__Angle = LuaGlobalLibrary.Add("Angle", Angle);
	static readonly LuaLibraryFunction worker__GLobal__LerpAngle = LuaGlobalLibrary.Add("LerpAngle", LerpAngle);

	public static ref QAngle Get_Angle(int stackPos) => ref LC_Angle.GetValue<QAngle>(stackPos);

	public static void Push_Angle(in QAngle ang) => g_Lua!.PushAngle(in ang);

	static char FirstChar(ReadOnlySpan<char> str) => str.IsEmpty ? '\0' : str[0];

	static int Angle____newindex(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		int index;
		double value;
		if (g_Lua!.GetType(2) == LuaType.Number) {
			index = (int)g_Lua.CheckNumber(2) - 1;
			value = g_Lua.CheckNumber(3);
			if (index < 0 || index > 2)
				return 0;
		}
		else {
			switch (FirstChar(g_Lua.CheckString(2))) {
				case 'P': case 'X': case 'p': case 'x': index = 0; break;
				case 'Y': case 'y': index = 1; break;
				case 'R': case 'Z': case 'r': case 'z': index = 2; break;
				default:
					g_Lua.CheckNumber(3);
					return 0;
			}
			value = g_Lua.CheckNumber(3);
		}

		ang[index] = (float)value;
		return 0;
	}

	static int Angle____index(ILuaInterface lua) {
		if (g_Lua!.FindOnObjectsMetaTable(1, 2))
			return 1;

		ref QAngle ang = ref Get_Angle(1);
		int index;
		if (g_Lua.GetType(2) == LuaType.Number) {
			index = (int)g_Lua.CheckNumber(2) - 1;
			if (index < 0 || index > 2)
				return 0;
		}
		else {
			switch (FirstChar(g_Lua.CheckString(2))) {
				case 'P': case 'X': case 'p': case 'x': index = 0; break;
				case 'Y': case 'y': index = 1; break;
				case 'R': case 'Z': case 'r': case 'z': index = 2; break;
				default: return 0;
			}
		}

		g_Lua.PushNumber(ang[index]);
		return 1;
	}

	static int Angle____tostring(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		string str = $"{FormatFixed(ang.X, 3)} {FormatFixed(ang.Y, 3)} {FormatFixed(ang.Z, 3)}";
		g_Lua!.PushString(str.Length > 63 ? str[..63] : str);
		return 1;
	}

	static int Angle____add(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		Push_Angle(new QAngle(a.X + b.X, a.Y + b.Y, a.Z + b.Z));
		return 1;
	}

	static int Angle____sub(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		Push_Angle(new QAngle(a.X - b.X, a.Y - b.Y, a.Z - b.Z));
		return 1;
	}

	static int Angle____unm(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		Push_Angle(new QAngle(-ang.X, -ang.Y, -ang.Z));
		return 1;
	}

	static int Angle____mul(ILuaInterface lua) {
		bool numberFirst = g_Lua!.GetType(1) == LuaType.Number;
		ref QAngle ang = ref Get_Angle(numberFirst ? 2 : 1);
		float scale = (float)g_Lua.CheckNumber(numberFirst ? 1 : 2);
		Push_Angle(new QAngle(ang.X * scale, ang.Y * scale, ang.Z * scale));
		return 1;
	}

	static int Angle____div(ILuaInterface lua) {
		bool numberFirst = g_Lua!.GetType(1) == LuaType.Number;
		ref QAngle ang = ref Get_Angle(numberFirst ? 2 : 1);
		float oofl = 1.0f / (float)g_Lua.CheckNumber(numberFirst ? 1 : 2);
		Push_Angle(new QAngle(ang.X * oofl, ang.Y * oofl, ang.Z * oofl));
		return 1;
	}

	static int Angle__Add(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		a.X += b.X;
		a.Y += b.Y;
		a.Z += b.Z;
		return 0;
	}

	static int Angle__Sub(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		a.X -= b.X;
		a.Y -= b.Y;
		a.Z -= b.Z;
		return 0;
	}

	static int Angle__Mul(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		float scale = (float)g_Lua!.CheckNumber(2);
		ang.X *= scale;
		ang.Y *= scale;
		ang.Z *= scale;
		return 0;
	}

	static int Angle__Div(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		float oofl = 1.0f / (float)g_Lua!.CheckNumber(2);
		ang.X *= oofl;
		ang.Y *= oofl;
		ang.Z *= oofl;
		return 0;
	}

	static int Angle__IsEqualTol(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		float tolerance = (float)g_Lua!.CheckNumber(3);
		g_Lua.PushBool(!(MathF.Abs(a.X - b.X) > tolerance) && !(MathF.Abs(a.Y - b.Y) > tolerance) && tolerance >= MathF.Abs(a.Z - b.Z));
		return 1;
	}

	static int Angle__Forward(ILuaInterface lua) {
		AngleVectors(Get_Angle(1), out Vector3 forward);
		LuaVector.Push_Vector(forward);
		return 1;
	}

	static int Angle__Right(ILuaInterface lua) {
		AngleVectors(Get_Angle(1), out _, out Vector3 right, out _);
		LuaVector.Push_Vector(right);
		return 1;
	}

	static int Angle__Up(ILuaInterface lua) {
		AngleVectors(Get_Angle(1), out _, out _, out Vector3 up);
		LuaVector.Push_Vector(up);
		return 1;
	}

	static int Angle__RotateAroundAxis(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		ref Vector3 axis = ref LuaVector.Get_Vector(2);
		float degrees = (float)g_Lua!.CheckNumber(3);

		AngleMatrix(ang, out Matrix3x4 matrix);
		VectorIRotate(axis, matrix, out Vector3 localAxis);
		AxisAngleQuaternion(localAxis, degrees, out Quaternion q);
		QuaternionMatrix(q, vec3_origin, out Matrix3x4 rotation);
		ConcatTransforms(matrix, rotation, out Matrix3x4 result);
		MatrixAngles(result, out QAngle rotated);
		ang = rotated;
		return 0;
	}

	static int Angle____eq(ILuaInterface lua) {
		ref QAngle a = ref Get_Angle(1);
		ref QAngle b = ref Get_Angle(2);
		g_Lua!.PushBool(a.X == b.X && a.Y == b.Y && a.Z == b.Z);
		return 1;
	}

	static int Angle__Normalize(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		ang.X = AngleNormalize(ang.X);
		ang.Y = AngleNormalize(ang.Y);
		ang.Z = AngleNormalize(ang.Z);
		return 0;
	}

	static int Angle__Set(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		ang = Get_Angle(2);
		return 0;
	}

	static int Angle__Zero(ILuaInterface lua) {
		Get_Angle(1) = default;
		return 0;
	}

	static int Angle__IsZero(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		g_Lua!.PushBool(ang.X == 0 && ang.Y == 0 && ang.Z == 0);
		return 1;
	}

	static int Angle__Unpack(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		g_Lua!.PushNumber(ang.X);
		g_Lua.PushNumber(ang.Y);
		g_Lua.PushNumber(ang.Z);
		return 3;
	}

	static int Angle__Random(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		float minVal = (float)g_Lua!.CheckNumberOpt(2, -360.0);
		float range = (float)g_Lua.CheckNumberOpt(3, 360.0) - minVal;
		ang.X = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		ang.Y = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		ang.Z = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		return 0;
	}

	static int Angle__SetUnpacked(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		ang.X = (float)g_Lua!.CheckNumber(2);
		ang.Y = (float)g_Lua.CheckNumber(3);
		ang.Z = (float)g_Lua.CheckNumber(4);
		return 0;
	}

	static int Angle__ToTable(ILuaInterface lua) {
		ref QAngle ang = ref Get_Angle(1);
		LuaTable table = new(null, 3);
		table.SetMemberDouble(1, ang.X);
		table.SetMemberDouble(2, ang.Y);
		table.SetMemberDouble(3, ang.Z);
		table.Push();
		table.UnReference();
		return 1;
	}

	static int Angle(ILuaInterface lua) {
		int top = lua.Top();
		Push_Angle(default);
		ref QAngle ang = ref Get_Angle(-1);

		LuaType type = lua.GetType(1);
		if (type == LuaType.String) {
			Span<float> values = stackalloc float[3];
			if (ScanFloats(g_Lua!.CheckString(1), values) == 3) {
				ang = new(values[0], values[1], values[2]);
				return 1;
			}
		}
		else if (type == LuaType.Angle && top > 0) {
			ang = Get_Angle(1);
			return 1;
		}
		else if (type != LuaType.Number
			&& g_Lua!.GetType(2) != LuaType.Number && g_Lua.GetType(2) != LuaType.String
			&& g_Lua.GetType(3) != LuaType.Number && g_Lua.GetType(3) != LuaType.String) {
			ang = default;
			return 1;
		}

		double z = lua.GetNumber(3);
		double y = lua.GetNumber(2);
		double x = lua.GetNumber(1);
		ang = new((float)x, (float)y, (float)z);
		return 1;
	}

	static int LerpAngle(ILuaInterface lua) {
		float frac = (float)g_Lua!.CheckNumber(1);
		ref QAngle from = ref Get_Angle(2);
		ref QAngle to = ref Get_Angle(3);

		if (to.X == from.X && to.Y == from.Y && to.Z == from.Z) {
			Push_Angle(from);
			return 1;
		}

		AngleQuaternion(from, out Quaternion src);
		AngleQuaternion(to, out Quaternion dest);
		QuaternionBlend(src, dest, frac, out Quaternion result);
		QuaternionAngles(result, out QAngle angles);
		Push_Angle(angles);
		return 1;
	}
}
#endif
