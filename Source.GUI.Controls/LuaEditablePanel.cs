#if GMOD_DLL
using Source.Common.GUI;

namespace Source.GUI.Controls;

public class LuaEditablePanel : EditablePanel
{
	static LuaEditablePanel() => ChainToAnimationMap<LuaEditablePanel>();

	IPanel? PreviousModal;

	public LuaEditablePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		PreviousModal = null;
		SetBuildModeEditable(true);
	}

	// FIXME #37
	public override void Dispose() {
		if (Input.GetAppModalSurface() == this) {
			Input.ReleaseAppModalSurface();
			if (PreviousModal != null) {
				Input.SetAppModalSurface(PreviousModal);
				PreviousModal = null;
			}
		}

		base.Dispose();
	}

	public override void OnChildAdded(IPanel child) => base.OnChildAdded(child);

	public override void MarkForDeletion() {
		if (Input.GetAppModalSurface() == this) {
			Input.ReleaseAppModalSurface();
			if (PreviousModal != null) {
				Input.SetAppModalSurface(PreviousModal);
				PreviousModal = null;
			}
		}

		base.MarkForDeletion();
	}

	public override void DoModal() { // TODO: Should this be an override?
		if (Surface.IsCursorVisible()) {
			PreviousModal = Input.GetAppModalSurface();
			Input.SetAppModalSurface(this);
		}
	}
}
#endif
