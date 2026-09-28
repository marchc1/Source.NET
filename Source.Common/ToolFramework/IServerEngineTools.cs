using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.ToolFramework;

public interface IServerEngineTools
{
	void LevelInitPreEntityAllTools();
	void LevelInitPostEntityAllTools();
	void LevelShutdownPreEntityAllTools();
	void LevelShutdownPostEntityAllTools();
	void FrameUpdatePreEntityThinkAllTools();
	void FrameUpdatePostEntityThinkAllTools();
	void PreClientUpdateAllTools();
	ReadOnlyMemory<byte> GetEntityData(ReadOnlyMemory<byte> actualEntityData);
	void PreSetupVisibilityAllTools();
	bool InToolMode();
}
