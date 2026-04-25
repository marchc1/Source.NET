global using static Game.Client.HUD.PDumpPanelGlobals;

using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Client.HUD;

public static class PDumpPanelGlobals
{
	public static PDumpPanel? g_PDumpPanel = null;
	public static PDumpPanel GetPDumpPanel() {
		return g_PDumpPanel!;
	}
}

[DeclareHudElement(Name = "CPDumpPanel")]
public class PDumpPanel : Panel, IHudElement
{
	public const int DUMP_CLASSNAME_SIZE = 128;
	public const int DUMP_STRING_SIZE = 128;

	public string? ElementName { get; set; }
	public HideHudBits HiddenBits { get; set; }
	public bool Active { get; set; }
	public bool NeedsRemove { get; set; }
	public bool IsParentedToClientDLLRootPanel { get; set; }
	public List<int> HudRenderGroups { get; set; } = [];

	public PDumpPanel(string elementName) : base(null, "HudPredictionDump") {
		((IHudElement)this).Ctor(elementName);

		g_PDumpPanel = this;
		Panel parent = clientMode.GetViewport();
		SetParent(parent);
		SetProportional(false);
	}

	public override void OnDelete() {
		base.OnDelete();
		g_PDumpPanel = null;
	}

	public override void ApplySettings(KeyValues resourceData) {
		SetProportional(false);
		base.ApplySettings(resourceData);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetProportional(false);
		SetPaintBackgroundEnabled(false);
	}

	public bool ShouldDraw() {
		if (DumpEntityInfo.Count == 0)
			return false;

		return IHudElement.DefaultShouldDraw(this);
	}

	[InlineArray(DUMP_CLASSNAME_SIZE)] struct InlineArrayDumpClassNameSize<T> { T? first; }
	[InlineArray(DUMP_STRING_SIZE)] struct InlineArrayDumpStringSize<T> { T? first; }

	public void Clear(){
		DumpEntityInfo.Clear();
	}

	public void DumpEntity(C_BaseEntity ent, int commandsAcknowledged) {
		DataFrame? originalStateData = null;
		DataFrame? predictedStateData = null;

		if (ent.GetPredictable()) {
			originalStateData = ent.GetOriginalNetworkDataObject();
			predictedStateData = ent.GetPredictedFrame(commandsAcknowledged - 1);
		}
		else {
			// TODO: No good way to do this right now I think...
			Clear(); 
			return;
		}

		Assert(originalStateData != null);
		Assert(predictedStateData != null);

		Clear();

		PredictionCopy datacompare = new(PredictionCopyType.Everything,
			originalStateData, 
			predictedStateData,
			true,  // counterrors
			true,  // reporterrors
			false, // copy data
			true,   // describe fields
			StaticDumpComparision
		);
		// Don't spew debugging info
		datacompare.TransferData("", -1, ent.GetPredDescMap());

		DumpEntityHandle.Set(ent);
	}

	public static void StaticDumpComparision(ReadOnlySpan<char> classname, ReadOnlySpan<char> fieldname, ReadOnlySpan<char> fieldtype, bool networked, bool noterrorchecked, bool differs, bool withintolerance, ReadOnlySpan<char> value) {
		if (g_PDumpPanel == null)
			return;

		g_PDumpPanel.DumpComparision(classname, fieldname, fieldtype, networked, noterrorchecked, differs, withintolerance, value);
	}
	public void DumpComparision(ReadOnlySpan<char> classname, ReadOnlySpan<char> fieldname, ReadOnlySpan<char> fieldtype, bool networked, bool noterrorchecked, bool differs, bool withintolerance, ReadOnlySpan<char> value) {
		if (fieldname.IsEmpty)
			return;

		DumpEntityInfo.Add(default); int idx = DumpEntityInfo.Count - 1;

		ref DumpInfo slot = ref DumpEntityInfo.AsSpan()[idx];

		sprintf(slot.ClassName, "%s").S(classname);
		slot.Networked = networked;
		sprintf(slot.FieldString, "%s %s").S(fieldname).S(value);

		slot.Differs = differs;
		slot.WithinTolerance= withintolerance;
		slot.NotErrrorChecked = noterrorchecked;
	}

	private void PredictionDumpColor(bool networked, bool errorchecked, bool differs, bool withintolerance, out int r, out int g, out int b, out int a){
		r = 255;
		g = 255;
		b = 255;
		a = 255;

		if (networked) {
			if (errorchecked) {
				r = 180;
				g = 180;
				b = 225;
			}
			else {
				r = 150;
				g = 180;
				b = 150;
			}
		}

		if (differs) {
			if (withintolerance) {
				r = 255;
				g = 255;
				b = 0;
				a = 255;
			}
			else {
				if (!networked) {
					r = 180;
					g = 180;
					b = 100;
					a = 255;
				}
				else {
					r = 255;
					g = 0;
					b = 0;
					a = 255;
				}
			}
		}
	}

	public override void Paint() {
		C_BaseEntity? ent = DumpEntityHandle.Get();
		if (ent == null) {
			Clear();
			return;
		}

		// Now output the strings
		Span<int> x = stackalloc int[5];
		x[0] = 20;
		int columnwidth = 375;
		int numcols = ScreenWidth() / columnwidth;
		int i;

		numcols = Math.Clamp(numcols, 1, 5);

		for (i = 0; i < numcols; i++) {
			if (i == 0) {
				x[i] = 20;
			}
			else {
				x[i] = x[i - 1] + columnwidth - 20;
			}
		}

		int c = DumpEntityInfo.Count;
		int fonttall = surface.GetFontTall(FontSmall) - 3;
		int fonttallMedium = surface.GetFontTall(FontMedium);
		int fonttallBig = surface.GetFontTall(FontBig);

		Span<char> currentclass = stackalloc char[128];
		currentclass[0] = '\0';

		int starty = 60;
		int y = starty;

		int col = 0;

		int r = 255;
		int g = 255;
		int b = 255;
		int a = 255;

		Span<char> classextra = stackalloc char[32];
		classextra[0] = '\0';
		Span<char> classprefix = stackalloc char[32];
		strcpy(classprefix, "class ");
		ReadOnlySpan<char> classname = ent.GetClassname();
		if (classname.IsStringEmpty) {
			classname = ent.GetType().Name;
			strcpy(classextra, " (classmap missing)");
			classprefix[0] = '\0';
		}

		Span<char> sz = stackalloc char[1024];

		surface.DrawSetTextFont(FontBig);
		surface.DrawSetTextColor(new Color(255, 255, 255, 255));
		surface.DrawSetTextPos(x[col] - 10, y - fonttallBig - 2);
		surface.DrawPrintText(sprintf(sz, "entity # %i: %s%s%s").I(ent.EntIndex()).S(classprefix.SliceNullTerminatedString()).S(classname.SliceNullTerminatedString()).S(classextra.SliceNullTerminatedString()).ToSpan());

		Span<DumpInfo> m_DumpEntityInfo = DumpEntityInfo.AsSpan();
		for (i = 0; i < c; i++) {
			ref DumpInfo slot = ref m_DumpEntityInfo[i];

			if (stricmp(slot.ClassName, currentclass) != 0) {
				y += 2;

				surface.DrawSetTextFont(FontMedium);
				surface.DrawSetTextColor(new Color(0, 255, 100, 255));
				surface.DrawSetTextPos(x[col] - 10, y);
				surface.DrawPrintText(sprintf(sz, "%s").S(slot.ClassName).ToSpan());

				y += fonttallMedium - 1;
				strcpy(currentclass, slot.ClassName);
			}


			PredictionDumpColor(slot.Networked, !slot.NotErrrorChecked, slot.Differs, slot.WithinTolerance, out r, out g, out b, out a);

			surface.DrawSetTextFont(FontSmall);
			surface.DrawSetTextColor(new Color(r, g, b, a));
			surface.DrawSetTextPos(x[col], y);
			surface.DrawPrintText(sprintf(sz, "%s").S(slot.FieldString).ToSpan());

			y += fonttall;

			if (y >= ScreenHeight() - fonttall - 60) {
				y = starty;
				col++;
				if (col >= numcols)
					break;
			}
		}

		surface.DrawSetTextFont(FontSmall);


		// Figure how far over the legend needs to be.
		ReadOnlySpan<char> pFirstAndLongestString = "Not networked, no differences";
		surface.GetTextSize(FontSmall, pFirstAndLongestString, out int textSizeWide, out int textSizeTall);

		// Draw a legend now
		int xpos = ScreenWidth() - textSizeWide - 5;
		y = ScreenHeight() - 7 * fonttall - 80;

		// Not networked, no differences
		PredictionDumpColor(false, false, false, false, out r, out g, out b, out a);


		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText(pFirstAndLongestString);

		y += fonttall;

		// Networked, no error check
		PredictionDumpColor(true, false, false, false, out r, out g, out b, out a);

		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText("Networked, not checked");

		y += fonttall;

		// Networked, with error check
		PredictionDumpColor(true, true, false, false, out r, out g, out b, out a);

		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText("Networked, error checked");

		y += fonttall;

		// Differs, but within tolerance
		PredictionDumpColor(true, true, true, true, out r, out g, out b, out a);

		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText("Differs, but within tolerance");

		y += fonttall;

		// Differs, not within tolerance, but not networked
		PredictionDumpColor(false, true, true, false, out r, out g, out b, out a);

		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText("Differs, but not networked");

		y += fonttall;

		// Differs, networked, not within tolerance
		PredictionDumpColor(true, true, true, false, out r, out g, out b, out a);

		surface.DrawSetTextColor(new Color(r, g, b, a));
		surface.DrawSetTextPos(xpos, y);
		surface.DrawPrintText("Differs, networked");

		y += fonttall;
	}

	struct DumpInfo
	{
		public InlineArrayDumpClassNameSize<char> ClassName;
		public bool Networked;
		public InlineArrayDumpStringSize<char> FieldString;
		public bool Differs;
		public bool WithinTolerance;
		public bool NotErrrorChecked;
	}

	readonly List<DumpInfo> DumpEntityInfo = [];
	EHANDLE DumpEntityHandle;

	[PanelAnimationVar("ItemFont", "DefaultVerySmall")] public IFont? FontSmall;
	[PanelAnimationVar("LabelFont", "DefaultSmall")] public IFont? FontMedium;
	[PanelAnimationVar("TitleFont", "Trebuchet24")] public IFont? FontBig;
}
