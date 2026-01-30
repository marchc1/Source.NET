global using static Game.Server.GameInterface;

using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Server;

public static class EngineCallbacks
{
	public static void WRITE_BYTE(byte x) => GameInterface.MessageWriteByte(x);
	public static void WRITE_CHAR(sbyte x) => GameInterface.MessageWriteChar(x);
	public static void WRITE_SHORT(short x) => GameInterface.MessageWriteShort(x);
	public static void WRITE_WORD(int x) => GameInterface.MessageWriteWord(x);
	public static void WRITE_LONG(int x) => GameInterface.MessageWriteLong(x);
	public static void WRITE_FLOAT(float x) => GameInterface.MessageWriteFloat(x);
	public static void WRITE_ANGLE(float x) => GameInterface.MessageWriteAngle(x);
	public static void WRITE_COORD(in float x) => GameInterface.MessageWriteCoord(x);
	public static void WRITE_VEC3COORD(in Vector3 x) => GameInterface.MessageWriteVec3Coord(x);
	public static void WRITE_VEC3NORMAL(in Vector3 x) => GameInterface.MessageWriteVec3Normal(x);
	public static void WRITE_ANGLES(in QAngle x) => GameInterface.MessageWriteAngles(x);
	public static void WRITE_STRING(ReadOnlySpan<char> x) => GameInterface.MessageWriteString(x);
	public static void WRITE_ENTITY(int x) => GameInterface.MessageWriteEntity(x);
	public static void WRITE_EHANDLE(BaseEntity? x) => GameInterface.MessageWriteEHandle(x);
	public static void WRITE_BOOL(bool x) => GameInterface.MessageWriteBool(x);
	public static void WRITE_UBITLONG(uint x, int bits) => GameInterface.MessageWriteUBitLong(x, bits);
	public static void WRITE_SBITLONG(int x, int bits) => GameInterface.MessageWriteSBitLong(x, bits);
	public static void WRITE_BITS(ReadOnlySpan<byte> x, int bits) => GameInterface.MessageWriteBits(x, bits);
}
