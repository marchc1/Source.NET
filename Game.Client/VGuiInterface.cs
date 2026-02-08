
global using static Game.Client.ClientVGui;

using Source.Common.GUI;

namespace Game.Client;

public static class ClientVGui
{
	public static void VGui_VideoMode_AdjustForModeChange() {
		INetGraphPanel.NetGraph.Destroy();
		IDebugOverlayPanel.DebugOverlay.Destroy();
		IFPSPanel.FPS.Destroy();

		IPanel gameToolParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.ClientDllTools);
		IPanel toolParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.Tools);

		IFPSPanel.FPS.Create(toolParent);
		INetGraphPanel.NetGraph.Create(toolParent);
		IDebugOverlayPanel.DebugOverlay.Create(gameToolParent);
	}

	internal static void VGui_CreateGlobalPanels() {
		IPanel gameToolParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.ClientDllTools);
		IPanel toolParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.Tools);

		CenterPrint.CenterString.Create(gameToolParent);

		IFPSPanel.FPS.Create(toolParent);
		INetGraphPanel.NetGraph.Create(toolParent);
		IDebugOverlayPanel.DebugOverlay.Create(gameToolParent);
	}

	public static void GetHudSize(out int w, out int h) {
		IPanel? hudParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.ClientDll);
		if (hudParent != null)
			hudParent.GetSize(out w, out h);
		else
			surface.GetScreenSize(out w, out h);
	}
}
