using System.Numerics;

namespace Source.Common.Networking.DataTable;

public class PackedEntity
{	
	public ServerClass? ServerClass;	// Valid on the server
	public ClientClass? ClientClass; // Valid on the client
		
	public int EntityIndex; // Entity index.
	public int ReferenceCount; // reference count;

	private Vector<SendProxyRecipients>	Recipients;

	private byte[]? Data; // Packed data.
	private int Bits; // Number of bits used to encode.
	private IChangeFrameList? ChangeFrameList;	// Only the most current 

	// This is the tick this PackedEntity was created on
	private uint SnapshotCreationTick;
	private uint ShouldCheckCreationTick;

	public void FreeData()
	{
		Data = null;
	}

	public void SetNumBits(int nBits)
	{
		Bits = nBits;
	}

	public byte[]? GetData()
	{
		return Data;
	}

	public int GetNumBytes()
	{
		return Net.Bits2Bytes(Bits);
	}

	public int GetNumBits()
	{
		return Bits;
	}

	public bool AllocAndCopyPadded(byte[]? pData, int size)
	{
		FreeData();

		if (pData == null)
			return true;

		Data = new byte[size];
		if (pData == null)
			return false;

		Array.Copy(pData, Data, size);
		SetNumBits(size * 8);
		
		return true;
	}
}