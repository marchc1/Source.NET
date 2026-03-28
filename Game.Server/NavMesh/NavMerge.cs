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
	void CommandNavSaveSelected(in TokenizedCommand args) { }

	void CommandNavMergeMesh(in TokenizedCommand args) { }
}
