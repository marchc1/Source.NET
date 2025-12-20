using Game.Shared;

using Source;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HUD;

struct WRect
{
	public int left;
	public int right;
	public int top;
	public int bottom;
}

class HudTexture
{
	string ShortName;
	string TextureFile;
	bool RenderUsingFont;
	bool Precached;
	string CharacterInFont;
	IFont Font;
	int TextureId;
	float[] TexCoords = new float[4];
	WRect RC;

	HudTexture() { }

	void Precache() { }

	void DrawSelf(int x, int y, Color clr) { }

	void DrawSelf(int x, int y, int w, int h, Color clr) { }

	void DrawSelfCropped(int x, int y, int cropx, int cropy, int cropw, int croph, int finalWidth, int finalHeight, Color clr) { }

	void DrawSelfCropped(int x, int y, int cropx, int cropy, int cropw, int croph, Color clr) { }

	// int EffectiveWidth(float scale) { }

	// int EffectiveHeight(float scale) { }
}

[EngineComponent]
public class Hud(HudElementHelper HudElementHelper, IFileSystem filesystem)
{
	public readonly List<IHudElement> HudList = [];
	internal InButtons KeyBits;

	public void Init() {
		HudElementHelper.CreateAllElements(this);
		foreach (var element in HudList)
			element.Init();

		KeyValues kv = new KeyValues("layout");
		if (kv.LoadFromFile(filesystem, "scripts/HudLayout.res")) {
			int numelements = HudList.Count;

			for (int i = 0; i < numelements; i++) {
				IHudElement element = HudList[i];

				if (element is not Panel panel) {
					Msg($"Non-vgui hud element {HudList[i].ElementName}\n");
					continue;
				}

				KeyValues? key = kv.FindKey(panel.GetName(), false);
				if (key == null)
					Msg($"Hud element '{element.ElementName}' doesn't have an entry '{panel.GetName()}' in scripts/HudLayout.res\n");

				if (!element.IsParentedToClientDLLRootPanel && panel.GetParent() == null)
					DevMsg($"Hud element '{element.ElementName}'/'{panel.GetName()}' doesn't have a parent\n");
			}

		}
	}
	internal void AddHudElement(IHudElement element) {
		HudList.Add(element);
		element.NeedsRemove = true;
	}

	public IHudElement? FindElement(ReadOnlySpan<char> name) {
		foreach (var hudElement in HudList) {
			if (name.Equals(hudElement.ElementName, StringComparison.OrdinalIgnoreCase))
				return hudElement;
		}

		DevWarning(1, $"Could not find HUD element: {name}\n");
		Assert(false);
		return null;
	}

	public int GetSensitivity() {
		return 0;
	}

	internal void ResetHUD() {
		clientMode.GetViewportAnimationController()!.CancelAllAnimations();
	}

	internal void RefreshHudTextures() {
	}

	internal bool IsHidden(HideHudBits hudFlags) {
		if (!engine.IsInGame())
			return true;

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return true;

		HideHudBits hideHud = (HideHudBits)player.Local.HideHUD;
		// todo: hidehud convar

		if ((hideHud & HideHudBits.All) != 0)
			return true;

		if ((hideHud & HideHudBits.PlayerDead) != 0 && player.GetHealth() <= 0 && !player.IsAlive())
			return true;

		if ((hideHud & HideHudBits.NeedSuit) != 0 && !player.IsSuitEquipped())
			return true;

		return (hudFlags & hideHud) != 0;
	}

	internal bool IsRenderGroupLockedFor(IHudElement hudElement, int groupIndex) {
		return false; // todo
	}
}

public class HudElementHelper
{
	Hud HUD;
	public void CreateAllElements(Hud HUD) {
		this.HUD = HUD;
		var declaredHudElements = ReflectionUtils.GetLoadedTypesWithAttribute<DeclareHudElementAttribute>();
		foreach (var kvp in declaredHudElements) {
			Type type = kvp.Key;
			DeclareHudElementAttribute hudElement = kvp.Value;
			string name = hudElement.Name ?? type.Name;

			IHudElement? element = Activator.CreateInstance(type, [name]) as IHudElement;
			if (element != null)
				HUD.AddHudElement(element);
		}
	}
}
