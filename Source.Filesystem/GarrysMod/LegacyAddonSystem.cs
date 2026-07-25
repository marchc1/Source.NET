using Source.Common.GarrysMod;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

public class LegacyAddonSystem : LegacyAddons.System
{
	public readonly List<ILegacyAddons.Information> Addons = [];

	public List<ILegacyAddons.Information> GetList() => Addons;
	public void Refresh() {
		foreach (ILegacyAddons.Information info in Addons) {
			g_FullFileSystem.RemoveSearchPath(info.Path, "GAME");
			g_FullFileSystem.RemoveSearchPath(info.Path, "thirdparty");
		}

		Addons.Clear();

		FileFindHandle_t findHandle;
		ReadOnlySpan<char> filename = g_FullFileSystem.FindFirstEx("addons/*", "MOD", out findHandle);
		Span<char> fullpath = stackalloc char[1024];
		while (!filename.IsEmpty) {
			if (g_FullFileSystem.FindIsDirectory(findHandle)) {
				ReadOnlySpan<char> path = $"addons/{filename}";

				g_FullFileSystem.RelativePathToFullPath(path, "MOD", fullpath);

				g_FullFileSystem.AddSearchPath(fullpath, "GAME", groupName: Common.Filesystem.PathGroupName.AddonContent);
				g_FullFileSystem.AddSearchPath(fullpath, "thirdparty", groupName: Common.Filesystem.PathGroupName.AddonContent);

				ILegacyAddons.Information information;
				information.Name = new(filename.SliceNullTerminatedString());
				information.Path = new(fullpath.SliceNullTerminatedString());
				information.LuaPath = new(path);
				information.Placeholder4 = ""; // ToDo: Find out.

				Addons.Add(information);
			}

			filename = g_FullFileSystem.FindNext(findHandle);
		}
	}
}
