
using Game.Shared;

using Source.Common;

namespace Game.Server.NextBot;

[LinkEntityToClass("func_nav_prerequisite")]
partial class FuncNavPrerequisite : BaseTrigger
{
	int Task;
	string TaskEntityName;
	float TaskValue;
	[NetworkVar] public partial bool Disabled { get; set; }
	EHANDLE TaskEntity;
}