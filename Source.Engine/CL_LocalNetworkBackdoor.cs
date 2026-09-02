namespace Source.Engine;


public partial class CL
{
	readonly LocalNetworkBackdoor localNetworkBackdoor = new();
	public static LocalNetworkBackdoor? LocalNetworkBackdoor;

	public void SetupLocalNetworkBackDoor(bool useBackdoor) {
		if (useBackdoor) {
			if (LocalNetworkBackdoor == null) {
				LocalNetworkBackdoor = localNetworkBackdoor;
				LocalNetworkBackdoor.StartBackdoorMode();
			}
		}
		else {
			if (LocalNetworkBackdoor != null) {
				LocalNetworkBackdoor.StopBackdoorMode();
				LocalNetworkBackdoor = null;
				cl.ForceFullUpdate();
			}
		}
	}
}
