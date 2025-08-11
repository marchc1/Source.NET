using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Networking.DataTable;
using Source.Engine.DataTable;
using static Source.Dbg;

namespace Source.Engine.DataTable;

using Source.Common.Bitbuffers;
using Source.Common.Mathematics;
using Source.Common.Networking.DataTable;
using System;
using System.Data.SqlTypes;
using System.Net.NetworkInformation;

public class DecodeInfo : CRecvProxyData
{
	public IntPtr Struct;
	public IntPtr Data;
	public SendProp Prop;
	public bf_read In;
	
	public char[] TempStr = new char[NetConstants.DT_MAX_STRING_BUFFERSIZE];
	
	public void CopyVars(DecodeInfo pOther)
	{
		Struct = pOther.Struct;
		Data = pOther.Data;
	
		RecvProp = pOther.RecvProp;
		Struct = pOther.Struct;
		In = pOther.In;
		ObjectID = pOther.ObjectID;
		Element = pOther.Element;
	}
}

// Function delegates matching the PropTypeFns function pointers
public delegate void EncodeDelegate(IntPtr pStruct, ref DVariant pVar, SendProp pProp, bf_write pOut, int objectID);
public delegate void DecodeDelegate(DecodeInfo pInfo);
public delegate int CompareDeltasDelegate(SendProp pProp, bf_read p1, bf_read p2);
public delegate void FastCopyDelegate(SendProp pSendProp, RecvProp pRecvProp, IntPtr pSendData, IntPtr pRecvData, int objectID);
public delegate string GetTypeNameStringDelegate();
public delegate bool IsZeroDelegate(IntPtr pStruct, ref DVariant pVar, SendProp pProp);
public delegate void DecodeZeroDelegate(DecodeInfo pInfo);
public delegate bool IsEncodedZeroDelegate(SendProp pProp, bf_read p);
public delegate void SkipPropDelegate(SendProp pProp, bf_read p);

public class PropTypeFns
{
	public EncodeDelegate? Encode;
	public DecodeDelegate? Decode;
	public CompareDeltasDelegate? CompareDeltas;
	public FastCopyDelegate? FastCopy;
	public GetTypeNameStringDelegate? GetTypeNameString;
	public IsZeroDelegate? IsZero;
	public DecodeZeroDelegate? DecodeZero;
	public IsEncodedZeroDelegate? IsEncodedZero;
	public SkipPropDelegate? SkipProp;
};

public static class DTGeneric
{
	public static void FastCopy(
		SendProp pSendProp,
		RecvProp pRecvProp,
		IntPtr pSendData,
		IntPtr pRecvData,
		int objectID)
	{
		var recvProxyData = new CRecvProxyData();
		var tmp = new DVariant();

		pSendProp.GetProxyFn()(
			pSendProp,
			pSendData,
			pSendData + pSendProp.GetOffset(),
			ref tmp,
			0,
			objectID
		);

		recvProxyData.Value = tmp;
		recvProxyData.RecvProp = pRecvProp;
		recvProxyData.Element = 0;
		recvProxyData.ObjectID = objectID;

		pRecvProp.GetProxyFn()(recvProxyData, pRecvData, pRecvData + pRecvProp.GetOffset());
	}
}

public static class DTEncode
{
	public static PropTypeFns[] g_PropTypeFns;
}

public static class DTInt
{
	public static void Decode(DecodeInfo pInfo)
	{
		var pProp = pInfo.Prop;
		int flags = pProp.GetFlags();

		if ((flags & (int)NetConstants.SPROP_VARINT) != 0)
		{
			if ((flags & (int)NetConstants.SPROP_UNSIGNED) != 0)
				pInfo.Value.IntValue = (int)pInfo.In.ReadVarInt32();
			else
				pInfo.Value.IntValue = pInfo.In.ReadSignedVarInt32();
		}
		else
		{
			int bits = pProp.NumBits;
			int val = (int)pInfo.In.ReadUBitLong(bits);

			if (bits != 32 && (flags & (int)NetConstants.SPROP_UNSIGNED) == 0)
			{
				uint highbit = 1u << (pProp.NumBits - 1);
				if (((uint)val & highbit) != 0)
				{
					val -= (int)highbit;
					val -= (int)highbit; // sign-extend
				}
			}

			pInfo.Value.IntValue = val;
		}

		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		if ((pProp.GetFlags() & (int)NetConstants.SPROP_VARINT) != 0)
		{
			if ((pProp.GetFlags() & (int)NetConstants.SPROP_UNSIGNED) != 0)
				return p1.ReadVarInt32() != p2.ReadVarInt32() ? 1 : 0;
			return p1.ReadSignedVarInt32() != p2.ReadSignedVarInt32() ? 1 : 0;
		}

		return p1.CompareBits(p2, pProp.NumBits) ? 1 : 0;
	}

	public static string GetTypeNameString() => "DPT_Int";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp) => pVar.IntValue == 0;

	public static void DecodeZero(DecodeInfo pInfo)
	{
		pInfo.Value.IntValue = 0;
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		if ((pProp.GetFlags() & (int)NetConstants.SPROP_VARINT) != 0)
		{
			if ((pProp.GetFlags() & (int)NetConstants.SPROP_UNSIGNED) != 0)
				return pIn.ReadVarInt32() == 0;
			return pIn.ReadSignedVarInt32() == 0;
		}
		return pIn.ReadUBitLong(pProp.NumBits) == 0;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		if ((pProp.GetFlags() & (int)NetConstants.SPROP_VARINT) != 0)
		{
			if ((pProp.GetFlags() & (int)NetConstants.SPROP_UNSIGNED) != 0)
				_ = pIn.ReadVarInt32();
			else
				_ = pIn.ReadSignedVarInt32();
		}
		else
		{
			pIn.SeekRelative(pProp.NumBits);
		}
	}
}

public static class DTFloat
{
	public static float DecodeFloat(SendProp pProp, bf_read pIn)
	{
		int flags = pProp.GetFlags();
		if ((flags & NetConstants.SPROP_COORD) != 0)
		{
			return pIn.ReadBitCoord();
		}
		else if ((flags & (NetConstants.SPROP_COORD_MP |
						   NetConstants.SPROP_COORD_MP_LOWPRECISION |
						   NetConstants.SPROP_COORD_MP_INTEGRAL)) != 0)
		{
			bool integral     = (flags & NetConstants.SPROP_COORD_MP_INTEGRAL) != 0;
			bool lowPrecision = (flags & NetConstants.SPROP_COORD_MP_LOWPRECISION) != 0;
			return pIn.ReadBitCoordMP(integral, lowPrecision);
		} else if ((flags & NetConstants.SPROP_NOSCALE) != 0) {
			return pIn.ReadBitFloat();
		} else if ((flags & NetConstants.SPROP_NORMAL) != 0) {
			return pIn.ReadBitNormal();
		} else // standard clamped-range float
		{
			ulong dwInterp = pIn.ReadUBitLong(pProp.NumBits);
			float fVal = (float)dwInterp / ((1 << pProp.NumBits) - 1);
			fVal = pProp.LowValue + (pProp.HighValue - pProp.LowValue) * fVal;
			return fVal;
		}
	}

	public static void Decode(DecodeInfo pInfo)
	{
		pInfo.Value.FloatValue = DecodeFloat(pInfo.Prop, pInfo.In);
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		int flags = pProp.GetFlags();
		if ((flags & (int)NetConstants.SPROP_COORD) != 0)
			return p1.ReadBitCoordBits() != p2.ReadBitCoordBits() ? 1 : 0;
		else if ((flags & ((int)NetConstants.SPROP_COORD_MP |
							(int)NetConstants.SPROP_COORD_MP_LOWPRECISION |
							(int)NetConstants.SPROP_COORD_MP_INTEGRAL)) != 0)
		{
			bool integral = ((flags >> 15) & 1) != 0;
			bool low = ((flags >> 14) & 1) != 0;
			return p1.ReadBitCoordMPBits(integral, low) != p2.ReadBitCoordMPBits(integral, low) ? 1 : 0;
		}
		else
		{
			int bits;
			if ((flags & (int)NetConstants.SPROP_NOSCALE) != 0)
				bits = 32;
			else if ((flags & (int)NetConstants.SPROP_NORMAL) != 0)
				bits = NetConstants.NORMAL_FRACTIONAL_BITS + 1;
			else
				bits = pProp.NumBits;

			return p1.ReadUBitLong(bits) != p2.ReadUBitLong(bits) ? 1 : 0;
		}
	}

	public static string GetTypeNameString() => "DPT_Float";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp) => pVar.FloatValue == 0.0f;

	public static void DecodeZero(DecodeInfo pInfo)
	{
		pInfo.Value.FloatValue = 0f;
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		return DecodeFloat(pProp, pIn) == 0.0f;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		int flags = pProp.GetFlags();

		if ((flags & (int)NetConstants.SPROP_COORD) != 0)
		{
			// Read req integer & fraction flags (2 bits)
			uint val = (uint)pIn.ReadUBitLong(2);
			if (val != 0)
			{
				int seekDist = 1; // sign bit
				if ((val & 1) != 0) seekDist += NetConstants.COORD_INTEGER_BITS;
				if ((val & 2) != 0) seekDist += NetConstants.COORD_FRACTIONAL_BITS;
				pIn.SeekRelative(seekDist);
			}
		}
		else if ((flags & (int)NetConstants.SPROP_COORD_MP) != 0)
		{
			_ = pIn.ReadBitCoordMP(false, false);
		}
		else if ((flags & (int)NetConstants.SPROP_COORD_MP_LOWPRECISION) != 0)
		{
			_ = pIn.ReadBitCoordMP(false, true);
		}
		else if ((flags & (int)NetConstants.SPROP_COORD_MP_INTEGRAL) != 0)
		{
			_ = pIn.ReadBitCoordMP(true, false);
		}
		else if ((flags & (int)NetConstants.SPROP_NOSCALE) != 0)
		{
			pIn.SeekRelative(32);
		}
		else if ((flags & (int)NetConstants.SPROP_NORMAL) != 0)
		{
			pIn.SeekRelative(NetConstants.NORMAL_FRACTIONAL_BITS + 1);
		}
		else
		{
			pIn.SeekRelative(pProp.NumBits);
		}
	}
};

public static class DTVector
{
	public static void DecodeVector(SendProp pProp, bf_read pIn, out Vector v)
	{
		v.X = DTFloat.DecodeFloat(pProp, pIn);
		v.Y = DTFloat.DecodeFloat(pProp, pIn);

		// Don't read in the third component for normals
		if ((pProp.GetFlags() & NetConstants.SPROP_NORMAL) == 0)
		{
			v.Z = DTFloat.DecodeFloat(pProp, pIn);
		}
		else
		{
			int signbit = pIn.ReadOneBit();

			float v0v0v1v1 = v.X * v.X +
				v.Y * v.Y;
			if (v0v0v1v1 < 1.0f)
				v.Z = (float)Math.Sqrt( 1.0f - v0v0v1v1 );
			else
				v.Z = 0.0f;

			if (signbit == 1)
				v.Z *= -1.0f;
		}
	}

	public static void Decode(DecodeInfo pInfo)
	{
		DecodeVector(pInfo.Prop, pInfo.In, out pInfo.Value.VectorValue);

		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		int c1 = DTFloat.CompareDeltas(pProp, p1, p2);
		int c2 = DTFloat.CompareDeltas(pProp, p1, p2);
		int c3;
		if ((pProp.GetFlags() & (int)NetConstants.SPROP_NORMAL) != 0)
			c3 = (p1.ReadOneBit() != p2.ReadOneBit()) ? 1 : 0;
		else
			c3 = DTFloat.CompareDeltas(pProp, p1, p2);

		return (c1 | c2 | c3);
	}

	public static string GetTypeNameString() => "DPT_Vector";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp)
	{
		return pVar.VectorValue.X == 0 && pVar.VectorValue.Y == 0 && pVar.VectorValue.Z == 0;
	}

	public static void DecodeZero(DecodeInfo pInfo)
	{
		pInfo.Value.VectorValue.Init();
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		Vector vec;
		DecodeVector(pProp, pIn, out vec);
		return vec.X == 0f && vec.Y == 0f && vec.Z == 0f;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		DTFloat.SkipProp(pProp, pIn);
		DTFloat.SkipProp(pProp, pIn);

		if ((pProp.GetFlags() & (int)NetConstants.SPROP_NORMAL) != 0)
		{
			pIn.SeekRelative(1); // sign bit for z
		}
		else
		{
			DTFloat.SkipProp(pProp, pIn);
		}
	}
};

public static class DTVectorXY
{
	public static void Decode(DecodeInfo pInfo)
	{
		pInfo.Value.VectorValue.Init();
		pInfo.Value.VectorValue.X = DTFloat.DecodeFloat(pInfo.Prop, pInfo.In);
		pInfo.Value.VectorValue.Y = DTFloat.DecodeFloat(pInfo.Prop, pInfo.In);

		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		int c1 = DTFloat.CompareDeltas(pProp, p1, p2);
		int c2 = DTFloat.CompareDeltas(pProp, p1, p2);
		return c1 | c2;
	}

	public static string GetTypeNameString() => "DPT_VectorXY";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp)
	{
		return pVar.VectorValue.X == 0f && pVar.VectorValue.Y == 0f;
	}

	public static void DecodeZero(DecodeInfo pInfo)
	{
		pInfo.Value.VectorValue.Init();
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		float x = DTFloat.DecodeFloat(pProp, pIn);
		float y = DTFloat.DecodeFloat(pProp, pIn);
		return x == 0f && y == 0f;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		DTFloat.SkipProp(pProp, pIn);
		DTFloat.SkipProp(pProp, pIn);
	}
};

public static class DTString
{
	public static void Decode(DecodeInfo pInfo)
	{
		int len = (int)pInfo.In.ReadUBitLong(NetConstants.DT_MAX_STRING_BITS);

		if (len >= NetConstants.DT_MAX_STRING_BUFFERSIZE)
		{
			Warning($"String_Decode({pInfo.RecvProp?.GetName()}) invalid length ({len})");
			len = NetConstants.DT_MAX_STRING_BUFFERSIZE - 1;
		}

		// Read bytes (len*8 bits) and convert to string.
		var buf = new byte[len];
		if (len > 0)
			pInfo.In.ReadBits(buf, len * 8);

		string s = len > 0 ? System.Text.Encoding.UTF8.GetString(buf, 0, len) : string.Empty;
		pInfo.Value.StringValue = s;

		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	static int AreBitsDifferent(bf_read pBuf1, bf_read pBuf2, int nBits)
	{
		int nDWords = nBits >> 5;
		int diff = 0;

		for (int i = 0; i < nDWords; i++)
			diff |= (pBuf1.ReadUBitLong(32) != pBuf2.ReadUBitLong(32)) ? 1 : 0;

		int nRemainingBits = nBits - (nDWords << 5);
		if (nRemainingBits > 0)
			diff |= (pBuf1.ReadUBitLong(nRemainingBits) != pBuf2.ReadUBitLong(nRemainingBits)) ? 1 : 0;

		return diff;
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		int len1 = (int)p1.ReadUBitLong(NetConstants.DT_MAX_STRING_BITS);
		int len2 = (int)p2.ReadUBitLong(NetConstants.DT_MAX_STRING_BITS);

		if (len1 == len2)
		{
			if (len1 == 0)
				return 0;
			return AreBitsDifferent(p1, p2, len1 * 8);
		}
		else
		{
			if (len1 > 0) p1.SeekRelative(len1 * 8);
			if (len2 > 0) p2.SeekRelative(len2 * 8);
			return 1;
		}
	}

	public static string GetTypeNameString() => "DPT_String";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp)
		=> string.IsNullOrEmpty(pVar.StringValue);

	public static void DecodeZero(DecodeInfo pInfo)
	{
		pInfo.Value.StringValue = string.Empty;
		if (pInfo.RecvProp != null)
			pInfo.RecvProp.GetProxyFn()(new CRecvProxyData
			{
				RecvProp = pInfo.RecvProp,
				Value = pInfo.Value,
				ObjectID = pInfo.ObjectID
			}, pInfo.Struct, pInfo.Data);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		int len = (int)pIn.ReadUBitLong(NetConstants.DT_MAX_STRING_BITS);
		if (len > 0) pIn.SeekRelative(len * 8);
		return len == 0;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		int len = (int)pIn.ReadUBitLong(NetConstants.DT_MAX_STRING_BITS);
		if (len > 0) pIn.SeekRelative(len * 8);
	}
};

public static class DTArray
{
	static void SkipPropData(bf_read buffer, SendProp prop)
	{
		DTEncode.g_PropTypeFns[(int)prop.Type].SkipProp(prop, buffer);
	}

	public static int GetLength(object pStruct, SendProp pProp, int objectID)
	{
		var proxy = pProp.GetArrayLengthProxy();
		if (proxy != null)
		{
			int nElements = proxy(pStruct, objectID);
			if (nElements > pProp.GetNumElements())
				nElements = pProp.GetNumElements();
			return nElements;
		}
		return pProp.GetNumElements();
	}

	public static void Decode(DecodeInfo pInfo)
	{
		var pArrayProp = pInfo.Prop.GetArrayProp();
		if (pArrayProp == null)
			throw new InvalidOperationException("Array_Decode: missing ArrayProp for a property.");

		var sub = new DecodeInfo();
		sub.CopyVars(pInfo);
		sub.Prop = pArrayProp;

		int elementStride = 0;
		ArrayLengthRecvProxyFn lengthProxy = null;

		if (pInfo.RecvProp != null)
		{
			var arrayRecvProp = pInfo.RecvProp.GetArrayProp();
			sub.RecvProp = arrayRecvProp;

			sub.Data = IntPtr.Add(pInfo.Data, arrayRecvProp.GetOffset());

			elementStride = pInfo.RecvProp.GetElementStride();
			lengthProxy  = pInfo.RecvProp.GetArrayLengthProxy();
		}

		int nElements = (int)pInfo.In.ReadUBitLong(pInfo.Prop.GetNumArrayLengthBits());
		if (lengthProxy != null)
			lengthProxy(pInfo.Struct, pInfo.ObjectID, nElements);

		for (sub.Element = 0; sub.Element < nElements; sub.Element++)
		{
			DTEncode.g_PropTypeFns[(int)pArrayProp.Type].Decode(sub);
			// advance data pointer by elementStride (engine-specific). Here we just signal progression.
			// Real engines would add elementStride to the destination pointer.
		}
	}

	public static int CompareDeltas(SendProp pProp, bf_read p1, bf_read p2)
	{
		var pArrayProp = pProp.GetArrayProp();
		if (pArrayProp == null)
			throw new InvalidOperationException($"Array_CompareDeltas: missing ArrayProp for SendProp '{pProp.VarName}'.");

		int nLengthBits = pProp.GetNumArrayLengthBits();
		int length1 = (int)p1.ReadUBitLong(nLengthBits);
		int length2 = (int)p2.ReadUBitLong(nLengthBits);

		int bDifferent = (length1 != length2) ? 1 : 0;

		int nSame = Math.Min(length1, length2);
		for (int i = 0; i < nSame; i++)
		{
			bDifferent |= DTEncode.g_PropTypeFns[(int)pArrayProp.Type].CompareDeltas(pArrayProp, p1, p2);
		}

		if (length1 != length2)
		{
			bf_read buffer = (length1 > length2) ? p1 : p2;
			int nExtra = Math.Max(length1, length2) - nSame;
			for (int iEatUp = 0; iEatUp < nExtra; iEatUp++)
			{
				SkipPropData(buffer, pArrayProp);
			}
		}

		return bDifferent;
	}

	public static void FastCopy(
		SendProp pSendProp,
		RecvProp pRecvProp,
		IntPtr pSendData,
		IntPtr pRecvData,
		int objectID)
	{
		var pArrayRecvProp = pRecvProp.GetArrayProp();
		var pArraySendProp = pSendProp.GetArrayProp();

		var recvProxyData = new CRecvProxyData
		{
			RecvProp = pArrayRecvProp,
			ObjectID = objectID
		};

		int nElements = DTArray.GetLength(pSendData, pSendProp, objectID);
		var lengthProxy = pRecvProp.GetArrayLengthProxy();
		if (lengthProxy != null)
			lengthProxy(pRecvData, objectID, nElements);

		object pCurSendPos = pSendData; // placeholder for offset math
		object pCurRecvPos = pRecvData; // placeholder for offset math

		for (recvProxyData.Element = 0; recvProxyData.Element < nElements; recvProxyData.Element++)
		{
			var val = new DVariant();
			pArraySendProp.GetProxyFn()(pArraySendProp, pSendData, pCurSendPos, ref val, recvProxyData.Element, objectID);
			recvProxyData.Value = val;

			pArrayRecvProp.GetProxyFn()(recvProxyData, pRecvData, pCurRecvPos);

			// advance pseudo-pointers by stride (engine would add bytes)
			// pCurSendPos = AddBytes(pCurSendPos, pSendProp.GetElementStride());
			// pCurRecvPos = AddBytes(pCurRecvPos, pRecvProp.GetElementStride());
		}
	}

	public static string GetTypeNameString() => "DPT_Array";

	public static bool IsZero(IntPtr pStruct, ref DVariant pVar, SendProp pProp)
	{
		int nElements = GetLength(pStruct, pProp, -1);
		return nElements == 0;
	}

	public static void DecodeZero(DecodeInfo pInfo)
	{
		var lengthProxy = pInfo.RecvProp?.GetArrayLengthProxy();
		if (lengthProxy != null)
			lengthProxy(pInfo.Struct, pInfo.ObjectID, 0);
	}

	public static bool IsEncodedZero(SendProp pProp, bf_read pIn)
	{
		var pArrayProp = pProp.GetArrayProp();
		if (pArrayProp == null)
			throw new InvalidOperationException("Array_IsEncodedZero: missing ArrayProp for a property.");

		uint nElements = pIn.ReadUBitLong(pProp.GetNumArrayLengthBits());
		for (int i = 0; i < nElements; i++)
		{
			// This mirrors the original call to IsEncodedZero (consumes bits as it checks).
			_ = DTEncode.g_PropTypeFns[(int)pArrayProp.Type].IsEncodedZero(pArrayProp, pIn);
		}
		return nElements == 0;
	}

	public static void SkipProp(SendProp pProp, bf_read pIn)
	{
		var pArrayProp = pProp.GetArrayProp();
		if (pArrayProp == null)
			throw new InvalidOperationException("Array_SkipProp: missing ArrayProp for a property.");

		uint nElements = pIn.ReadUBitLong(pProp.GetNumArrayLengthBits());
		for (int i = 0; i < nElements; i++)
		{
			DTEncode.g_PropTypeFns[(int)pArrayProp.Type].SkipProp(pArrayProp, pIn);
		}
	}
}

public static class DTDataTable
{
	public static string GetTypeNameString() => "DPT_DataTable";
}

public static class DTEncodeSetup
{
	public static void SetupDT()
	{
		DTEncode.g_PropTypeFns = new PropTypeFns[(int)SendPropType.NUMSendPropTypes]
		{
			new PropTypeFns
			{
				Encode = null,
				Decode = DTInt.Decode,
				CompareDeltas = DTInt.CompareDeltas,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = DTInt.GetTypeNameString,
				IsZero = DTInt.IsZero,
				DecodeZero = DTInt.DecodeZero,
				IsEncodedZero = DTInt.IsEncodedZero,
				SkipProp = DTInt.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = DTFloat.Decode,
				CompareDeltas = DTFloat.CompareDeltas,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = DTFloat.GetTypeNameString,
				IsZero = DTFloat.IsZero,
				DecodeZero = DTFloat.DecodeZero,
				IsEncodedZero = DTFloat.IsEncodedZero,
				SkipProp = DTFloat.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = DTVector.Decode,
				CompareDeltas = DTVector.CompareDeltas,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = DTVector.GetTypeNameString,
				IsZero = DTVector.IsZero,
				DecodeZero = DTVector.DecodeZero,
				IsEncodedZero = DTVector.IsEncodedZero,
				SkipProp = DTVector.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = DTVectorXY.Decode,
				CompareDeltas = DTVectorXY.CompareDeltas,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = DTVectorXY.GetTypeNameString,
				IsZero = DTVectorXY.IsZero,
				DecodeZero = DTVectorXY.DecodeZero,
				IsEncodedZero = DTVectorXY.IsEncodedZero,
				SkipProp = DTVectorXY.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = DTString.Decode,
				CompareDeltas = DTString.CompareDeltas,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = DTString.GetTypeNameString,
				IsZero = DTString.IsZero,
				DecodeZero = DTString.DecodeZero,
				IsEncodedZero = DTString.IsEncodedZero,
				SkipProp = DTString.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = DTArray.Decode,
				CompareDeltas = DTArray.CompareDeltas,
				FastCopy = DTArray.FastCopy,
				GetTypeNameString = DTArray.GetTypeNameString,
				IsZero = DTArray.IsZero,
				DecodeZero = DTArray.DecodeZero,
				IsEncodedZero = DTArray.IsEncodedZero,
				SkipProp = DTArray.SkipProp
			},
			new PropTypeFns
			{
				Encode = null,
				Decode = null,
				CompareDeltas = null,
				FastCopy = null,
				GetTypeNameString = DTDataTable.GetTypeNameString,
				IsZero = null,
				DecodeZero = null,
				IsEncodedZero = null,
				SkipProp = null
			},
			new PropTypeFns // GmodDataTable
			{
				Encode = null,
				Decode = null,
				CompareDeltas = null,
				FastCopy = DTGeneric.FastCopy,
				GetTypeNameString = null,
				IsZero = null,
				DecodeZero = null,
				IsEncodedZero = null,
				SkipProp = null
			}
		};
	}
}

public class DeltaBitsReader
{
    private bf_read Buffer;
    private int LastProp = -1;
    private bool Finished = false;

    public DeltaBitsReader(bf_read Buf)
    {
        Buffer = Buf;
    }

    public void ForceFinished()
    {
        Finished = true;
    }

    public uint ReadNextPropIndex()
    {
        if (Buffer.BitsLeft >= 7)
        {
            uint bits = Buffer.ReadUBitLong(7);
            if ((bits & 1) != 0)
            {
                uint delta = bits >> 3;
                if ((bits & 6) != 0)
                {
                    int extraBits = (int)((bits & 6) >> 1);
                    delta = Buffer.ReadUBitVarInternal(extraBits);
                }
                LastProp += 1 + (int)delta;
                return (uint)LastProp;
            }

            // Roll back 6 bits
            Buffer.curBit -= 6;
        }
        else
        {
            if (Buffer.ReadOneBit() == 1)
                Buffer.Seek(-1);
        }

        ForceFinished();
        return uint.MaxValue;
    }

    public void SkipPropData(SendProp prop)
    {
        DTEncode.g_PropTypeFns[(int)prop.Type].SkipProp(prop, Buffer);
    }

    public void CopyPropData(bf_write writer, SendProp prop)
    {
        int start = Buffer.BitsRead;
        DTEncode.g_PropTypeFns[(int)prop.Type].SkipProp(prop, Buffer);
        int len = Buffer.BitsRead - start;
        Buffer.Seek(start);
        writer.WriteBits(Buffer.BaseArray, len);
    }

    public int ComparePropData(DeltaBitsReader other, SendProp prop)
    {
        return DTEncode.g_PropTypeFns[(int)prop.Type].CompareDeltas(prop, Buffer, other.Buffer);
    }
}

public class DeltaBitsWriter
{
	public DeltaBitsWriter( bf_write pBuf )
	{
		Buffer = pBuf;
		LastProp = -1;
	}

	~DeltaBitsWriter()
	{
		Buffer.WriteOneBit( 0 );
	}

	// Write the next property index. Returns the number of bits used.
	public void WritePropIndex(uint iProp)
	{
		int diff = (int)iProp - LastProp;
		LastProp = (int)iProp;
		int n = ((diff < 0x11u) ? -1 : 0) + ((diff < 0x101u) ? -1 : 0);
		Buffer.WriteUBitLong( (uint)(diff*8 - 8 + 4 + n*2 + 1), 8 + n*4 + 4 + 2 + 1 );
	}

	// Access the buffer it's outputting to.
	public bf_write GetBitBuf() { return Buffer; }

	public bf_write	Buffer;
	public int LastProp;
};