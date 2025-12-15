using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;

using System.Drawing;

namespace Source.Engine;


public class VideoMode_Common(Sys Sys, IServiceProvider services, IFileSystem fileSystem, IMaterialSystem materials, RenderUtils renderUtils, ICommandLine CommandLine) : IVideoMode
{
	VMode mode = new();
	bool Windowed;
	bool ClientViewRectDirty = true;
	int ModeWidth;
	int ModeHeight;
	int ModeBPP;
	int UIWidth;
	int UIHeight;
	int StereoWidth;
	int StereoHeight;

	// View rects. Never shrinks
	readonly ViewRects ClientViewRect = new([new()]);

	public ViewRects GetClientViewRect() {
		RecomputeClientViewRect();
		return ClientViewRect;
	}

	private void RecomputeClientViewRect() {
		if (!Sys.InEditMode()) {
			if (!ClientViewRectDirty)
				return;
		}

		ClientViewRectDirty = false;

		MatRenderContextPtr renderContext = new(materials);

		renderContext.GetRenderTargetDimensions(out int width, out int height);
		ref ViewRect viewrect = ref ClientViewRect[0];
		viewrect.X = 0;
		viewrect.Y = 0;
		viewrect.Width = width;
		viewrect.Height = height;

		if (width == 0 || height == 0)
			ClientViewRectDirty = true;
	}

	public ref VMode RequestedWindowVideoMode() {
		return ref mode;
	}

	public void ResetCurrentModeForNewResolution(int width, int height, bool windowed) {
		ref VMode mode = ref RequestedWindowVideoMode();
		ModeWidth = mode.Width;
		ModeHeight = mode.Height;
		UIWidth = mode.Width;
		UIHeight = mode.Height;
		StereoWidth = mode.Width;
		StereoHeight = mode.Height;
	}

	public bool IsWindowedMode() => Windowed;
	public int GetModeWidth() => ModeWidth;
	public int GetModeHeight() => ModeHeight;
	public int GetModeBPP() => ModeBPP;
	public int GetModeStereoWidth() => StereoWidth;
	public int GetModeStereoHeight() => StereoHeight;
	public int GetModeUIWidth() => UIWidth;
	public int GetModeUIHeight() => UIHeight;

	public void AdjustWindow(int width, int height, int bpp, bool windowed) {
		IGame game = services.GetRequiredService<IGame>();
		ILauncherManager launcherMgr = services.GetRequiredService<ILauncherManager>();

		Rectangle windowRect = Rectangle.FromLTRB(
			0,
			0,
			width,
			height
			);

		game.SetWindowSize(width, height);
		launcherMgr.CenterWindow(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
		MarkClientViewRectDirty();
	}

	public void MarkClientViewRectDirty() => ClientViewRectDirty = true;

	public bool CreateGameWindow(int width, int height, bool windowed) {
		if (width != 0 && height != 0 && windowed) {
			ref VMode requested = ref RequestedWindowVideoMode();
			requested.Width = width;
			requested.Height = height;
		}

		if (true) { // InEditMode(), we aren't doing edit mode right now, so just true
			ResetCurrentModeForNewResolution(width, height, windowed);

			// Aggggghhh i hate this
			services.GetRequiredService<IGraphicsProvider>().PrepareContext(services.GetRequiredService<MaterialSystem_Config>().Driver);
			IGame game = services.GetRequiredService<IGame>();
			if (!game.CreateGameWindow(width, height, windowed))
				return false;

			AdjustWindow(GetModeWidth(), GetModeHeight(), GetModeBPP(), IsWindowedMode());
			if (!SetMode(GetModeWidth(), GetModeHeight(), IsWindowedMode()))
				return false;

			DrawStartupGraphic();
		}

		return true;
	}

	public void DrawStartupGraphic() {
		bool debugstartup = CommandLine.FindParm("-debugstartupscreen") > 0;
		SetupStartupGraphic();

		if (backgroundTexture == null)
			return;

		using MatRenderContextPtr renderContext = new(materials);

		Span<char> startupGraphicName = stackalloc char[MAX_PATH];
		// ComputeStartupGraphicName(startupGraphicName);

		if (debugstartup || true)
			"console/background01.vtf".CopyTo(startupGraphicName);

		KeyValues keyValues = new KeyValues("UnlitGeneric");
		keyValues.SetString("$basetexture", startupGraphicName);
		keyValues.SetInt("$ignorez", 1);
		keyValues.SetInt("$nofog", 1);
		keyValues.SetInt("$no_fullbright", 1);
		keyValues.SetInt("$nocull", 1);
		IMaterial material = materials.CreateMaterial("__background", keyValues);

		keyValues = new KeyValues("UnlitGeneric");
		keyValues.SetString("$basetexture", "console/startup_loading.vtf");
		keyValues.SetInt("$translucent", 1);
		keyValues.SetInt("$ignorez", 1);
		keyValues.SetInt("$nofog", 1);
		keyValues.SetInt("$no_fullbright", 1);
		keyValues.SetInt("$nocull", 1);
		IMaterial loadingMaterial = materials.CreateMaterial("__loading", keyValues);

		int w = GetModeStereoWidth();
		int h = GetModeStereoHeight();
		int tw = backgroundTexture!.Width();
		int th = backgroundTexture!.Height();
		int lw = loadingTexture!.Width();
		int lh = loadingTexture!.Height();


		if (false && debugstartup) {
			for (int repeat = 0; repeat < 100000; repeat++) {
				renderContext.Viewport(0, 0, w, h);
				renderContext.DepthRange(0, 1);
				renderContext.ClearColor3ub(0, (byte)((repeat & 0x7) << 3), 0);
				renderContext.ClearBuffers(true, true, true);

				if (true)  // draw normal BK
				{
					float depth = 0.55f;
					int slide = (repeat) % 200; // 100 down and 100 up
					if (slide > 100) {
						slide = 200 - slide;        // aka 100-(slide-100).
					}

					// stop sliding about
					slide = 0;

					renderUtils.DrawScreenSpaceRectangle(material, 0, 0 + slide, w, h - 50, 0, 0, tw - 1, th - 1, tw, th, null, 1, 1, depth);
					renderUtils.DrawScreenSpaceRectangle(loadingMaterial, w - lw, h - lh + slide / 2, lw, lh, 0, 0, lw - 1, lh - 1, lw, lh, null, 1, 1, depth - 0.1f);
				}

				if (true) {
					// draw a grid too
					int grid_size = 8;
					float depthacc = 0.0f;
					float depthinc = 1.0f / ((grid_size * grid_size) + 1);

					for (int x = 0; x < grid_size; x++) {
						float cornerx = x * 20.0f;

						for (int y = 0; y < grid_size; y++) {
							float cornery = ((float)y) * 20.0f;

							renderUtils.DrawScreenSpaceRectangle(material, 10 + (int)cornerx, 10 + (int)cornery, 15, 15, 0, 0, tw - 1, th - 1, tw, th, null, 1, 1, depthacc);

							depthacc += depthinc;
						}
					}
				}

				materials.SwapBuffers();
			}
		}
		else {
			renderContext.Viewport(0, 0, w, h);
			renderContext.DepthRange(0, 1);
			// SetToneMappingScaleLinear - what does it do... (in this context)
			// I guess it just sets it to 1, 1, 1 but still, need to review how we'd even replicate tone mapping 
			float depth = 0.5f;

			for (int i = 0; i < 2; i++) {
				renderContext.ClearColor3ub(0, 0, 0);
				renderContext.ClearBuffers(true, true, true);
				renderUtils.DrawScreenSpaceRectangle(material, 0, 0, w, h, 0, 0, tw - 1, th - 1, tw, th, null, 1, 1, depth);
				renderUtils.DrawScreenSpaceRectangle(loadingMaterial, w - lw, h - lh, lw, lh, 0, 0, lw - 1, lh - 1, lw, lh, null, 1, 1, depth);
				materials.SwapBuffers();
			}
		}
	}

	IVTFTexture? backgroundTexture;
	IVTFTexture? loadingTexture;

	private void SetupStartupGraphic() {
		string material = "materials/console/background01.vtf";
		backgroundTexture = LoadVTF(material);
		if (backgroundTexture == null) {
			Error($"Can't find background image '{material}'\n");
			return;
		}

		loadingTexture = LoadVTF("materials/console/startup_loading.vtf");
		if (loadingTexture == null) {
			Error($"Can't find background image materials/console/startup_loading.vtf\n");
			return;
		}
	}

	private IVTFTexture? LoadVTF(string material) {
		using IFileHandle? handle = fileSystem.Open(material, FileOpenOptions.Read);
		if (handle != null) {
			IVTFTexture texture = IVTFTexture.Create();
			if (!texture.Unserialize(handle)) {
				Error($"Invalid or corrupt texture {material}\n");
			}
			// texture.ConvertImageFormat(ImageFormat.RGBA8888, false);
			return texture;
		}

		return null;
	}

	public void SetGameWindow(nint window) {
		throw new NotImplementedException();
	}

	public virtual bool SetMode(int width, int height, bool windowed) {
		return false;
	}
}
public class VideoMode_MaterialSystem(Sys Sys, IMaterialSystem materials, IGame game, IServiceProvider services, IFileSystem fileSystem, RenderUtils renderUtils, ICommandLine commandLine)
	: VideoMode_Common(Sys, services, fileSystem, materials, renderUtils, commandLine)
{
	bool SetModeOnce;
	public override bool SetMode(int width, int height, bool windowed) {
		ref VMode mode = ref RequestedWindowVideoMode();
		MaterialSystem_Config config = services.GetRequiredService<MaterialSystem_Config>();
		config.VideoMode.Width = mode.Width;
		config.VideoMode.Height = mode.Height;
		config.VideoMode.RefreshRate = mode.RefreshRate;

		if (!SetModeOnce) {
			if (!materials.SetMode(game.GetMainDeviceWindow(), config)) {

			}
			SetModeOnce = true;

			InitStartupScreen();
			return true;
		}

		return true;
	}

	private void InitStartupScreen() {

	}
}