
using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Utilities;

using System.Numerics;
using System.Runtime.CompilerServices;

using static Source.Engine.MatSysInterface;
namespace Source.Engine;

public struct ViewStack
{
	public ViewSetup View;
	public Matrix4x4 MatrixView;
	public Matrix4x4 MatrixProjection;
	public Matrix4x4 MatrixWorldToScreen;
	public bool Is2DView;
	public bool NoDraw;
}

public class Render(
	CommonHostState host_state,
	IMaterialSystem materials,
	Host Host,
	MatSysInterface MaterialSystem,
	ClientGlobalVariables gpGlobals
	)
{
	int framecount = 1;
	RefStack<ViewStack> ViewStack = new();
	Matrix4x4 MatrixView;
	Matrix4x4 MatrixProjection;
	Matrix4x4 MatrixWorldToScreen;

	ModelLoader? _modelLoader;
	ModelLoader modelLoader => _modelLoader ??= (ModelLoader)Singleton<IModelLoader>();

	IViewRender? viewRender;
	IViewRender engineRenderer => viewRender ??= Singleton<IViewRender>();

	float FOV;
	float Framerate;
	float ZNear;
	float ZFar;
	IMaterial? SkyboxOcclude;

	public Vector3 CurrentViewOrigin = new(0, 0, 0);
	public Vector3 CurrentViewForward = new(1, 0, 0);
	public Vector3 CurrentViewRight = new(0, -1, 0);
	public Vector3 CurrentViewUp = new(0, 0, 1);

	public Vector3 MainViewOrigin = new(0, 0, 0);
	public Vector3 MainViewForward = new(1, 0, 0);
	public Vector3 MainViewRight = new(0, -1, 0);
	public Vector3 MainViewUp = new(0, 0, 1);

	bool CanAccessCurrentView;

	internal void FrameBegin() {

		framecount++;
	}

	internal void FrameEnd() {

	}

	internal void PopView(Frustum frustumPlanes) {
		if (!ViewStack.Top().NoDraw) {
			using MatRenderContextPtr renderContext = new(materials);

			renderContext.MatrixMode(MaterialMatrixMode.Projection);
			renderContext.PopMatrix();

			renderContext.MatrixMode(MaterialMatrixMode.View);
			renderContext.PopMatrix();

			renderContext.MatrixMode(MaterialMatrixMode.Model);
			renderContext.PopMatrix();

			renderContext.PopRenderTargetAndViewport();
		}

		bool reset = (ViewStack.Count > 1) ? true : false;
		ViewStack.Pop();

		if (reset) {
			if (!ViewStack.Top().Is2DView) {
				ExtractMatrices();
				OnViewActive(frustumPlanes);
			}
		}
	}

	private void ExtractMatrices() {
		MatrixView = ViewStack.Top().MatrixView;
		MatrixProjection = ViewStack.Top().MatrixProjection;
		MatrixWorldToScreen = ViewStack.Top().MatrixWorldToScreen;
	}

	public void SetMainView(in Vector3 origin, in QAngle angles) {
		MainViewOrigin = origin;
		angles.Vectors(out MainViewForward, out MainViewRight, out MainViewUp);
	}

	private ref ViewSetup CurrentView() => ref ViewStack.Top().View;

	private void OnViewActive(Frustum frustumPlanes) {
		ref ViewSetup view = ref CurrentView();

		FOV = MathLib.CalcFovY(view.FOV, view.AspectRatio);

		CurrentViewOrigin = view.Origin;
		view.Angles.Vectors(out CurrentViewForward, out CurrentViewRight, out CurrentViewUp);
		CanAccessCurrentView = true;

		/*if (view.Ortho) {
			OrthoExtractFrustumPlanes(frustumPlanes);
		}
		else {
			ExtractFrustumPlanes(frustumPlanes);
		}*/

		// OcclusionSystem.SetView(view.Origin, view.FOV, MatrixView, MatrixProjection, frustumPlanes[FrustumPlane.NearZ]);

		if (!ViewStack.Top().NoDraw) {
			// R_SceneBegin();
		}
	}

	internal void Push2DView(in ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes) {
		ref ViewStack viewStack = ref ViewStack.Push();
		viewStack.View = view;
		viewStack.Is2DView = true;
		viewStack.NoDraw = (flags & ClearFlags.NoDraw) != 0;
		viewStack.MatrixView = MatrixView;
		viewStack.MatrixProjection = MatrixProjection;
		viewStack.MatrixWorldToScreen = MatrixWorldToScreen;

		ref ViewSetup topView = ref viewStack.View;
		using MatRenderContextPtr renderContext = new(materials);
		renderContext.PushRenderTargetAndViewport(renderTarget, topView.X, topView.Y, topView.Width, topView.Height);
		ClearView(topView, flags, renderTarget);

		renderContext.MatrixMode(MaterialMatrixMode.Projection);
		renderContext.PushMatrix();
		renderContext.LoadIdentity();
		renderContext.Scale(1, -1, 1);
		renderContext.Ortho(0, 0, topView.Width, topView.Height, -99999, 99999);

		renderContext.MatrixMode(MaterialMatrixMode.View);
		renderContext.PushMatrix();
		renderContext.LoadIdentity();

		renderContext.MatrixMode(MaterialMatrixMode.Model);
		renderContext.PushMatrix();
		renderContext.LoadIdentity();
	}

	private void ClearView(ViewSetup topView, ClearFlags flags, ITexture? renderTarget, ITexture? depthTexture = null) {
		bool clearColor = (flags & ClearFlags.ClearColor) != 0;
		bool clearDepth = (flags & ClearFlags.ClearDepth) != 0;
		bool clearStencil = (flags & ClearFlags.ClearStencil) != 0;
		bool forceClearWholeRenderTarget = (flags & ClearFlags.ClearFullTarget) != 0;
		bool obeyStencil = (flags & ClearFlags.ClearObeyStencil) != 0;

		if (!clearColor && !clearDepth && !clearStencil)
			return;

		using MatRenderContextPtr renderContext = new(materials);

		if (!forceClearWholeRenderTarget) {
			// if (obeyStencil)
			//	renderContext.ClearBuffersObeyStencil(clearColor, clearDepth);
			// else
			renderContext.ClearBuffers(clearColor, clearDepth, clearStencil);
		}
		else {
			// Get the render target dimensions
			int width, height;
			if (renderTarget != null) {
				width = renderTarget.GetActualWidth();
				height = renderTarget.GetActualHeight();
			}
			else {
				materials.GetBackBufferDimensions(out width, out height);
			}

			renderContext.PushRenderTargetAndViewport(renderTarget, depthTexture, 0, 0, width, height);

			// if (obeyStencil)
			// 	renderContext->ClearBuffersObeyStencil(clearColor, clearDepth);
			// else
			renderContext.ClearBuffers(clearColor, clearDepth, clearStencil);

			renderContext.PopRenderTargetAndViewport();
		}
	}

	public int FrameCount = 1;

	public void LevelInit() {
		ConDMsg("Initializing renderer...\n");

		FrameCount = 1;
		ResetLightStyles();
		DecalInit();
		LoadSkys();
		InitStudio();

		LoadWorldGeometry();

		Surface_LevelInit();
		Areaportal_LevelInit();
	}

	private void ResetLightStyles() { }
	private void DecalInit() { }
	private void LoadSkys() {
		bool success = true;
		Span<char> requestedsky = stackalloc char[128];
		ConVarRef skyname = this.skyname.Value;
		if (skyname.IsValid()) {
			ReadOnlySpan<char> skynameValue = skyname.GetString();
			skynameValue.CopyTo(requestedsky);
			requestedsky = requestedsky[..skynameValue.Length];
		}
		else {
			ConDMsg("Unable to find skyname ConVar!!!\n");
			return;
		}

		if (!LoadNamedSkys(requestedsky)) {
			success = false;

			if (((ReadOnlySpan<char>)requestedsky).Equals("sky_urb01", StringComparison.OrdinalIgnoreCase)) {
				skyname.SetValue("sky_urb01");
				if (LoadNamedSkys(skyname.GetString())) {
					ConDMsg($"Unable to load sky {requestedsky}, but successfully loaded {skyname.GetString()}\n");
					success = true;
				}
			}
		}

		if (!success)
			ConDMsg($"Unable to load sky {requestedsky}\n");
	}
	private void InitStudio() { }
	private void LoadWorldGeometry() {
		if (host_state.WorldModel == null)
			return;

		MaterialSystem.DestroySortInfo();
		MaterialSystem.RegisterLightmapSurfaces();
		MaterialSystem.CreateSortInfo();

		modelLoader.Map_LoadDisplacements(MaterialSystem.materialSortInfoArray, host_state.WorldModel!);
	}
	private void Surface_LevelInit() { }
	private void Areaportal_LevelInit() { }


	public void Init() {
		KeyValues kvs = new("Occlude");
		SkyboxOcclude = materials.CreateMaterial("__skybox_occlude", kvs);
	}

	internal void DrawSceneBegin() {

	}

	internal void DrawSceneEnd() {

	}

	internal void ViewSetupVisEx(bool novis, ReadOnlySpan<Vector3> origins, out uint returnFlags) {
		ModelLoader.Map_VisSetup(host_state.WorldModel, origins, novis, out returnFlags);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void RenderOneMesh(MatRenderContextPtr renderContext, in MatSysInterface.MeshList meshList) {
		renderContext.Bind(meshList.Material);
		meshList.Mesh.Draw();
	}

	internal void DrawWorld(DrawWorldListFlags flags, float waterZAdjust) {
		using MatRenderContextPtr renderContext = new(materials);
		Span<MatSysInterface.MeshList> meshLists = MaterialSystem.Meshes.AsSpan();

		if ((flags & DrawWorldListFlags.Skybox) != 0) {
			DrawSkybox(engineRenderer.GetZFar());
		}

		for (int i = meshLists.Length - 1; i >= 0; i--) {
			ref MatSysInterface.MeshList meshList = ref meshLists[i];

			switch (meshList.ToolTexture) {
				case MatSysInterface.ToolTexture.None:
					RenderOneMesh(renderContext, in meshList);
					break;
				default:
					// Don't draw the mesh
					break;
			}
		}
	}
	static ConVar r_drawskybox = new("1", FCvar.Cheat);

	static readonly int[] SkyTexOrder = [0, 2, 1, 3, 4, 5];
	static readonly int[] FakePlaneType = [1, -1, 2, -2, 3, -3];
	private void DrawSkybox(float zFar, int drawFlags = 0x3F) {
		if (!r_drawskybox.GetBool())
			return;

		MatRenderContextPtr renderContext = new(materials);

		// Before drawing the skybox, draw any meshes in the skybox lists only to the depth texture.
		// This deviates from Source rendering but is necessary since we aren't using the PVS to calculate
		// visible surfaces at runtime, and we need other sky-rooms to not be visible

		/*Span<int> skyboxMeshesIndices = MaterialSystem.SkyboxMeshesIndices.AsSpan();
		Span<MeshList> meshes = MaterialSystem.Meshes.AsSpan();
		renderContext.Bind(SkyboxOcclude!); // If Init() ran, this isn't null
		for (int i = 0; i < skyboxMeshesIndices.Length; i++) {
			ref MeshList meshList = ref meshes[skyboxMeshesIndices[i]];
			meshList.Mesh.Draw();
		}*/

		Vector3 normal;
		for (int i = 0; i < 6; i++, drawFlags >>= 1) {
			// Don't draw this panel of the skybox if the flag isn't set:
			if ((drawFlags & 1) == 0)
				continue;

			normal = vec3_origin;
			switch (FakePlaneType[i]) {
				case 1:
					normal[0] = 1;
					break;

				case -1:
					normal[0] = -1;
					break;

				case 2:
					normal[1] = 1;
					break;

				case -2:
					normal[1] = -1;
					break;

				case 3:
					normal[2] = 1;
					break;

				case -3:
					normal[2] = -1;
					break;
			}

			if (Vector3.Dot(CurrentViewForward, normal) < -0.29289f)
				continue;

			Span<Vector3> positionArray = stackalloc Vector3[4];
			Span<Vector2> texCoordArray = stackalloc Vector2[4];
			if (skyboxMaterials[SkyTexOrder[i]] != null) {
				renderContext.Bind(skyboxMaterials[SkyTexOrder[i]]!);

				MakeSkyVec(-1.0f, -1.0f, i, zFar, out positionArray[0], out texCoordArray[0]);
				MakeSkyVec(-1.0f, 1.0f, i, zFar, out positionArray[1], out texCoordArray[1]);
				MakeSkyVec(1.0f, 1.0f, i, zFar, out positionArray[2], out texCoordArray[2]);
				MakeSkyVec(1.0f, -1.0f, i, zFar, out positionArray[3], out texCoordArray[3]);

				IMesh mesh = renderContext.GetDynamicMesh();

				MeshBuilder meshBuilder = new();
				meshBuilder.Begin(mesh, MaterialPrimitiveType.Triangles, 4, 6);

				for (int j = 0; j < 4; ++j) {
					meshBuilder.Position3fv(positionArray[j]);
					meshBuilder.TexCoord2fv(0, texCoordArray[j]);
					meshBuilder.AdvanceVertex();
				}
				ref IndexBuilder indexBuilder = ref meshBuilder.IndexBuilder;
				indexBuilder.FastQuad(0);

				meshBuilder.End();
				mesh.Draw();

				meshBuilder.Dispose();
			}
		}
	}

	public const float SQRT3INV = 0.57735f;
	readonly static int[,] st_to_vec = {
		{ 3, -1, 2 },
		{ -3, 1, 2 },

		{ 1, 3, 2 },
		{ -1, -3, 2 },

		{ -2, -1, 3 },
		{ 2, -1, -3 }
	};
	private void MakeSkyVec(float s, float t, int axis, float zFar, out Vector3 position, out Vector2 texCoord) {
		Vector3 v = default, b = default;
		int j = default, k = default;
		float width = zFar * SQRT3INV;

		if (s < -1)
			s = -1;
		else if (s > 1)
			s = 1;
		if (t < -1)
			t = -1;
		else if (t > 1)
			t = 1;

		b[0] = s * width;
		b[1] = t * width;
		b[2] = width;

		for (j = 0; j < 3; j++) {
			k = st_to_vec[axis, j];
			if (k < 0)
				v[j] = -b[-k - 1];
			else
				v[j] = b[k - 1];
			v[j] += CurrentViewOrigin[j];
		}

		s = (s + 1) * 0.5F;
		t = (t + 1) * 0.5F;

		if (s < 1.0F / 512)
			s = 1.0F / 512;
		else if (s > 511.0F / 512)
			s = 511.0F / 512;
		if (t < 1.0F / 512)
			t = 1.0F / 512;
		else if (t > 511.0 / 512)
			t = 511.0F / 512;

		t = 1.0F - t;
		position = v;
		texCoord = new(s, t);
	}

	float Near;
	float Far;

	internal void Push3DView(in ViewSetup view, ClearFlags clearFlags, ITexture? rtColor, Frustum frustum, ITexture? rtDepth) {
		ref ViewStack writeStack = ref ViewStack.Push();
		writeStack.View = view;
		writeStack.Is2DView = false;
		writeStack.NoDraw = (clearFlags & ClearFlags.NoDraw) != 0;

		ref ViewSetup topView = ref writeStack.View;

		if (topView.AspectRatio == 0.0f)
			topView.AspectRatio = (topView.Height != 0) ? ((float)topView.Width / (float)topView.Height) : 1.0f;

		ref ViewStack viewStack = ref ViewStack.Top();
		topView.AspectRatio = ComputeViewMatrices(ref viewStack.MatrixView, ref viewStack.MatrixProjection, ref viewStack.MatrixWorldToScreen, in topView);

		Near = topView.ZNear;
		Far = topView.ZFar;

		ExtractMatrices();

		if (!writeStack.NoDraw) {
			using MatRenderContextPtr renderContext = new(materials);

			if (rtColor == null)
				rtColor = renderContext.GetRenderTarget();

			renderContext.PushRenderTargetAndViewport(rtColor, rtDepth, topView.X, topView.Y, topView.Width, topView.Height);

			ClearView(topView, clearFlags, rtColor, rtDepth);

			renderContext.DepthRange(0, 1);
			renderContext.MatrixMode(MaterialMatrixMode.Projection);
			renderContext.PushMatrix();
			renderContext.LoadMatrix(MatrixProjection);
			renderContext.MatrixMode(MaterialMatrixMode.View);
			renderContext.PushMatrix();
			renderContext.LoadMatrix(MatrixView);
			renderContext.MatrixMode(MaterialMatrixMode.Model);
			renderContext.PushMatrix();

			OnViewActive(frustum);
		}
	}

	private float ComputeViewMatrices(ref Matrix4x4 worldToView, ref Matrix4x4 viewToProjection, ref Matrix4x4 worldToProjection, in ViewSetup viewSetup) {
		float aspectRatio = viewSetup.AspectRatio;
		if (aspectRatio == 0.0f)
			aspectRatio = (viewSetup.Height != 0) ? ((float)viewSetup.Height / (float)viewSetup.Width) : 1.0f;

		// (-12152) + (MathF.Sin((float)gpGlobals.CurTime * 1) * 64))
		// ComputeViewMatrix(ref worldToView, new(-852, 907, (-12152) + (MathF.Sin((float)gpGlobals.CurTime * 1) * 64)), new QAngle(17.68f, -53.19f, 0));
		ComputeViewMatrix(ref worldToView, viewSetup.Origin, viewSetup.Angles);

		float fovX = MathLib.DEG2RAD(viewSetup.FOV);

		if (viewSetup.Ortho) {
			throw new NotImplementedException();
		}
		else if (viewSetup.OffCenter) {
			throw new NotImplementedException();
		}
		else if (viewSetup.ViewToProjectionOverride) {
			throw new NotImplementedException();
		}
		else
			viewToProjection = Matrix4x4.CreatePerspectiveFieldOfView(fovX, aspectRatio, viewSetup.ZNear, viewSetup.ZFar);

		worldToProjection = Matrix4x4.Multiply(viewToProjection, worldToView);

		return aspectRatio;
	}

	private static Matrix4x4 baseRotation;
	private static bool didInit = false;
	private void ComputeViewMatrix(ref Matrix4x4 worldToView, in Vector3 origin, in QAngle angles) {
		angles.Vectors(out Vector3 forward, out _, out Vector3 up);
		worldToView = Matrix4x4.CreateLookTo(origin, forward, up);
	}
	readonly IMaterial?[] skyboxMaterials = new IMaterial?[6];
	readonly static string[] skyboxsuffix = ["rt", "bk", "lf", "ft", "up", "dn"];
	Lazy<ConVarRef> skyname = new(() => new("sv_skyname"));

	public bool LoadNamedSkys(ReadOnlySpan<char> skyname) {
		Span<char> name = stackalloc char[MAX_PATH];
		IMaterial?[] skies = new IMaterial?[6];
		bool success = true;
		const string SKYBOX_ = "skybox/";
		SKYBOX_.CopyTo(name);
		skyname.CopyTo(name[SKYBOX_.Length..]);
		int writePos = SKYBOX_.Length + skyname.Length;
		for (int i = 0; i < 6; i++) {
			skyboxsuffix[i].CopyTo(name[writePos..]);
			skies[i] = materials.FindMaterial(name[..(writePos + 2)], MaterialDefines.TEXTURE_GROUP_SKYBOX);

			if (!skies[i].IsErrorMaterial())
				continue;

			success = false;
			break;
		}

		if (!success)
			return false;

		for (int i = 0; i < 6; i++) {
			if (skyboxMaterials[i] != null) {
				skyboxMaterials[i]!.DecrementReferenceCount();
				skyboxMaterials[i] = null;
			}

			Assert(skies[i] != null);
			skyboxMaterials[i] = skies[i];
			skyboxMaterials[i]!.IncrementReferenceCount();
		}

		return true;
	}

	internal void Shutdown() {

	}
}
