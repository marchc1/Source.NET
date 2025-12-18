using Source.Common.Client;
using Source.Common.Formats.Keyvalues;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Common.ToolFramework;

public interface IToolSystem
{
	ReadOnlySpan<char> GetToolName();
	bool Init();
	void Shutdown();
	bool ServerInit(IServiceProvider serverFactory); 
	bool ClientInit(IServiceProvider clientFactory); 
	void ServerShutdown();
	void ClientShutdown();
	bool CanQuit(); 
    void PostMessage(HTOOLHANDLE hEntity, KeyValues message);
	void Think(bool finalTick);
	void ServerLevelInitPreEntity();
	void ServerLevelInitPostEntity();
	void ServerLevelShutdownPreEntity();
	void ServerLevelShutdownPostEntity();
	void ServerFrameUpdatePreEntityThink();
	void ServerFrameUpdatePostEntityThink();
	void ServerPreClientUpdate();
	void ServerPreSetupVisibility();
	ReadOnlySpan<char> GetEntityData( ReadOnlySpan<char> pActualEntityData );
	void ClientLevelInitPreEntity();
	void ClientLevelInitPostEntity();
	void ClientLevelShutdownPreEntity();
	void ClientLevelShutdownPostEntity();
	void ClientPreRender();
	void ClientPostRender();
	void AdjustEngineViewport(ref int x, ref int y, ref int width, ref int height );
	bool SetupEngineView(ref Vector origin, ref QAngle angles, ref float fov );
	bool SetupAudioState(ref AudioState audioState);
	bool ShouldGameRenderView();
	bool IsThirdPersonCamera();
	bool IsToolRecording();
	IMaterialProxy LookupProxy( ReadOnlySpan<char> proxyName );
	void OnToolActivate();
	void OnToolDeactivate();
	bool TrapKey(ButtonCode key, bool down);
	bool GetSoundSpatialization(int userData, int guid, ref SpatializationInfo info);
	void RenderFrameBegin();
	void RenderFrameEnd();
	void HostRunFrameBegin();
	void HostRunFrameEnd();
	void VGui_PreRender(int paintMode);
	void VGui_PostRender(int paintMode);
	void VGui_PreSimulate();
	void VGui_PostSimulate();
}
