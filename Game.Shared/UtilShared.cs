#if CLIENT_DLL || GAME_DLL
global using static Game.Util_Globals;
#endif

using Source.Common.Hashing;
using Source.Common.Mathematics;

using System.Drawing.Drawing2D;
using System.Numerics;

namespace Game;
#if CLIENT_DLL || GAME_DLL

public static partial class Util_Globals {
	public static int SeedFileLineHash(int seedvalue, ReadOnlySpan<char> sharedname, int additionalSeed) {
		CRC32_t retval = default;

		CRC32.Init(ref retval);

		CRC32.ProcessBuffer(ref retval, seedvalue);
		CRC32.ProcessBuffer(ref retval, additionalSeed);
		CRC32.ProcessBuffer(ref retval, sharedname, strlen(sharedname));

		CRC32.Final(ref retval);

		return (int)(retval);
	}

	public static float SharedRandomFloat(ReadOnlySpan<char> sharedname, int minVal, int maxVal, int additionalSeed = 0) {
		int seed = SeedFileLineHash(SharedBaseEntity.GetPredictionRandomSeed(), sharedname, additionalSeed);
		RandomSeed(seed);
		return RandomFloat(minVal, maxVal);
	}

	public static int SharedRandomInt(ReadOnlySpan<char> sharedname, int minVal, int maxVal, int additionalSeed = 0) {
		int seed = SeedFileLineHash(SharedBaseEntity.GetPredictionRandomSeed(), sharedname, additionalSeed);
		RandomSeed(seed);
		return RandomInt(minVal, maxVal);
	}

}

public static partial class Util
{

#if CLIENT_DLL
	public static BasePlayer PlayerByIndex(int entindex) => ToBasePlayer(cl_entitylist.GetEnt(entindex));
#endif
	public static float VecToYaw(in Vector3 vec) {
		if (vec.Y == 0 && vec.X == 0)
			return 0;

		float yaw = MathF.Atan2(vec.Y, vec.X);
		yaw = MathLib.RAD2DEG(yaw);

		if (yaw < 0)
			yaw += 360;

		return yaw;
	}

	public static float VecToPitch(in Vector3 vec) {
		if (vec.Y == 0 && vec.X == 0) {
			if (vec.Z < 0)
				return 180.0f;
			else
				return -180.0f;
		}

		float dist = vec.Length2D();
		float pitch = MathF.Atan2(-vec.Z, dist);

		pitch = MathLib.RAD2DEG(pitch);

		return pitch;
	}

	public static float VecToYaw(in Matrix3x4 matrix, in Vector3 vec) {
		Vector3 tmp = vec;
		MathLib.VectorNormalize(ref tmp);

		float x = matrix[0][0] * tmp.X + matrix[1][0] * tmp.Y + matrix[2][0] * tmp.Z;
		float y = matrix[0][1] * tmp.X + matrix[1][1] * tmp.Y + matrix[2][1] * tmp.Z;

		if (x == 0.0f && y == 0.0f)
			return 0.0f;

		float yaw = MathF.Atan2(-y, x);
		yaw = MathLib.RAD2DEG(yaw);

		if (yaw < 0)
			yaw += 360;

		return yaw;
	}


	public static float VecToPitch(in Matrix3x4 matrix, in Vector3 vec) {
		Vector3 tmp = vec;
		MathLib.VectorNormalize(ref tmp);

		float x = matrix[0][0] * tmp.X + matrix[1][0] * tmp.Y + matrix[2][0] * tmp.Z;
		float z = matrix[0][2] * tmp.X + matrix[1][2] * tmp.Y + matrix[2][2] * tmp.Z;

		if (x == 0.0f && z == 0.0f)
			return 0.0f;

		float pitch = MathF.Atan2(z, x);
		pitch = MathLib.RAD2DEG(pitch);

		if (pitch < 0)
			pitch += 360;

		return pitch;
	}

	public static Vector3 YawToVector(float yaw) {
		Vector3 ret;

		ret.Z = 0;
		float angle = MathLib.DEG2RAD(yaw);
		MathLib.SinCos(angle, out ret.Y, out ret.X);

		return ret;
	}
}
#endif
