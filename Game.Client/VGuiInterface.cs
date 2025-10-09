
using Source.Common.GUI;

namespace Game.Client;

public static class ClientVGui
{
	internal static void CreateGlobalPanels() {
		IPanel toolParent = enginevgui.GetPanel(Source.Engine.VGuiPanelType.Tools);
		IFPSPanel.FPS.Create(toolParent);
		INetGraphPanel.NetGraph.Create(toolParent);
		IEntityReportPanel.EntityReport.Create(toolParent);
	}
}
