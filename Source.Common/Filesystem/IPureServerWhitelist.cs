using Source.Common.Hashing;

using System.Runtime.CompilerServices;

namespace Source.Common.Filesystem;

public enum FileHashType
{
	Unknown = 0,
	EntireFile = 1,
	IncompleteFile = 2,
}

public struct FileHash
{
	public FileHash() {
		FileHashType = FileHashType.Unknown;
		CRCIOSequence = 0;
		MD5Contents = default;
		FileLen = 0;
		PackFileID = 0;
		PackFileNumber = 0;
	}

	public FileHashType FileHashType;
	public CRC32_t CRCIOSequence;
	public MD5Value MD5Contents;
	public int FileLen;
	public int PackFileID;
	public int PackFileNumber;

	public static bool operator ==(in FileHash a, in FileHash b) => a.CRCIOSequence == b.CRCIOSequence && a.MD5Contents == b.MD5Contents && a.FileHashType == b.FileHashType;
	public static bool operator !=(in FileHash a, in FileHash b) => a.CRCIOSequence != b.CRCIOSequence || a.MD5Contents != b.MD5Contents || a.FileHashType != b.FileHashType;

	public readonly override bool Equals(object? obj) => obj is FileHash h && h == this;
	public readonly override int GetHashCode() => HashCode.Combine(CRCIOSequence, MD5Contents, FileHashType);
}

public struct UnverifiedFileHash
{
	public InlineArrayMaxPath<char> m_PathID;
	public InlineArrayMaxPath<char> m_Filename;
	public int m_nFileFraction;
	public FileHash m_FileHash;
}

public struct UnverifiedCRCFile
{
	public InlineArrayMaxPath<char> PathID;
	public InlineArrayMaxPath<char> Filename;
	public CRC32_t m_CRC;
}

public struct UnverifiedMD5File
{
	public InlineArrayMaxPath<char> PathID;
	public InlineArrayMaxPath<char> Filename;
	public InlineArray16<byte> bits;
}

public enum PureServerFileClass
{
	Unknown = -1,
	Any = 0,
	AnyTrusted,
	CheckHash,
}

public interface IPureServerWhitelist
{
	PureServerFileClass GetFileClass(ReadOnlySpan<char> filename);
	int GetTrustedKeyCount();
	ReadOnlySpan<byte> GetTrustedKey(int keyIndex);
}
