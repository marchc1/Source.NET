using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.GUI;
using Source.Common.Networking;
using Source.Engine.Client;

namespace Source.Engine;


#if !SWDS
public static class ConsoleCVars
{
	static readonly ConVar con_trace = new("0", FCvar.MaterialSystemThread, "Print console text to low level printout.");
	static readonly ConVar con_notifytime = new("8", FCvar.MaterialSystemThread, "How long to display recent console text to the upper part of the game window");
	static readonly ConVar con_times = new("8", FCvar.MaterialSystemThread, "Number of console lines to overlay for debugging.");
	static readonly ConVar con_drawnotify = new("1", 0, "Disables drawing of notification area (for taking screenshots).");
	static readonly ConVar con_enable = new(
#if GMOD_DLL
		"1"
#else
		"0"
#endif
		, FCvar.Archive, "Allows the console to be activated.");
	static readonly ConVar con_filter_enable = new("0", FCvar.MaterialSystemThread, "Filters console output based on the setting of con_filter_text. 1 filters completely, 2 displays filtered text brighter than other text.");
	static readonly ConVar con_filter_text = new("", FCvar.MaterialSystemThread, "Text with which to filter console spew. Set con_filter_enable 1 or 2 to activate.");
	static readonly ConVar con_filter_text_out = new("", FCvar.MaterialSystemThread, "Text with which to filter OUT of console spew. Set con_filter_enable 1 or 2 to activate.");
}

public class ConPanel : BasePanel
{
	public Host Host = Singleton<Host>();
	public Con Con = Singleton<Con>();
	public ClientState cl => Host.cl;
	public VideoMode_Common videomode = (VideoMode_Common)Singleton<IVideoMode>();

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
	}

	public override void Paint() {
		// Client DLL shoulddrawdropdownconsole?

		DrawDebugAreas();
		DrawNotify();
	}

	public override unsafe void PaintBackground() {
		if (!Con.IsVisible())
			return;

		Span<char> text;
		int wide = GetWide();
		Span<char> ver = stackalloc char[100];
		text = new PrintF(ver, "Source.NET Engine %i (build %d)").I(Protocol.VERSION).D(Sys.BuildNumber());

		Surface.DrawSetTextColor(new Color(255, 255, 255, 255));
		int x = wide - DrawTextLen(Font, text) - 2;
		DrawText(Font, x, 0, text);

		if (cl.IsActive()) {
			
			if (cl.NetChannel!.IsLoopback())
				text = new PrintF(ver, "Map '%s'").S(cl.LevelBaseName).ToSpan();
			else 
				text = new PrintF(ver, "Server '%s' Map '%s'").S(cl.NetChannel!.RemoteAddress!.ToString()).S(cl.LevelBaseName).ToSpan();
			
			int tall = Surface.GetFontTall(Font);

			x = wide - DrawTextLen(Font, text) - 2;
			DrawText(Font, x, tall + 1, text);
		}
	}

	readonly List<NotifyText> TextToDraw = [];
	public virtual void DrawNotify() {
		int x = 8;
		int y = 5;

		if (FontFixed == null)
			return;

		// notify area only draws in developer mode
		if (!Host.developer.GetBool())
			return;

		Surface.DrawSetTextFont(FontFixed);

		int fontTall = Surface.GetFontTall(FontFixed) + 1;

		TextToDraw.Clear();
		lock (NotifyTexts) {
			TextToDraw.EnsureCountDefault(NotifyTexts.Count);
			NotifyTexts.CopyTo(TextToDraw.AsSpan());
		}

		Span<NotifyText> textToDraw = TextToDraw.AsSpan();
		int c = textToDraw.Length;
		for (int i = 0; i < c; i++) {
			ref NotifyText notify = ref textToDraw[i];
			TimeUnit_t timeleft = notify.LifeRemaining;
			Color clr = notify.Color;

			if (timeleft < .5f) {
				TimeUnit_t f = Math.Clamp(timeleft, 0.0, .5) / .5;

				clr[3] = (byte)(int)(float)(f * 255.0);

				if (i == 0 && f < 0.2f) 
					y -= (int)(float)(fontTall * (1.0 - f / 0.2));
			}
			else {
				clr[3] = 255;
			}

			DrawColoredText(FontFixed, x, y, clr[0], clr[1], clr[2], clr[3], notify.Text);

			y += fontTall;
		}
	}

	public virtual void DrawDebugAreas() {

	}

	public virtual int ProcessNotifyLines(ref int left, ref int top, ref int right, ref int bottom, bool draw) {
		int count = 0;
		int y = 20;

		for (int i = 0; i < MAX_DBG_NOTIFY; i++) {
			if (Host.RealTime < da_notify[i].Expire || da_notify[i].Expire == -1) {
				if (da_notify[i].Expire == -1 && draw) {
					da_notify[i].Expire = Host.RealTime - 1;
				}

				int len;
				int x;

				IFont? font = da_notify[i].FixedWidthFont ? FontFixed : Font;

				int fontTall = Surface.GetFontTall(FontFixed) + 1;

				len = DrawTextLen(font, da_notify[i].Notify);
				x = videomode.GetModeStereoWidth() - 10 - len;

				if (y + fontTall > videomode.GetModeStereoHeight() - 20)
					return count;

				count++;
				y = 20 + 10 * i;

				if (draw) {
					DrawColoredText(font, x, y,
						(byte)(int)(da_notify[i].Color[0] * 255),
						(byte)(int)(da_notify[i].Color[1] * 255),
						(byte)(int)(da_notify[i].Color[2] * 255),
						255,		 
						da_notify[i].Notify);
				}

				if (da_notify[i].Notify[0] != '\0') {
					left = Math.Min(left, x);
					top = Math.Min(top, y);
					right = Math.Max(right, x + len);
					bottom = Math.Max(bottom, y + fontTall);
				}

				y += fontTall;
			}
		}

		return count;
	}

	IFont? Font;
	IFont? FontFixed;

	struct NotifyText
	{
		public Color Color;
		public TimeUnit_t LifeRemaining;
		public InlineArray256<char> Text;
	}
	readonly List<NotifyText> NotifyTexts = [];

	InlineArray3<float> DefaultColor;
	struct da_notify_t
	{
		public InlineArray256<char> Notify;
		public TimeUnit_t Expire;
		public InlineArray3<float> Color;
		public bool FixedWidthFont;
	}

	const int MAX_DBG_NOTIFY = 128;
	InlineArray128<da_notify_t> da_notify;
	bool drawDebugAreas;

	public override bool ShouldDraw() {
		bool visible = false;

		if (drawDebugAreas) {
			visible = true;
		}

		if (!Con.IsVisible()) {
			int i;
			int c = NotifyTexts.Count;
			Span<NotifyText> notifyTexts = NotifyTexts.AsSpan();
			for (i = c - 1; i >= 0; i--) {
				ref NotifyText notify = ref notifyTexts[i];

				notify.LifeRemaining -= Host.FrameTime;

				if (notify.LifeRemaining <= 0.0f) {
					NotifyTexts.RemoveAt(i);
					notifyTexts = NotifyTexts.AsSpan();
					continue;
				}

				visible = true;
			}
		}
		else {
			visible = true;
		}

		return visible;
	}

	public void Con_NPrintf(int idx, ReadOnlySpan<char> msg) {

	}

	public void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> msg) {

	}

	public void AddToNotify(in Color clr, ReadOnlySpan<char> msg) {

	}

	public void ClearNotify() {

	}


	public override void OnTick() {
		throw new NotImplementedException();
	}
}
#endif


public class Con(ICvar cvar, IEngineVGuiInternal EngineVGui, IVGuiInput Input, IBaseClientDLL ClientDLL)
{
	static ConVar con_enable = new("1", FCvar.Archive, "Allows the console to be activated.");

	[ConCommand]
	void toggleconsole() => ToggleConsole();

	public void ShowConsole() {
		if (Input.GetAppModalSurface() != null)
			return;

		if (!ClientDLL.ShouldAllowConsole())
			return;

		if (con_enable.GetBool()) {
			EngineVGui.ShowConsole();
			Singleton<Scr>().EndLoadingPlaque();
		}
	}

	public void HideConsole() {
		if (EngineVGui.IsConsoleVisible())
			EngineVGui.HideConsole();
	}

	public void ToggleConsole() {
		if (EngineVGui.IsConsoleVisible()) {
			HideConsole();
			EngineVGui.HideGameUI();
		}
		else
			ShowConsole();
	}

	public void Init() { }
	public void Shutdown() { }
	public void Execute() { }

	// TODO: ConPanel

	internal void ClearNotify() {

	}

	public void Clear() {
		Singleton<IEngineVGui>().ClearConsole();
		ClearNotify();
	}

	[ConCommand] void clear() => Clear();

	public void ColorPrintf(in Color clr, ReadOnlySpan<char> fmt) {
		cvar.ConsoleColorPrintf(in clr, fmt);
	}

	public bool IsVisible() => EngineVGui.IsConsoleVisible();
}
