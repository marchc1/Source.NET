using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.ToolFramework;

public interface IToolFrameworkInternal
{
	bool ClientInit(IServiceProvider factory);
	void ClientShutdown();

	// Level init, shutdown
	void ClientLevelInitPreEntityAllTools();
	// entities are created / spawned / precached here
	void ClientLevelInitPostEntityAllTools();

	void ClientLevelShutdownPreEntityAllTools();
	// Entities are deleted / released here...
	void ClientLevelShutdownPostEntityAllTools();

	void ClientPreRenderAllTools();
	void ClientPostRenderAllTools();

	// Should we render with a thirdperson camera?
	bool IsThirdPersonCamera();

	// is the current tool recording?
	bool IsToolRecording();

	bool ServerInit(IServiceProvider factory);
	void ServerShutdown();

	void ServerLevelInitPreEntityAllTools();
	// entities are created / spawned / precached here
	void ServerLevelInitPostEntityAllTools();

	void ServerLevelShutdownPreEntityAllTools();
	// Entities are deleted / released here...
	void ServerLevelShutdownPostEntityAllTools();
	// end of level shutdown

	// Called each frame before entities think
	void ServerFrameUpdatePreEntityThinkAllTools();
	// called after entities think
	void ServerFrameUpdatePostEntityThinkAllTools();
	void ServerPreClientUpdateAllTools();

	void ServerPreSetupVisibilityAllTools();

	// If any tool returns false, the engine will not actually quit
	// FIXME:  Not implemented yet
	bool CanQuit();

	// Called at end of Host_Init
	bool PostInit();

	void Think(bool finalTick);

	void PostMessage(KeyValues msg);

	bool GetSoundSpatialization(int iUserData, int guid, ref SpatializationInfo info);

	void HostRunFrameBegin();
	void HostRunFrameEnd();

	void RenderFrameBegin();
	void RenderFrameEnd();

	// Paintmode is an enum declared in enginevgui.h
	void VGui_PreRenderAllTools(int paintMode);
	void VGui_PostRenderAllTools(int paintMode);

	void VGui_PreSimulateAllTools();
	void VGui_PostSimulateAllTools();

	// Are we using tools?
	bool InToolMode();

	// Should the game be allowed to render the world?
	bool ShouldGameRenderView();

	IMaterialProxy? LookupProxy(ReadOnlySpan<char> proxyName);

	int GetToolCount();
	ReadOnlySpan<char> GetToolName(int index);
	void SwitchToTool(int index);
	IToolSystem? SwitchToTool(ReadOnlySpan<char> toolName);
	bool IsTopmostTool(IToolSystem sys);
	IToolSystem? GetToolSystem(int index);
	IToolSystem? GetTopmostTool();
};
