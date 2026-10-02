namespace Source.Filesystem.GarrysMod;

public static class AddonWhiteList
{
	static readonly string[] Wildcard = [
		"lua/*.lua",
		"scenes/*.vcd",
		"particles/*.pcf",
		"resource/fonts/*.ttf",
		"scripts/vehicles/*.txt",
		"resource/localization/*/*.properties",
		"maps/*.bsp",
		"maps/*.lmp",
		"maps/*.nav",
		"maps/*.ain",
		"maps/*.png",
		"sound/*.wav",
		"sound/*.mp3",
		"sound/*.ogg",
		"materials/*.vmt",
		"materials/*.vtf",
		"materials/*.png",
		"materials/*.jpg",
		"materials/*.jpeg",
		"materials/colorcorrection/*.raw",
		"models/*.mdl",
		"models/*.vtx",
		"models/*.phy",
		"models/*.ani",
		"models/*.vvd",
		"gamemodes/*/*.txt",
		"gamemodes/*/*.fgd",
		"gamemodes/*/content/maps/*_ttt.txt",
		"gamemodes/*/content/data/*.txt",
		"gamemodes/*/logo.png",
		"gamemodes/*/icon24.png",
		"gamemodes/*/gamemode/*.lua",
		"gamemodes/*/entities/effects/*.lua",
		"gamemodes/*/entities/weapons/*.lua",
		"gamemodes/*/entities/entities/*.lua",
		"gamemodes/*/backgrounds/*.png",
		"gamemodes/*/backgrounds/*.jpg",
		"gamemodes/*/backgrounds/*.jpeg",
		"gamemodes/*/content/models/*.mdl",
		"gamemodes/*/content/models/*.vtx",
		"gamemodes/*/content/models/*.phy",
		"gamemodes/*/content/models/*.ani",
		"gamemodes/*/content/models/*.vvd",
		"gamemodes/*/content/materials/*.vmt",
		"gamemodes/*/content/materials/*.vtf",
		"gamemodes/*/content/materials/*.png",
		"gamemodes/*/content/materials/*.jpg",
		"gamemodes/*/content/materials/*.jpeg",
		"gamemodes/*/content/materials/colorcorrection/*.raw",
		"gamemodes/*/content/scenes/*.vcd",
		"gamemodes/*/content/particles/*.pcf",
		"gamemodes/*/content/resource/fonts/*.ttf",
		"gamemodes/*/content/scripts/vehicles/*.txt",
		"gamemodes/*/content/resource/localization/*/*.properties",
		"gamemodes/*/content/maps/*.bsp",
		"gamemodes/*/content/maps/*.nav",
		"gamemodes/*/content/maps/*.ain",
		"gamemodes/*/content/maps/thumb/*.png",
		"gamemodes/*/content/sound/*.wav",
		"gamemodes/*/content/sound/*.mp3",
		"gamemodes/*/content/sound/*.ogg",
		"data_static/*.txt",
		"data_static/*.dat",
		"data_static/*.json",
		"data_static/*.xml",
		"data_static/*.csv",
		"shaders/fxc/*.vcs"
	];

	const string BadChars = ":?\"<>|";
	const char FirstPrintable = (char)32;
	const ulong LastDataTimestamp = 1706652000;

	public static string FilenameErrors(string fileName, ulong timestamp) {
		foreach (char c in fileName) {
			if (c < FirstPrintable || BadChars.Contains(c))
				return "Invalid filename: '" + fileName + "': Code point " + (int)c;
		}

		foreach (string pattern in Wildcard) {
			bool matched = Bootil.String.Test.Wildcard(pattern, fileName);
			if (!matched)
				continue;

			if (pattern == "gamemodes/*/*.txt" || pattern == "gamemodes/*/*.fgd") {
				if (Bootil.String.Util.Count(fileName, '/') > 2)
					matched = false;
			}

			if (pattern == "gamemodes/*/content/data/*.txt" || pattern == "gamemodes/*/content/maps/*_ttt.txt") {
				if (timestamp - 1 >= LastDataTimestamp)
					matched = false;
			}

			if (matched)
				return "";
		}

		return "File not on whitelist: " + fileName;
	}
}
