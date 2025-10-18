using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.UI;
public class OptionsDialog : PropertyDialog
{
	readonly ModInfo ModInfo = Singleton<ModInfo>();
	public OptionsDialog(Panel? parent) : base(parent, "OptionsDialog") {
		SetDeleteSelfOnClose(true);
		SetBounds(0, 0, 512, 406);
		SetSizeable(false);

		SetTitle("#GameUI_Options", true);
		// TODO

		AddPage(new OptionsSubMouse(this, null), "#GameUI_Mouse");

		SetApplyButtonVisible(true);
		GetPropertySheet().SetTabWidth(84);
	}

	public override void Activate() {
		base.Activate();
		EnableApplyButton(false);
	}

	public void OnGameUIHidden() {
		for (int i = 0; i < GetChildCount(); i++) {
			Panel child = GetChild(i);
			if (child != null && child.IsVisible())
				PostMessage(child, new KeyValues("GameUIHidden")); // todo static kv
		}
	}
}