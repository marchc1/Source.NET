using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;

namespace Game.Server.NavMesh;

public partial class NavArea
{
	void SaveToSelectedSet(KeyValues areaKey) {
		ReadOnlySpan<char> placeName = NavMesh.Instance!.PlaceToName(GetPlace());
		areaKey.SetString("Place", placeName.IsEmpty ? "" : placeName);
		areaKey.SetInt("Attributes", (int)GetAttributes());
	}

	void RestoreFromSelectedSet(KeyValues areaKey) {
		SetPlace(NavMesh.Instance!.NameToPlace(areaKey.GetString("Place")));
		SetAttributes((NavAttributeType)areaKey.GetInt("Attributes"));
	}
}

class BuildSelectedSet
{

}

public partial class NavMesh
{
	void CommandNavSaveSelected(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	[ConCommand("nav_save_selected", "Writes the selected set to disk for merging into another mesh via nav_merge_mesh.", FCvar.Cheat | FCvar.GameDLL)]
	static void nav_save_selected(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		Instance!.CommandNavSaveSelected(args);
	}

	void CommandNavMergeMesh(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	[ConCommand("nav_merge_mesh", "Merges a saved selected set into the current mesh.", FCvar.Cheat | FCvar.GameDLL, nameof(NavMeshMergeAutocomplete))]
	static void nav_merge_mesh(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		Instance!.CommandNavMergeMesh(args);
	}

	IEnumerable<string> NavMeshMergeAutocomplete(string partial) {
		const string commandName = "nav_merge_mesh";
		string partialFilename = partial[(commandName.Length + 1)..];

		Span<char> txtFilenameNoExtension = stackalloc char[260];
		ReadOnlySpan<char> fileName = filesystem.FindFirstEx("maps/*_selected_*.txt", "MOD", out ulong findHandle);
		while (!fileName.IsEmpty) {
			if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) {
				int extensionIndex = fileName.LastIndexOf(".txt", StringComparison.OrdinalIgnoreCase);
				ReadOnlySpan<char> fileNameWithoutExtension = fileName[..extensionIndex];
				if (fileNameWithoutExtension.Contains("_selected_", StringComparison.OrdinalIgnoreCase) &&
					fileNameWithoutExtension.StartsWith(partialFilename, StringComparison.OrdinalIgnoreCase)) {
					yield return $"{commandName} {fileNameWithoutExtension.ToString()}";
				}
			}

			fileName = filesystem.FindNext(findHandle);
		}
	}
}
