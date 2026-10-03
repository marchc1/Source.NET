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

public static class LuaVector
{
	public static readonly LuaClass LC_Vector = new("Vector", LuaType.Vector, null, null);

	static readonly LuaClassFunction Vector___index__Factory = LC_Vector.Add("__index", Vector____index);
	static readonly LuaClassFunction Vector___newindex__Factory = LC_Vector.Add("__newindex", Vector____newindex);
	static readonly LuaClassFunction Vector___tostring__Factory = LC_Vector.Add("__tostring", Vector____tostring);
	static readonly LuaClassFunction Vector_Length__Factory = LC_Vector.Add("Length", Vector__Length);
	static readonly LuaClassFunction Vector___eq__Factory = LC_Vector.Add("__eq", Vector____eq);
	static readonly LuaClassFunction Vector___add__Factory = LC_Vector.Add("__add", Vector____add);
	static readonly LuaClassFunction Vector_Add__Factory = LC_Vector.Add("Add", Vector__Add);
	static readonly LuaClassFunction Vector_Sub__Factory = LC_Vector.Add("Sub", Vector__Sub);
	static readonly LuaClassFunction Vector_Mul__Factory = LC_Vector.Add("Mul", Vector__Mul);
	static readonly LuaClassFunction Vector_Div__Factory = LC_Vector.Add("Div", Vector__Div);
	static readonly LuaClassFunction Vector___sub__Factory = LC_Vector.Add("__sub", Vector____sub);
	static readonly LuaClassFunction Vector___unm__Factory = LC_Vector.Add("__unm", Vector____unm);
	static readonly LuaClassFunction Vector___mul__Factory = LC_Vector.Add("__mul", Vector____mul);
	static readonly LuaClassFunction Vector___div__Factory = LC_Vector.Add("__div", Vector____div);
	static readonly LuaClassFunction Vector_Normalize__Factory = LC_Vector.Add("Normalize", Vector__Normalize);
	static readonly LuaClassFunction Vector_GetNormal__Factory = LC_Vector.Add("GetNormal", Vector__GetNormal);
	static readonly LuaClassFunction Vector_GetNormalized__Factory = LC_Vector.Add("GetNormalized", Vector__GetNormal);
	static readonly LuaClassFunction Vector_Dot__Factory = LC_Vector.Add("Dot", Vector__Dot);
	static readonly LuaClassFunction Vector_DotProduct__Factory = LC_Vector.Add("DotProduct", Vector__Dot);
	static readonly LuaClassFunction Vector_Cross__Factory = LC_Vector.Add("Cross", Vector__Cross);
	static readonly LuaClassFunction Vector_Distance__Factory = LC_Vector.Add("Distance", Vector__Distance);
	static readonly LuaClassFunction Vector_Angle__Factory = LC_Vector.Add("Angle", Vector__Angle);
	static readonly LuaClassFunction Vector_AngleEx__Factory = LC_Vector.Add("AngleEx", Vector__AngleEx);
	static readonly LuaClassFunction Vector_Rotate__Factory = LC_Vector.Add("Rotate", Vector__Rotate);
	static readonly LuaClassFunction Vector_Length2D__Factory = LC_Vector.Add("Length2D", Vector__Length2D);
	static readonly LuaClassFunction Vector_LengthSqr__Factory = LC_Vector.Add("LengthSqr", Vector__LengthSqr);
	static readonly LuaClassFunction Vector_Length2DSqr__Factory = LC_Vector.Add("Length2DSqr", Vector__Length2DSqr);
	static readonly LuaClassFunction Vector_Distance2D__Factory = LC_Vector.Add("Distance2D", Vector__Distance2D);
	static readonly LuaClassFunction Vector_Distance2DSqr__Factory = LC_Vector.Add("Distance2DSqr", Vector__Distance2DSqr);
	static readonly LuaClassFunction Vector_DistToSqr__Factory = LC_Vector.Add("DistToSqr", Vector__DistToSqr);
	static readonly LuaClassFunction Vector_WithinAABox__Factory = LC_Vector.Add("WithinAABox", Vector__WithinAABox);
	static readonly LuaClassFunction Vector_IsZero__Factory = LC_Vector.Add("IsZero", Vector__IsZero);
	static readonly LuaClassFunction Vector_IsEqualTol__Factory = LC_Vector.Add("IsEqualTol", Vector__IsEqualTol);
	static readonly LuaClassFunction Vector_Zero__Factory = LC_Vector.Add("Zero", Vector__Zero);
	static readonly LuaClassFunction Vector_Set__Factory = LC_Vector.Add("Set", Vector__Set);
	static readonly LuaClassFunction Vector_Unpack__Factory = LC_Vector.Add("Unpack", Vector__Unpack);
	static readonly LuaClassFunction Vector_SetUnpacked__Factory = LC_Vector.Add("SetUnpacked", Vector__SetUnpacked);
	static readonly LuaClassFunction Vector_ToTable__Factory = LC_Vector.Add("ToTable", Vector__ToTable);
	static readonly LuaClassFunction Vector_Random__Factory = LC_Vector.Add("Random", Vector__Random);
	static readonly LuaClassFunction Vector_Negate__Factory = LC_Vector.Add("Negate", Vector__Negate);
	static readonly LuaClassFunction Vector_GetNegated__Factory = LC_Vector.Add("GetNegated", Vector__GetNegated);
#if CLIENT_DLL
	static readonly LuaClassFunction Vector_ToScreen__Factory = LC_Vector.Add("ToScreen", Vector__ToScreen);
#endif

	static readonly LuaLibraryFunction worker__GLobal__Vector = LuaGlobalLibrary.Add("Vector", Vector);
	static readonly LuaLibraryFunction worker__GLobal__OrderVectors = LuaGlobalLibrary.Add("OrderVectors", OrderVectors);
	static readonly LuaLibraryFunction worker__GLobal__LerpVector = LuaGlobalLibrary.Add("LerpVector", LerpVector);

	public static ref Vector3 Get_Vector(int stackPos) => ref LC_Vector.GetValue<Vector3>(stackPos);

	public static void Push_Vector(in Vector3 vec) => g_Lua!.PushVector(in vec);

	static char FirstChar(ReadOnlySpan<char> str) => str.IsEmpty ? '\0' : str[0];

	static int Vector____index(ILuaInterface lua) {
		if (g_Lua!.FindOnObjectsMetaTable(1, 2))
			return 1;

		ref Vector3 vec = ref Get_Vector(1);
		int index;
		if (g_Lua.GetType(2) == LuaType.Number) {
			index = (int)g_Lua.CheckNumber(2) - 1;
			if (index < 0 || index > 2)
				return 0;
		}
		else {
			switch (FirstChar(g_Lua.CheckString(2))) {
				case 'X': case 'r': case 'x': index = 0; break;
				case 'Y': case 'g': case 'y': index = 1; break;
				case 'Z': case 'b': case 'z': index = 2; break;
				default: return 0;
			}
		}

		g_Lua.PushNumber(vec[index]);
		return 1;
	}

	static int Vector____newindex(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
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
				case 'X': case 'r': case 'x': index = 0; break;
				case 'Y': case 'g': case 'y': index = 1; break;
				case 'Z': case 'b': case 'z': index = 2; break;
				default:
					g_Lua.CheckNumber(3);
					return 0;
			}
			value = g_Lua.CheckNumber(3);
		}

		vec[index] = (float)value;
		return 0;
	}

	static int Vector____tostring(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		string str = $"{FormatFixed(vec.X, 6)} {FormatFixed(vec.Y, 6)} {FormatFixed(vec.Z, 6)}";
		g_Lua!.PushString(str.Length > 127 ? str[..127] : str);
		return 1;
	}

	static int Vector__Length(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		g_Lua!.PushNumber(MathF.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z));
		return 1;
	}

	static int Vector____eq(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		g_Lua!.PushBool(a.X == b.X && a.Y == b.Y && a.Z == b.Z);
		return 1;
	}

	static int Vector__Add(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		a.X += b.X;
		a.Y += b.Y;
		a.Z += b.Z;
		return 0;
	}

	static int Vector__Sub(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		a.X -= b.X;
		a.Y -= b.Y;
		a.Z -= b.Z;
		return 0;
	}

	static int Vector__Mul(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		LuaType type = g_Lua!.GetType(2);
		if (type == LuaType.Vector) {
			ref Vector3 other = ref Get_Vector(2);
			vec.X *= other.X;
			vec.Y *= other.Y;
			vec.Z *= other.Z;
		}
		// todo: type == LuaType.Matrix: *vec = *Get_VMatrix(2) * *vec;
		else {
			float scale = (float)g_Lua.CheckNumber(2);
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
		}
		return 0;
	}

	static int Vector__Div(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		if (g_Lua!.GetType(2) == LuaType.Vector) {
			ref Vector3 other = ref Get_Vector(2);
			vec.X /= other.X;
			vec.Y /= other.Y;
			vec.Z /= other.Z;
		}
		else {
			float oofl = 1.0f / (float)g_Lua.CheckNumber(2);
			vec.X *= oofl;
			vec.Y *= oofl;
			vec.Z *= oofl;
		}
		return 0;
	}

	static int Vector____add(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		Push_Vector(new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z));
		return 1;
	}

	static int Vector____unm(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		Push_Vector(new Vector3(-vec.X, -vec.Y, -vec.Z));
		return 1;
	}

	static int Vector____sub(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		Push_Vector(new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z));
		return 1;
	}

	static int Vector____div(ILuaInterface lua) {
		LuaType type = g_Lua!.GetType(1);
		if (type == LuaType.Vector && g_Lua.GetType(2) == LuaType.Vector) {
			ref Vector3 a = ref Get_Vector(1);
			ref Vector3 b = ref Get_Vector(2);
			Push_Vector(new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z));
			return 1;
		}

		bool numberFirst = type == LuaType.Number;
		ref Vector3 vec = ref Get_Vector(numberFirst ? 2 : 1);
		float oofl = 1.0f / (float)g_Lua.CheckNumber(numberFirst ? 1 : 2);
		Push_Vector(new Vector3(vec.X * oofl, vec.Y * oofl, vec.Z * oofl));
		return 1;
	}

	static int Vector____mul(ILuaInterface lua) {
		LuaType type = g_Lua!.GetType(1);
		if (type == LuaType.Vector && g_Lua.GetType(2) == LuaType.Vector) {
			ref Vector3 a = ref Get_Vector(1);
			ref Vector3 b = ref Get_Vector(2);
			Push_Vector(new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z));
			return 1;
		}

		bool numberFirst = type == LuaType.Number;
		ref Vector3 vec = ref Get_Vector(numberFirst ? 2 : 1);
		float scale = (float)g_Lua.CheckNumber(numberFirst ? 1 : 2);
		Push_Vector(new Vector3(vec.X * scale, vec.Y * scale, vec.Z * scale));
		return 1;
	}

	static int Vector__Cross(ILuaInterface lua) {
		Vector3 a = Get_Vector(1);
		Vector3 b = Get_Vector(2);
		Push_Vector(new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X));
		return 1;
	}

	static int Vector__Normalize(ILuaInterface lua) {
		VectorNormalize(ref Get_Vector(1));
		return 0;
	}

	static int Vector__GetNormal(ILuaInterface lua) {
		Push_Vector(Get_Vector(1));
		VectorNormalize(ref Get_Vector(-1));
		return 1;
	}

	static int Vector__Dot(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		g_Lua!.PushNumber(a.X * b.X + a.Y * b.Y + a.Z * b.Z);
		return 1;
	}

	static int Vector__Distance(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		float dx = a.X - b.X;
		float dy = a.Y - b.Y;
		float dz = a.Z - b.Z;
		g_Lua!.PushNumber(MathF.Sqrt(dx * dx + dy * dy + dz * dz));
		return 1;
	}

	static int Vector__Angle(ILuaInterface lua) {
		Vector3 forward = Get_Vector(1);
		VectorNormalize(ref forward);
		VectorAngles(forward, out QAngle angles);
		LuaAngle.Push_Angle(angles);
		return 1;
	}

	static int Vector__AngleEx(ILuaInterface lua) {
		Vector3 forward = Get_Vector(1);
		VectorNormalize(ref forward);
		Vector3 up = Get_Vector(2);
		VectorNormalize(ref up);
		VectorAngles(forward, up, out QAngle angles);
		LuaAngle.Push_Angle(angles);
		return 1;
	}

	static int Vector__Rotate(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		AngleMatrix(LuaAngle.Get_Angle(2), out Matrix3x4 matrix);
		VectorRotate(vec, matrix, out Vector3 rotated);
		vec = rotated;
		return 0;
	}

	static int Vector__Length2D(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		g_Lua!.PushNumber(MathF.Sqrt(vec.X * vec.X + vec.Y * vec.Y));
		return 1;
	}

	static int Vector__LengthSqr(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		g_Lua!.PushNumber(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
		return 1;
	}

	static int Vector__Length2DSqr(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		g_Lua!.PushNumber(vec.X * vec.X + vec.Y * vec.Y);
		return 1;
	}

	static int Vector__Distance2D(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		float dx = a.X - b.X;
		float dy = a.Y - b.Y;
		g_Lua!.PushNumber(MathF.Sqrt(dx * dx + dy * dy));
		return 1;
	}

	static int Vector__Distance2DSqr(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		float dx = a.X - b.X;
		float dy = a.Y - b.Y;
		g_Lua!.PushNumber(dx * dx + dy * dy);
		return 1;
	}

	static int Vector__DistToSqr(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		float dx = a.X - b.X;
		float dy = a.Y - b.Y;
		float dz = a.Z - b.Z;
		g_Lua!.PushNumber(dx * dx + dy * dy + dz * dz);
		return 1;
	}

	static void OrderVectors(in Vector3 a, in Vector3 b, out Vector3 mins, out Vector3 maxs) {
		mins = default;
		maxs = default;
		for (int i = 0; i < 3; i++) {
			mins[i] = a[i] < b[i] ? a[i] : b[i];
			maxs[i] = a[i] > b[i] ? a[i] : b[i];
		}
	}

	static int Vector__WithinAABox(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		ref Vector3 boxStart = ref Get_Vector(2);
		ref Vector3 boxEnd = ref Get_Vector(3);
		OrderVectors(boxStart, boxEnd, out Vector3 mins, out Vector3 maxs);
		g_Lua!.PushBool(vec.X >= mins.X && maxs.X >= vec.X && vec.Y >= mins.Y && maxs.Y >= vec.Y && vec.Z >= mins.Z && maxs.Z >= vec.Z);
		return 1;
	}

	static int Vector__IsZero(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		const float tolerance = 0.01f;
		g_Lua!.PushBool(vec.X > -tolerance && vec.X < tolerance && vec.Y > -tolerance && vec.Y < tolerance && vec.Z > -tolerance && vec.Z < tolerance);
		return 1;
	}

	static int Vector__IsEqualTol(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		float tolerance = (float)g_Lua!.CheckNumber(3);
		g_Lua.PushBool(!(MathF.Abs(a.X - b.X) > tolerance) && !(MathF.Abs(a.Y - b.Y) > tolerance) && tolerance >= MathF.Abs(a.Z - b.Z));
		return 1;
	}

	static int Vector__Zero(ILuaInterface lua) {
		Get_Vector(1) = default;
		return 0;
	}

	static int Vector__Set(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		vec = Get_Vector(2);
		return 0;
	}

	static int Vector__Unpack(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		g_Lua!.PushNumber(vec.X);
		g_Lua.PushNumber(vec.Y);
		g_Lua.PushNumber(vec.Z);
		return 3;
	}

	static int Vector__SetUnpacked(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		vec.X = (float)g_Lua!.CheckNumber(2);
		vec.Y = (float)g_Lua.CheckNumber(3);
		vec.Z = (float)g_Lua.CheckNumber(4);
		return 0;
	}

	static int Vector__ToTable(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		LuaTable table = new(null, 3);
		table.SetMemberDouble(1, vec.X);
		table.SetMemberDouble(2, vec.Y);
		table.SetMemberDouble(3, vec.Z);
		table.Push();
		table.UnReference();
		return 1;
	}

	static int Vector__Random(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		float minVal = (float)g_Lua!.CheckNumberOpt(2, -1.0);
		float range = (float)g_Lua.CheckNumberOpt(3, 1.0) - minVal;
		vec.X = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		vec.Y = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		vec.Z = RandomInt(0, 0x7FFF) * (1.0f / VALVE_RAND_MAX) * range + minVal;
		return 0;
	}

	static int Vector__Negate(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		vec.X = -vec.X;
		vec.Y = -vec.Y;
		vec.Z = -vec.Z;
		return 0;
	}

	static int Vector__GetNegated(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		Push_Vector(new Vector3(-vec.X, -vec.Y, -vec.Z));
		return 1;
	}

#if CLIENT_DLL
	static int Vector__ToScreen(ILuaInterface lua) {
		ref Vector3 vec = ref Get_Vector(1);
		bool behind = ScreenTransform(vec, out Vector3 screen);
		LuaTable table = new(null, 0);
		table.SetMember("x", (screen.X + 1.0f) * 0.5f * ScreenWidth());
		table.SetMember("y", (0.5f - screen.Y * 0.5f) * ScreenHeight());
		table.SetMember("visible", !behind);
		table.Push();
		table.UnReference();
		return 1;
	}
#endif

	static int OrderVectors(ILuaInterface lua) {
		ref Vector3 a = ref Get_Vector(1);
		ref Vector3 b = ref Get_Vector(2);
		OrderVectors(a, b, out Vector3 mins, out Vector3 maxs);
		a = mins;
		b = maxs;
		return 0;
	}

	static int LerpVector(ILuaInterface lua) {
		float frac = (float)g_Lua!.CheckNumber(1);
		ref Vector3 from = ref Get_Vector(2);
		ref Vector3 to = ref Get_Vector(3);
		Push_Vector(new Vector3(
			(to.X - from.X) * frac + from.X,
			(to.Y - from.Y) * frac + from.Y,
			(to.Z - from.Z) * frac + from.Z
		));
		return 1;
	}

	static int Vector(ILuaInterface lua) {
		int top = lua.Top();
		Push_Vector(default);
		ref Vector3 vec = ref Get_Vector(-1);

		LuaType type = lua.GetType(1);
		if (type == LuaType.String) {
			Span<float> values = stackalloc float[3];
			if (ScanFloats(g_Lua!.CheckString(1), values) == 3) {
				vec = new(values[0], values[1], values[2]);
				return 1;
			}
		}
		else if (type == LuaType.Vector && top > 0) {
			vec = Get_Vector(1);
			return 1;
		}
		else if (type != LuaType.Number
			&& g_Lua!.GetType(2) != LuaType.Number && g_Lua.GetType(2) != LuaType.String
			&& g_Lua.GetType(3) != LuaType.Number && g_Lua.GetType(3) != LuaType.String) {
			vec = default;
			return 1;
		}

		double z = lua.GetNumber(3);
		double y = lua.GetNumber(2);
		double x = lua.GetNumber(1);
		vec = new((float)x, (float)y, (float)z);
		return 1;
	}
}
#endif
