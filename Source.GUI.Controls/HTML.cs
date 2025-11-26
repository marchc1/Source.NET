namespace Source.GUI.Controls;

class HTMLInterior : Panel
{
	public HTMLInterior(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}

class HTMLPopup : Frame
{
	public HTMLPopup(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

		SetPostChildPaintEnabled(true);

	}
}

public class HTML : Panel
{
	public static Panel Create_HTML() => new HTML(null, null);

	public HTML(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}
}
