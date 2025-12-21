global using static Game.Client.HUD.HudExtensions;

global using HudTextureDict = System.Collections.Generic.Dictionary<ulong, Game.Client.HUD.HudTexture>;

using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.MaterialSystem;
using Source.Common.Utilities;
using Source.GUI.Controls;

using System.Drawing;

using Color = Source.Color;

namespace Game.Client.HUD;

public static class HudExtensions
{
	public static T? GET_HUDELEMENT<T>() where T : IHudElement => (T?)gHUD.FindElement(typeof(T).Name);
}

struct HudTextureFileRef
{
	const int fileKeyLength = 64;
	const int hudTexturePrefix = 16;

	public InlineArray64<char> FileKey;
	public InlineArray16<char> HudTexturePrefix;
	public uint PrefixLength;
	public UtlSymbol FileKeySymbol;

	public HudTextureFileRef(ReadOnlySpan<char> fileKey, ReadOnlySpan<char> texturePrefix) {
		strcpy(FileKey, fileKey);
		strcpy(HudTexturePrefix, texturePrefix);
		PrefixLength = (uint)strlen(texturePrefix);
		FileKeySymbol = new UtlSymbol(fileKey);
	}
}

public class HudTexture
{
	public InlineArray64<char> ShortName;
	public InlineArray64<char> TextureFile;

	public int Width() => RC.Right - RC.Left;
	public int Height() => RC.Bottom - RC.Top;

	public void Precache() {

	}

	public void DrawSelf(int x, int y, in Color clr) {
		DrawSelf(x, y, Width(), Height(), clr);
	}

	public void DrawSelf(int x, int y, int w, int h, in Color clr) {
		if (RenderUsingFont) {
			surface.DrawSetTextFont(Font);
			surface.DrawSetTextColor(clr);
			surface.DrawSetTextPos(x, y);
			surface.DrawChar(CharacterInFont);
		}
		else {
			if (TextureID == TextureID.INVALID)
				return;

			surface.DrawSetTexture(TextureID);
			surface.DrawSetColor(clr);
			// todo: DrawTexturedSubRect
			// surface.DrawTexturedSubRect(x, y, x + w, y + h, texCoords[0], texCoords[1], texCoords[2], texCoords[3]);
		}
	}

	public bool RenderUsingFont;
	public bool Precached;
	public char CharacterInFont;
	public IFont? Font;
	public TextureID TextureID;
	public InlineArray4<float> TexCoords;
	public Rectangle RC;
}

[EngineComponent]
public class Hud(HudElementHelper HudElementHelper)
{
	readonly HudTextureDict Icons = [];
	public readonly List<IHudElement> HudList = [];
	internal InButtons KeyBits;
	bool HudTexturesLoaded;

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

		if (HudTexturesLoaded)
			return;

		HudTextureDict textureList = [];

		Span<char> sz = stackalloc char[128];
		strcpy(sz, "resource/hud_textures.txt");
		LoadHudTextures(textureList, sz.SliceNullTerminatedString());
		strcpy(sz, "resource/mod_textures.txt");
		LoadHudTextures(textureList, sz.SliceNullTerminatedString());

		foreach (var t in textureList)
			AddSearchableHudIconToList(t.Value);
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

	internal static void LoadHudTextures(HudTextureDict list, Span<char> filenameWithExtension) {
		KeyValues? temp, textureSection;

		KeyValues keyValuesData = new();
		if (keyValuesData.LoadFromFile(filesystem, filenameWithExtension)) {
			List<HudTextureFileRef> hudTextureFileRefs = [];
			hudTextureFileRefs.Add(new HudTextureFileRef("file", ""));

			KeyValues? textureFileRefs = keyValuesData.FindKey("TextureFileRefs");
			if (textureFileRefs != null) {
				temp = textureFileRefs.GetFirstSubKey();
				while (temp != null) {
					hudTextureFileRefs.Add(new HudTextureFileRef(temp.Name, temp.GetString("prefix", "")));
					temp = temp.GetNextKey();
				}
			}

			textureSection = keyValuesData.FindKey("TextureData");
			if (textureSection != null) {
				temp = textureSection.GetFirstSubKey();
				while (temp != null) {
					if (!temp.GetString("font", null).IsEmpty) {
						HudTexture tex = new HudTexture();

						// Key Name is the sprite name
						strcpy(tex.ShortName, temp.Name);

						// it's a font-based icon
						tex.RenderUsingFont = true;
						tex.CharacterInFont = temp.GetString("character", "\0")[0];
						strcpy(tex.TextureFile, temp.GetString("font"));

						list.Add(((ReadOnlySpan<char>)tex.ShortName).Hash(false), tex);
					}
					else {
						int iTexLeft = temp.GetInt("x", 0),
							iTexTop = temp.GetInt("y", 0),
							iTexRight = temp.GetInt("width", 0) + iTexLeft,
							iTexBottom = temp.GetInt("height", 0) + iTexTop;

						for (int i = 0; i < hudTextureFileRefs.Count(); i++) {
							ReadOnlySpan<char> fileName = temp.GetString(hudTextureFileRefs[i].FileKeySymbol, null);
							if (!fileName.IsEmpty) {
								HudTexture tex = new HudTexture();

								tex.RenderUsingFont = false;
								tex.RC.X = iTexLeft;
								tex.RC.Y = iTexTop;
								tex.RC.Width = iTexRight;
								tex.RC.Height = iTexBottom;

								strcpy(tex.ShortName, hudTextureFileRefs.AsSpan()[i].HudTexturePrefix);
								strcpy(tex.ShortName[(int)hudTextureFileRefs[i].PrefixLength..], temp.Name.SliceNullTerminatedString());
								strcpy(tex.TextureFile, fileName);

								list.Add(((ReadOnlySpan<char>)tex.ShortName).Hash(false), tex);
							}
						}
					}

					temp = temp.GetNextKey();
				}
			}
		}
	}

	internal HudTexture? GetIcon(ReadOnlySpan<char> icon) {
		if (Icons.TryGetValue(icon.Hash(false), out HudTexture? tex))
			return tex;

		return null;
	}

	internal HudTexture AddUnsearchableHudIconToList(HudTexture texture) {
		Span<char> composedName = stackalloc char[512];

		if (texture.RenderUsingFont)
			sprintf(composedName, "%s_c%i").S(texture.TextureFile).I(texture.CharacterInFont);
		else
			sprintf(composedName, "%s_%i_%i_%i_%i").S(texture.TextureFile).I(texture.RC.X).I(texture.RC.Y).I(texture.RC.Width).I(texture.RC.Height);


		HudTexture? icon = GetIcon(composedName);
		if (icon != null)
			return icon;

		HudTexture newTexture = new();
		texture.CopyInstantiatedReferenceTo(newTexture);

		SetupNewHudTexture(newTexture);

		Icons.Add(composedName.Hash(false), newTexture);
		return newTexture;
	}

	internal HudTexture AddSearchableHudIconToList(HudTexture texture) {
		HudTexture? icon = GetIcon(texture.ShortName);
		if (icon != null)
			return icon;

		HudTexture newTexture = new();
		texture.CopyInstantiatedReferenceTo(newTexture);

		SetupNewHudTexture(newTexture);

		Icons.Add(((ReadOnlySpan<char>)newTexture.ShortName).Hash(false), newTexture);
		return newTexture;
	}

	private void SetupNewHudTexture(HudTexture t) {
		if (t.RenderUsingFont) {
			IScheme scheme = vguiSchemeManager.GetScheme("ClientScheme")!;
			t.Font = scheme.GetFont(t.TextureFile, true);
			t.RC.Y = 0;
			t.RC.X = 0;
			t.RC.Width = surface.GetCharacterWidth(t.Font, t.CharacterInFont);
			t.RC.Height = surface.GetFontTall(t.Font);
		}
		else {
			// Set up texture id and texture coordinates
			t.TextureID = surface.CreateNewTextureID();
			surface.DrawSetTextureFile(t.TextureID, t.TextureFile, 0, false);

			surface.DrawGetTextureSize(t.TextureID, out int wide, out int tall);

			t.TexCoords[0] = (float)(t.RC.X + 0.5f) / (float)wide;
			t.TexCoords[1] = (float)(t.RC.Y + 0.5f) / (float)tall;
			t.TexCoords[2] = (float)(t.RC.Width - 0.5f) / (float)wide;
			t.TexCoords[3] = (float)(t.RC.Height - 0.5f) / (float)tall;
		}
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
