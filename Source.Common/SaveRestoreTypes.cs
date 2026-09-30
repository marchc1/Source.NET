global using static Source.Common.SaveRestoreGlobals;

using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common;

public static class SaveRestoreGlobals
{
	public const int MAX_LEVEL_CONNECTIONS = 16;

	public static SaveRestoreData MakeSaveRestoreData(Memory<byte> data) {
		throw new NotImplementedException();
	}
}

[InlineArray(MAX_LEVEL_CONNECTIONS)] public struct InlineArrayMaxLevelConnections<T> { T first; }

public class SaveRestoreSegment
{
	public SaveRestoreSegment() { }

	//---------------------------------
	// Buffer data
	//
	public Memory<byte> BaseData;        // Start of all entity save data
	public Memory<byte> CurrentData; // Current buffer pointer for sequential access
	public int Size;           // Current data size, aka, pCurrentData - pBaseData
	public int BufferSize;     // Total space for data

	//---------------------------------
	// Symbol table
	//
	public int TokenCount;     // Number of elements in the pTokens table
	public byte[][]? Tokens;     // Hash table of entity strings (sparse)
}

public struct LevelList
{
	public InlineArrayMaxMapNameSave<char> MapName;
	public InlineArray32<char> LandmarkName;
	public Edict? EntLandmark;
	public Vector3 LandmarkOrigin;
}


public struct EHandlePlaceholder // Engine does some of the game writing (alas, probably shouldn't), but can't see ehandle.h
{
	public uint i;
}

public struct EntityTable
{
	public void Clear() {
		ID = -1;
		EdictIndex = -1;
		SaveEntityIndex = -1;
		RestoreEntityIndex = -1;
		Location = 0;
		Size = 0;
		Flags = 0;
		ClassName = null;
		GlobalName = null;
		LandmarkModelSpace = default;
		ModelName = null;
	}

	public int ID;             // Ordinal ID of this entity (used for entity <--> pointer conversions)
	public int EdictIndex;     // saved for if the entity requires a certain edict number when restored (players, world)

	public int SaveEntityIndex; // the entity index the entity had at save time ( for fixing up client side entities )
	public int RestoreEntityIndex; // the entity index given to this entity at restore time

	public EHandlePlaceholder Ent;

	public int Location;       // Offset from the base data of this entity
	public int Size;           // Byte size of this entity's data
	public int Flags;          // This could be a short -- bit mask of transitions that this entity is in the PVS of
	public string? ClassName;     // entity class name
	public string? GlobalName;        // entity global name
	public Vector3 LandmarkModelSpace;  // a fixed position in model space for comparison
										// NOTE: Brush models can be built in different coordiante systems
										//		in different levels, so this fixes up local quantities to match
										//		those differences.
	public string? ModelName;
}

public enum EntTableFlags : uint
{
	Player = 0x80000000,
	Removed = 0x40000000,
	Moveable = 0x20000000,
	Global = 0x10000000,
	PlayerChild = 0x08000000,
	LevelMask = 0x0000FFFF
}

public struct SaveRestoreLevelInfo
{
	public int ConnectionCount;
	public InlineArrayMaxLevelConnections<LevelList> LevelList;

	public bool UseLandmark;
	public InlineArray20<char> LandmarkName;
	public Vector3 LandmarkOffset;
	public TimeUnit_t Time;
	public InlineArrayMaxMapNameSave<char> CurrentMapName;
	public int MapVersion;
}

public class GameSaveRestoreInfo
{

}

public class SaveRestoreData
{
	public readonly SaveRestoreSegment Segment = new();
	public readonly GameSaveRestoreInfo Info = new();

	public SaveRestoreData() {

	}

	public bool Async;
}
