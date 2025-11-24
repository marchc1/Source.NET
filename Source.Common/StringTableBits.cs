using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public static class StringTableBits
{
	public static int MaxModelIndexBits { get; private set; }
	public static int MaxModels { get; private set; }

	public static int MaxGenericIndexBits { get; private set; }
	public static int MaxGenerics { get; private set; }

	public static int MaxSoundIndexBits { get; private set; }
	public static int MaxSounds { get; private set; }

	public static int MaxDecalIndexBits { get; private set; }
	public static int MaxPrecacheDecals { get; private set; }

	public static void CL_SetupNetworkStringTableBits(INetworkStringTableContainer container, ReadOnlySpan<char> tableName) {
		Span<char> lowercased = stackalloc char[tableName.Length];
		tableName.ToLower(lowercased, null);
		switch (lowercased) {
			case "modelprecache":
				MaxModelIndexBits = container.FindTable(tableName)!.GetEntryBits();
				MaxModels = 1 << MaxModelIndexBits;
				break;
			case "genericprecache":
				MaxGenericIndexBits = container.FindTable(tableName)!.GetEntryBits();
				MaxGenerics = 1 << MaxGenericIndexBits;
				break;
			case "soundprecache":
				MaxSoundIndexBits = container.FindTable(tableName)!.GetEntryBits();
				MaxSounds = 1 << MaxSoundIndexBits;
				break;
			case "decalprecache":
				MaxDecalIndexBits = container.FindTable(tableName)!.GetEntryBits();
				MaxPrecacheDecals = 1 << MaxDecalIndexBits;
				break;
		}
	}
}
