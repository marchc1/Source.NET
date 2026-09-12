using Source.Common.Commands;
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

	public static void SV_SetupNetworkStringTableBits() {
		ConVarRef sv_precache_modelbits = new("sv_precache_modelbits");
		if (sv_precache_modelbits.IsValid()) {
			g_MaxModelIndexBits = sv_precache_modelbits.GetInt();
			g_MaxModels = 1 << g_MaxModelIndexBits;
		}

		ConVarRef sv_precache_generalbits = new("sv_precache_generalbits");
		if (sv_precache_generalbits.IsValid()) {
			g_MaxGenericIndexBits = sv_precache_generalbits.GetInt();
			g_MaxGenerics = 1 << g_MaxGenericIndexBits;
		}

		ConVarRef sv_precache_soundbits = new("sv_precache_soundbits");
		if (sv_precache_soundbits.IsValid()) {
			g_MaxSoundIndexBits = sv_precache_soundbits.GetInt();
			g_MaxSounds = 1 << g_MaxSoundIndexBits;
		}

		ConVarRef sv_precache_decalbits = new("sv_precache_decalbits");
		if (sv_precache_decalbits.IsValid()) {
			g_MaxDecalIndexBits = sv_precache_decalbits.GetInt();
			g_MaxPrecacheDecals = 1 << g_MaxDecalIndexBits;
		}
	}

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
