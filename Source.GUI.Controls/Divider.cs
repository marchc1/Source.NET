using Source.Common.GUI;

namespace Source.GUI.Controls;

public class Divider : Panel
{
	public static Panel Create_Divider() => new Divider(null, null);
	public Divider(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) => SetSize(128, 2);
	public override void ApplySchemeSettings(IScheme scheme) {
		SetBorder(scheme.GetBorder("ButtonDepressedBorder"));
		base.ApplySchemeSettings(scheme);
	}
}
