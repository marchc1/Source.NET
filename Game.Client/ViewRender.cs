global using static Game.Client.ViewRenderConVars;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine;

using System.Numerics;

namespace Game.Client;

public static class ViewRenderConVars
{
	internal readonly static ConVar cl_maxrenderable_dist = new("3000", FCvar.Cheat, "Max distance from the camera at which things will be rendered");
	internal readonly static ConVar r_drawopaqueworld = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawtranslucentworld = new("1", FCvar.Cheat);
	internal readonly static ConVar r_3dsky = new("1", 0, "Enable the rendering of 3d sky boxes");
	internal readonly static ConVar r_skybox = new("1", FCvar.Cheat, "Enable the rendering of sky boxes");
	internal readonly static ConVar r_drawviewmodel = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawtranslucentrenderables = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawopaquerenderables = new("1", FCvar.Cheat);
	internal readonly static ConVar r_threaded_renderables = new("0", 0);
}

public class RenderExecutor
{
	public virtual void AddView(Rendering3dView view) { }
	public virtual void Execute() { }
	public RenderExecutor(ViewRender mainView) {
		this.mainView = mainView;
	}
	protected ViewRender mainView;
}

public class SimpleRenderExecutor : RenderExecutor
{
	public SimpleRenderExecutor(ViewRender mainView) : base(mainView) {

	}
	public override void AddView(Rendering3dView view) {
		Base3dView? prevRenderer = mainView.SetActiveRenderer(view);
		view.Draw();
		mainView.SetActiveRenderer(prevRenderer);
	}
	public override void Execute() {

	}
}

public enum ViewID : sbyte
{
	Illegal = -2,
	None = -1,
	Main = 0,
	Sky3D = 1,
	Monitor = 2,
	Reflection = 3,
	Refraction = 4,
	IntroPlayer = 5,
	IntroCamera = 6,
	ShadowDepthTexture = 7,
	SSAO = 8,
	Count
}

public class Base3dView
{
	protected Frustum frustum;
	protected ViewRender mainView;
	public Base3dView(ViewRender mainView) {
		this.mainView = mainView;
		frustum = mainView.GetFrustum();
	}

	protected ViewSetup setup;
	public Frustum GetFrustrum() => frustum;
	public virtual DrawFlags GetDrawFlags() => 0;
}
public class BaseWorldView : Rendering3dView
{
	public BaseWorldView(ViewRender mainView) : base(mainView) { }
	protected void DrawSetup(float waterHeight, DrawFlags setupFlags, float waterZAdjust) {
		ViewID savedViewID = ViewRender.CurrentViewID;
		ViewRender.CurrentViewID = ViewID.Illegal;
		BuildRenderableRenderLists(savedViewID);

		ViewRender.CurrentViewID = savedViewID;
	}
	protected void DrawExecute(float waterHeight, ViewID viewID, float waterZAdjust) {
		using MatRenderContextPtr renderContext = new(mainView.materials);
		renderContext.ClearBuffers(false, true, false);

		RenderDepthMode depthMode = RenderDepthMode.Normal;

		if ((DrawFlags & DrawFlags.DrawEntities) != 0) {
			DrawWorld(waterZAdjust);
			DrawOpaqueRenderables(depthMode);
		}
	}
}

public class SimpleWorldView : BaseWorldView
{
	public SimpleWorldView(ViewRender mainView) : base(mainView) { }
	public void Setup(in ViewSetup view, ClearFlags clearFlags, bool drawSkybox) {
		base.Setup(in view);

		ClearFlags = clearFlags;
		DrawFlags = DrawFlags.DrawEntities;

		if (drawSkybox)
			DrawFlags |= DrawFlags.DrawSkybox;
	}
	public override void Draw() {
		using MatRenderContextPtr renderContext = new(mainView.materials);
		DrawSetup(0, DrawFlags, 0);
		DrawExecute(0, ViewRender.CurrentViewID, 0);
	}
}
public class Rendering3dView : Base3dView
{
	protected DrawFlags DrawFlags;
	protected ClearFlags ClearFlags;
	protected ViewSetup ViewSetup;

	ClientRenderablesList RenderablesList = null!;

	public Rendering3dView(ViewRender mainView) : base(mainView) {

	}
	protected void BuildRenderableRenderLists(ViewID viewID) {
		//  if (viewID != ViewID.ShadowDepthTexture)
		//  	render.BeginUpdateLightmaps();
		mainView.IncRenderablesListsNumber();
		SetupRenderablesList(viewID);
	}
	public virtual void Setup(in ViewSetup setup) {
		ViewSetup = setup; // copy to our ViewSetup
		ReleaseLists();

		RenderablesList = ClientRenderablesList.Shared.Alloc();
	}
	public virtual void ReleaseLists() {
		ClientRenderablesList.Shared.Free(RenderablesList);
	}
	public override DrawFlags GetDrawFlags() {
		return DrawFlags;
	}
	public virtual void Draw() {

	}

	protected void DrawOpaqueRenderables(RenderDepthMode depthMode) {
		if (!r_drawopaquerenderables.GetBool())
			return;

		if (!mainView.ShouldDrawEntities())
			return;

		render.SetBlend(1);

		// TODO: do this properly, lazy right now
		int count = RenderablesList.Count(RenderGroup.OpaqueEntity);
		for (int i = 0; i < count; i++) {
			ref ClientRenderablesList.Entry entry = ref RenderablesList[RenderGroup.OpaqueEntity, i];
			if (entry.Renderable != null)
				DrawOpaqueRenderable(entry.Renderable, entry.TwoPass, depthMode);
		}
	}

	private void DrawOpaqueRenderable(IClientRenderable ent, bool twoPass, RenderDepthMode depthMode, StudioFlags defaultFlags = 0) {
		Span<float> color = stackalloc float[3];

		ent.GetColorModulation(color);
		render.SetColorModulation(color);

		StudioFlags flags = defaultFlags | StudioFlags.Render;
		if (twoPass)
			flags |= StudioFlags.TwoPass;

		if (depthMode == RenderDepthMode.Shadow) {
			flags |= StudioFlags.ShadowDepthTexture;
		}
		else if (depthMode == RenderDepthMode.SSAO) {
			flags |= StudioFlags.SSAODepthTexture;
		}

		// todo: entity clip planes

		Assert(view.GetCurrentlyDrawingEntity() == null);
		view.SetCurrentlyDrawingEntity(ent.GetIClientUnknown().GetBaseEntity());
		ent.DrawModel(flags);
		view.SetCurrentlyDrawingEntity(null);
	}

	protected void EnableWorldFog() => throw new NotImplementedException();
	protected void SetupRenderablesList(ViewID viewID) {
		// Clear the list.
		int i;
		for (i = 0; i < (int)RenderGroup.Count; i++)
			RenderablesList.RenderGroupCounts[i] = 0;

		// Now collate the entities in the leaves.
		if (mainView.ShouldDrawEntities()) {
			SetupRenderInfo setupInfo = default;
			setupInfo.RenderFrame = mainView.BuildRenderablesListsNumber();
			// setupInfo.DetailBuildFrame = mainView.BuildWorldListsNumber();  
			setupInfo.RenderList = RenderablesList;
			// setupInfo.DrawDetailObjects = ClientMode.ShouldDrawDetailObjects() && r_DrawDetailProps.GetInt();
			setupInfo.DrawTranslucentObjects = (viewID != ViewID.ShadowDepthTexture);

			setupInfo.RenderOrigin = setup.Origin;
			setupInfo.RenderForward = ViewRender.CurrentRenderForward;

			float fMaxDist = cl_maxrenderable_dist.GetFloat();

			setupInfo.RenderDistSq = (viewID == ViewID.ShadowDepthTexture) ? Math.Min(setup.ZFar, fMaxDist) : fMaxDist;
			setupInfo.RenderDistSq *= setupInfo.RenderDistSq;

			clientLeafSystem.BuildRenderablesList(setupInfo);
		}
	}
	protected void DrawWorld(float waterZAdjust) {
		DrawWorldListFlags engineFlags = BuildEngineDrawWorldListFlags(DrawFlags);
		mainView.render.DrawWorld(engineFlags, waterZAdjust);
	}

	private DrawWorldListFlags BuildEngineDrawWorldListFlags(DrawFlags drawFlags) {
		DrawWorldListFlags engineFlags = 0;

		if ((drawFlags & DrawFlags.DrawSkybox) != 0)
			engineFlags |= DrawWorldListFlags.Skybox;

		if ((drawFlags & DrawFlags.RenderAbovewater) != 0) {
			engineFlags |= DrawWorldListFlags.StrictlyAboveWater;
			engineFlags |= DrawWorldListFlags.IntersectsWater;
		}

		if ((drawFlags & DrawFlags.RenderUnderwater) != 0) {
			engineFlags |= DrawWorldListFlags.StrictlyUnderWater;
			engineFlags |= DrawWorldListFlags.IntersectsWater;
		}

		if ((drawFlags & DrawFlags.RenderWater) != 0)
			engineFlags |= DrawWorldListFlags.WaterSurface;

		if ((drawFlags & DrawFlags.ClipSkybox) != 0)
			engineFlags |= DrawWorldListFlags.ClipSkybox;

		if ((drawFlags & DrawFlags.ShadowDepthMap) != 0)
			engineFlags |= DrawWorldListFlags.ShadowDepth;

		if ((drawFlags & DrawFlags.RenderRefraction) != 0)
			engineFlags |= DrawWorldListFlags.Refraction;

		if ((drawFlags & DrawFlags.RenderReflection) != 0)
			engineFlags |= DrawWorldListFlags.Reflection;

		if ((drawFlags & DrawFlags.SSAODepthPass) != 0) {
			engineFlags |= DrawWorldListFlags.SSAO | DrawWorldListFlags.StrictlyUnderWater | DrawWorldListFlags.IntersectsWater | DrawWorldListFlags.StrictlyAboveWater;
			engineFlags &= ~(DrawWorldListFlags.WaterSurface | DrawWorldListFlags.Refraction | DrawWorldListFlags.Reflection);
		}

		return engineFlags;
	}
}

public class SkyboxView : Rendering3dView
{
	SafeFieldPointer<PlayerLocalData, Sky3DParams> Sky3dParams = new();
	public SkyboxView(ViewRender mainView) : base(mainView) {

	}

	public bool Setup(in ViewSetup viewRender, ref ClearFlags clearFlags, ref SkyboxVisibility skyboxVisible) {
		base.Setup(in viewRender);

		skyboxVisible = ComputeSkyboxVisibility();
		Sky3dParams = PreRender3dSkyboxWorld(ref skyboxVisible);
		if (Sky3dParams.IsNull)
			return false;

		ClearFlags = clearFlags;
		clearFlags &= ~(ClearFlags.ClearColor | ClearFlags.ClearDepth | ClearFlags.ClearStencil | ClearFlags.ClearFullTarget);
		clearFlags |= ClearFlags.ClearDepth;

		DrawFlags = DrawFlags.RenderUnderwater | DrawFlags.RenderAbovewater | DrawFlags.RenderWater;
		if (r_skybox.GetBool())
			DrawFlags |= DrawFlags.DrawSkybox;

		return true;
	}

	public override void Draw() {
		ITexture? rtColor = null;
		ITexture? rtDepth = null;
		if (ViewSetup.StereoEye != StereoEye.Mono)
			throw new Exception("No support for multi-stereo-eye yet");
		DrawInternal(ViewID.Sky3D, true, rtColor, rtDepth);
	}

	private void DrawInternal(ViewID skyBoxViewID, bool invokePreAndPostRender, ITexture? rtColor, ITexture? rtDepth) {
		ref Sky3DParams sky3dParams = ref Sky3dParams.Get();

		ViewSetup.ZNear = 2;
		ViewSetup.ZFar = WorldSize.MAX_TRACE_LENGTH;
		if (sky3dParams.Scale > 0)
			ViewSetup.Origin *= 1f / sky3dParams.Scale;
		ViewSetup.Origin += sky3dParams.Origin;

		render.ViewSetupVisEx(false, new(ref sky3dParams.Origin), out _);
		render.Push3DView(in ViewSetup, ClearFlags, rtColor, GetFrustrum(), rtDepth);

		SetupCurrentView(in setup.Origin, in setup.Angles, skyBoxViewID);

		if (invokePreAndPostRender)
			IGameSystem.PreRenderAllSystems();

		BuildRenderableRenderLists(skyBoxViewID);

		DrawWorld(0);

		if (invokePreAndPostRender) {
			IGameSystem.PostRenderAllSystems();
			// FinishCurrentView();
		}

		render.PopView(GetFrustrum());
	}


	private void SetupCurrentView(in Vector3 origin, in QAngle angles, ViewID viewID) {
		ViewRender.CurrentRenderOrigin = origin;
		ViewRender.CurrentRenderAngles = angles;
		ViewRender.CurrentViewID = viewID;
	}

	private SafeFieldPointer<PlayerLocalData, Sky3DParams> PreRender3dSkyboxWorld(ref SkyboxVisibility skyboxVisible) {
		if ((skyboxVisible != SkyboxVisibility.Skybox3D) && r_3dsky.GetInt() != 1)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		if (r_3dsky.GetInt() == 0)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		PlayerLocalData local = player.Local;
		if (local.Skybox3D.Area == 255)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		return new(local, GetSkybox3DRef);
	}

	static ref Sky3DParams GetSkybox3DRef(PlayerLocalData local) => ref local.Skybox3D;

	private SkyboxVisibility ComputeSkyboxVisibility() {
		return engine.IsSkyboxVisibleFromPoint(ViewSetup.Origin);
	}
}
