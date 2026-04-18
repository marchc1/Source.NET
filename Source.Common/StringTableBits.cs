using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public static class StringTableBits
{
	public static int g_MaxModelIndexBits { get; private set; }
	public static int g_MaxModels { get; private set; }

	public static int g_MaxGenericIndexBits { get; private set; }
	public static int g_MaxGenerics { get; private set; }

	public static int g_MaxSoundIndexBits { get; private set; }
	public static int g_MaxSounds { get; private set; }

	public static int g_MaxDecalIndexBits { get; private set; }
	public static int g_MaxPrecacheDecals { get; private set; }

	public static void CL_SetupNetworkStringTableBits(INetworkStringTableContainer container, ReadOnlySpan<char> tableName) {
		Span<char> lowercased = stackalloc char[tableName.Length];
		tableName.ToLower(lowercased, null);
		switch (lowercased) {
			case "modelprecache":
				g_MaxModelIndexBits = container.FindTable(tableName)!.GetEntryBits();
				g_MaxModels = 1 << g_MaxModelIndexBits;
				break;
			case "genericprecache":
				g_MaxGenericIndexBits = container.FindTable(tableName)!.GetEntryBits();
				g_MaxGenerics = 1 << g_MaxGenericIndexBits;
				break;
			case "soundprecache":
				g_MaxSoundIndexBits = container.FindTable(tableName)!.GetEntryBits();
				g_MaxSounds = 1 << g_MaxSoundIndexBits;
				break;
			case "decalprecache":
				g_MaxDecalIndexBits = container.FindTable(tableName)!.GetEntryBits();
				g_MaxPrecacheDecals = 1 << g_MaxDecalIndexBits;
				break;
		}
	}
}
