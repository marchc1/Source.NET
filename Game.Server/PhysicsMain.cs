namespace Game.Server;

using Source.Common.Commands;

using System.Numerics;

public partial class BaseEntity
{
	public void CheckStepSimulationChanged() { /* todo */ }
}

public static class Physics
{
	public static bool g_bTestMoveTypeStepSimulation = true;
	static ConVar sv_teststepsimulation = new("1", 0);

	public static void TraceEntity(BaseEntity entity, in Vector3 start, in Vector3 end, uint mask, out Trace tr) {
		throw new NotImplementedException();
	}

	static void SimulateEntity(BaseEntity entity) {
		throw new NotImplementedException();
	}

	public static void RunThinkFunctions(bool simulating) {
		g_bTestMoveTypeStepSimulation = sv_teststepsimulation.GetBool();

		TimeUnit_t startTime = gpGlobals.CurTime;

		gEntList.CleanupDeleteList();

		if (!simulating) {
			for (int i = 1; i <= gpGlobals.MaxClients; i++) {
				BasePlayer? player = Util.PlayerByIndex(i);
				if (player != null) {
					gpGlobals.CurTime = startTime;
					player.ForceSimulation();
					SimulateEntity(player);
				}
			}
		}
		else {
			int listMax = SimThinkManager.g_SimThinkManager.ListCount();
			listMax = Math.Max(listMax, 1);
			BaseEntity[] list = new BaseEntity[listMax];

			int count = SimThinkManager.g_SimThinkManager.ListCopy(list, listMax);

			for (int i = 0; i < count; i++) {
				if (list[i] == null)
					continue;

				gpGlobals.CurTime = startTime;
				SimulateEntity(list[i]);
			}

			// Util.EnableRemoveImmediate();
		}

		gpGlobals.CurTime = startTime;
	}
}