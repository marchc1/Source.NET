using CommunityToolkit.HighPerformance;

using Game.Client.HL2;
using Game.Client.HUD;
using Game.Shared;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;

namespace Game.Client;

public static class CdllExts
{
	public static void TrackBoneSetupEnt(C_BaseAnimating ent) {
		// todo
	}
}

public class HLClient(IServiceProvider services, ClientGlobalVariables gpGlobals, ISurface surface, ViewRender view, IInput input, Hud HUD, UserMessages usermessages, Interpolation Interpolation) : IBaseClientDLL
{
	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton<IInput, HLInput>();
		services.AddSingleton<ClientEntityList>();
		services.AddSingleton<IClientEntityList>(x => x.GetRequiredService<ClientEntityList>());
		services.AddSingleton<IPrediction, Prediction>();
		services.AddSingleton<ICenterPrint, CenterPrint>();
		services.AddSingleton<ClientLeafSystem>();
		services.AddSingleton<IClientLeafSystem>(x => x.GetRequiredService<ClientLeafSystem>());
		services.AddSingleton<IClientLeafSystemEngine>(x => x.GetRequiredService<ClientLeafSystem>());
		services.AddSingleton<ViewRender>();
		services.AddSingleton<Hud>();
		services.AddSingleton<HudElementHelper>();
		services.AddSingleton<ViewportClientSystem>();
		services.AddSingleton<IViewRender>(x => x.GetRequiredService<ViewRender>());

		services.AddSingleton<ViewportClientSystem>();
	}

	public void IN_SetSampleTime(double frameTime) {

	}

	public void PostInit() {

	}

	public void CreateMove(int sequenceNumber, double inputSampleFrametime, bool active) {
		input.CreateMove(sequenceNumber, inputSampleFrametime, active);
	}

	public bool WriteUsercmdDeltaToBuffer(bf_write buf, int from, int to, bool isNewCommand) {
		return input.WriteUsercmdDeltaToBuffer(buf, from, to, isNewCommand);
	}
	public bool DisconnectAttempt() => false;

	public void HudText(ReadOnlySpan<char> text) {

	}

	public bool DispatchUserMessage(int msgType, bf_read msgData) {
		return usermessages.DispatchUserMessage(msgType, msgData);
	}

	public bool Init() {
		IGameSystem.Add(Singleton<ViewportClientSystem>());

		clientMode ??= new ClientModeHL2MPNormal(services, gpGlobals, HUD, Singleton<IEngineVGui>(), surface);
		HUD.Init();
		clientMode.Init();
		if (!IGameSystem.InitAllSystems())
			return false;
		clientMode.Enable();
		view.Init();
		input.Init();
		ClientVGui.CreateGlobalPanels();
		return true;
	}

	public void EncodeUserCmdToBuffer(bf_write buf, int slot) {
		input.EncodeUserCmdToBuffer(buf, slot);
	}

	public void DecodeUserCmdFromBuffer(bf_read buf, int slot) {
		input.DecodeUserCmdFromBuffer(buf, slot);
	}

	public bool HandleUiToggle() {
		return false;
	}

	public void IN_DeactivateMouse() {
		input.DeactivateMouse();
	}

	public void IN_ActivateMouse() {
		input.ActivateMouse();
	}

	public void ExtraMouseSample(double frametime, bool active) {
		input.ExtraMouseSample(frametime, active);
	}

	public void View_Render(ViewRects rects) {
		ref ViewRect rect = ref rects[0];
		if (rect.Width == 0 || rect.Height == 0)
			return;
		view.Render(rects);
	}

	public void InstallStringTableCallback(ReadOnlySpan<char> tableName) {
		// TODO: what to do here, if anything
	}

	public int IN_KeyEvent(int eventcode, ButtonCode keynum, ReadOnlySpan<char> currentBinding) {
		return input.KeyEvent(eventcode, keynum, currentBinding);
	}

	public void IN_OnMouseWheeled(int delta) {

	}

	public void IN_ClearStates() {
		input.ClearStates();
	}

	public bool ShouldAllowConsole() => true;

	public ClientFrameStage CurFrameStage;

	public void FrameStageNotify(ClientFrameStage stage) {
		CurFrameStage = stage;
		switch (stage) {
			default:
				break;

			case ClientFrameStage.RenderStart:
				OnRenderStart();
				break;
			case ClientFrameStage.NetUpdateStart:
				// TODO: AbsRecomputations/AbsQueriesValid stuff in C_BaseEntity
				Interpolation.SetLastPacketTimeStamp(engine.GetLastTimeStamp());
				break;
			case ClientFrameStage.NetUpdateEnd:
				break;
			case ClientFrameStage.RenderEnd:
				OnRenderEnd();
				break;
		}
	}

	private void OnRenderStart() {
		// TODO: the rest of this, as features get implemented

		C_BaseEntity.InterpolateServerEntities();
		C_BaseAnimating.InvalidateBoneCaches();
		C_BaseEntity.SetAbsQueriesValid(true);
		C_BaseEntity.EnableAbsRecomputations(true);

		input.CAM_Think();
		view.OnRenderStart();

		C_BaseAnimating.UpdateClientSideAnimations();

		ProcessOnDataChangedEvents();

		SimulateEntities();
		PhysicsSimulate();

		engine.FireEvents();

		C_BaseEntity.CalcAimEntPositions();
	}

	class DataChangedEvent : IPoolableObject {
		public IClientNetworkable? Entity;
		public DataUpdateType UpdateType;
		public ReusableBox<ulong>? StoredEvent;

		public DataChangedEvent() { }
		public void Init(IClientNetworkable ent, DataUpdateType updateType, ReusableBox<ulong> storedEvent) {
			Entity = ent;
			UpdateType = updateType;
			StoredEvent = storedEvent;
		}

		public void Init() { }
		public void Reset() {
			Entity = null;
			UpdateType = 0;
			StoredEvent = null;
		}
	}

	static readonly PooledValueDictionary<DataChangedEvent> DataChangedEvents = new();
	public static bool AddDataChangeEvent(C_BaseEntity ent, DataUpdateType updateType, ReusableBox<ulong> storedEvent) {
		if (storedEvent.Struct != unchecked((ulong)-1)) {
			if (updateType == DataUpdateType.Created)
				DataChangedEvents[storedEvent.Struct].UpdateType = updateType;
			return false;
		}
		else {
			storedEvent.Struct = DataChangedEvents.AddToTail();
			DataChangedEvent ev = DataChangedEvents[storedEvent.Struct];
			ev.Init(ent, updateType, storedEvent);
			return true;
		}
	}

	private static void ProcessOnDataChangedEvents() {
		foreach(var ev in DataChangedEvents) { 
			ev.StoredEvent!.Struct = unchecked((ulong)-1);

			// Send the event.
			IClientNetworkable pNetworkable = ev.Entity!;
			pNetworkable.OnDataChanged(ev.UpdateType);
		}
		DataChangedEvents.Purge();
	}

	private void PhysicsSimulate() {

	}

	private void SimulateEntities() {
		ClientThinkList().PerformThinkFunctions();
	}

	private void OnRenderEnd() {

	}

	public ClientClass? GetAllClasses() {
		return ClientClass.Head;
	}

	public RenamedRecvTableInfo? GetRenamedRecvTableInfos() {
		return RenamedRecvTableInfo.Head;
	}

	public void ErrorCreatingEntity(int entityIdx, int classIdx, int serialNumber) {
		Msg($"Entity creation failed.\n");
		Msg($"        Entity ID: {entityIdx}\n");
		Msg($"    Entity Serial: {serialNumber}\n");
		Msg($"         Class ID: {classIdx}\n");
		Msg($"     Class Lookup: {Enum.GetName((StaticClassIndices)classIdx) ?? "Failed"}\n");
	}
}
