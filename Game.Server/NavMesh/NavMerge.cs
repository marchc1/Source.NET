using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;

namespace Game.Server.NavMesh;

public partial class NavArea
{
	void SaveToSelectedSet(KeyValues areaKey) { }

	void RestoreFromSelectedSet(KeyValues areaKey) { }
}

class BuildSelectedSet
{

}

public partial class NavMesh
{
	void CommandNavSaveSelected(in TokenizedCommand args) { }

	void CommandNavMergeMesh(in TokenizedCommand args) { }
}
