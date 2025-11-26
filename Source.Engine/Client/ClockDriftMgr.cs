using Source.Common;
using Source.Common.Commands;
using Source.Common.Networking;
using Source.Engine.Server;

namespace Source.Engine.Client;

public class ClockDriftMgr(Host Host, CommonHostState host_state, Net Net, ClientGlobalVariables clientGlobalVariables)
{
	public static readonly ConVar cl_clock_correction = new( "cl_clock_correction", "1", FCvar.Cheat, "Enable/disable clock correction on the client." );

	public static readonly ConVar cl_clockdrift_max_ms = new( "cl_clockdrift_max_ms", "150", FCvar.Cheat, "Maximum number of milliseconds the clock is allowed to drift before the client snaps its clock to the server's." );
	public static readonly ConVar cl_clockdrift_max_ms_threadmode = new( "cl_clockdrift_max_ms_threadmode", "0", FCvar.Cheat, "Maximum number of milliseconds the clock is allowed to drift before the client snaps its clock to the server's." );

	public static readonly ConVar cl_clock_showdebuginfo = new( "cl_clock_showdebuginfo", "0", FCvar.Cheat, "Show debugging info about the clock drift. ");

	public static readonly ConVar cl_clock_correction_force_server_tick = new( "cl_clock_correction_force_server_tick", "999", FCvar.Cheat, "Force clock correction to match the server tick + this offset (-999 disables it)."  );

	public static readonly ConVar cl_clock_correction_adjustment_max_amount = new( "cl_clock_correction_adjustment_max_amount", "200", FCvar.Cheat,
		"Sets the maximum number of milliseconds per second it is allowed to correct the client clock. " +
	"It will only correct this amount if the difference between the client and server clock is equal to or larger than cl_clock_correction_adjustment_max_offset." );

	public static readonly ConVar cl_clock_correction_adjustment_min_offset = new( "cl_clock_correction_adjustment_min_offset", "10", FCvar.Cheat,
		"If the clock offset is less than this amount (in milliseconds), then no clock correction is applied." );

	public static readonly ConVar cl_clock_correction_adjustment_max_offset = new( "cl_clock_correction_adjustment_max_offset", "90", FCvar.Cheat,
		"As the clock offset goes from cl_clock_correction_adjustment_min_offset to this value (in milliseconds), " +
	"it moves towards applying cl_clock_correction_adjustment_max_amount of adjustment. That way, the response " +
	"is small when the offset is small." );



	public static bool Enabled = true;

	public bool IsClockCorrectionEnabled() {
#if SWDS
		return false;
#else

		bool isMultiplayer = Net.IsMultiplayer();
		bool wantsClockDriftMgr = isMultiplayer;
		if (isMultiplayer) {
			bool isListenServer = sv.IsActive();
			// todo: net_usesocketsforloopback
			bool localConnectionHasZeroLatency = (Net.GetFakeLag() <= 0.0f) && false;

			if (isListenServer && localConnectionHasZeroLatency) 
				wantsClockDriftMgr = false;
		}

		return cl_clock_correction.GetInt() != 0 &&
			(EngineThreads.IsEngineThreaded() || wantsClockDriftMgr);
#endif
	}

	public void Clear() {
		ClientTick = 0;
		ServerTick = 0;
		CurClockOffset = 0;
		for (int i = 0; i < NUM_CLOCKDRIFT_SAMPLES; i++) {
			ClockOffsets[i] = 0;
		}
	}
	public void SetServerTick(int tick) {
		ServerTick = tick;
		int maxDriftTicks = Host.TimeToTicks(cl_clockdrift_max_ms.GetFloat() / 1000f);
		int clientTick = cl.GetClientTickCount() + clientGlobalVariables.SimTicksThisFrame - 1; 

		if (cl_clock_correction_force_server_tick.GetInt() == 999) {
			if (!IsClockCorrectionEnabled() || clientTick == 0 || Math.Abs(tick - clientTick) > maxDriftTicks) {
				cl.SetClientTickCount(tick - 0);
				if (cl.GetClientTickCount() < cl.OldTickCount) {
					cl.OldTickCount = cl.GetClientTickCount();
				}
				for (int i = 0; i < NUM_CLOCKDRIFT_SAMPLES; i++) {
					ClockOffsets[i] = 0;
				}
			}
		}
		else {
			cl.SetClientTickCount(tick + cl_clock_correction_force_server_tick.GetInt());
		}

		ClockOffsets[CurClockOffset] = clientTick - ServerTick;
		CurClockOffset = (CurClockOffset + 1) % NUM_CLOCKDRIFT_SAMPLES;
	}
	public float AdjustFrameTime(float inputFrameTime) {
		float adjustmentThisFrame = 0;
		float adjustmentPerSec = 0;

		float flCurDiffInSeconds = GetCurrentClockDifference() * (float)host_state.IntervalPerTick;
		float flCurDiffInMS = flCurDiffInSeconds * 1000.0f;

		if (flCurDiffInMS > cl_clock_correction_adjustment_min_offset.GetFloat()) {
			adjustmentPerSec = -GetClockAdjustmentAmount(flCurDiffInMS);
			adjustmentThisFrame = inputFrameTime * adjustmentPerSec;
			adjustmentThisFrame = Math.Max(adjustmentThisFrame, -flCurDiffInSeconds);
		}
		else if (flCurDiffInMS < -cl_clock_correction_adjustment_min_offset.GetFloat()) {
			adjustmentPerSec = GetClockAdjustmentAmount(-flCurDiffInMS);
			adjustmentThisFrame = inputFrameTime * adjustmentPerSec;
			adjustmentThisFrame = Math.Min(adjustmentThisFrame, -flCurDiffInSeconds);
		}

		if (EngineThreads.IsEngineThreaded())
			adjustmentThisFrame = -flCurDiffInSeconds;

		AdjustAverageDifferenceBy(adjustmentThisFrame);
		return inputFrameTime + adjustmentThisFrame;
	}
	void AdjustAverageDifferenceBy(float amountInSeconds) {
		float c = GetCurrentClockDifference();
		if (c < 0.05f)
			return;

		float flAmountInTicks = amountInSeconds / (float)host_state.IntervalPerTick;
		float factor = 1 + flAmountInTicks / c;

		for (int i = 0; i < NUM_CLOCKDRIFT_SAMPLES; i++)
			ClockOffsets[i] *= factor;
	}
	public float GetCurrentClockDifference() {
		float total = 0;
		for (int i = 0; i < NUM_CLOCKDRIFT_SAMPLES; i++)
			total += ClockOffsets[i];

		return total / NUM_CLOCKDRIFT_SAMPLES;
	}
	static float Remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo) {
		return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
	}
	public static float GetClockAdjustmentAmount(float curDiffInMS) {
		curDiffInMS = Math.Clamp(curDiffInMS, cl_clock_correction_adjustment_min_offset.GetFloat(), cl_clock_correction_adjustment_max_offset.GetFloat());

		float flReturnValue = Remap(curDiffInMS,
			cl_clock_correction_adjustment_min_offset.GetFloat(), cl_clock_correction_adjustment_max_offset.GetFloat(),
			0, cl_clock_correction_adjustment_max_amount.GetFloat() / 1000.0f);

		return flReturnValue;
	}
	const int NUM_CLOCKDRIFT_SAMPLES = 16;
	public float[] ClockOffsets = new float[NUM_CLOCKDRIFT_SAMPLES];
	public int CurClockOffset;
	public int ServerTick;
	public int ClientTick;

}
