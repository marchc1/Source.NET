using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.Common.Networking;
using Source.GUI.Controls;

using System.Drawing;
using System.Runtime.CompilerServices;

using Color = Source.Color;

namespace Game.Client;

public static class NetgraphCallbacks
{
	public static void NetgraphFontChangeCallback(IConVar var, string pOldValue, float flOldValue) {
		if (NetGraphPanel.g_NetGraphPanel != null) {
			NetGraphPanel.g_NetGraphPanel.OnFontChanged();
		}
	}
}

/// <summary>
/// Displays the net graph
/// </summary>
public class NetGraphPanel : Panel
{
	public static NetGraphPanel? g_NetGraphPanel = null;

	private struct PacketLatency
	{
		public int Latency;
		public int Choked;
	}

	private struct NetBandWidthGraph
	{
		[InlineArray((int)NetChannelGroup.Total + 1)] public struct MSGBYTES { public ushort first; }
		public MSGBYTES MsgBytes;
		public int SampleY;
		public int SampleHeight;
	}

	private struct CmdInfo
	{
		public float CmdLerp;
		public int Size;
		public bool Sent;
	}

	private struct NetColor
	{
		public Color Color;
		public int this[int index] {
			get => Color[index];
			set => Color[index] = (byte)value;
		}
	}

	public struct LineSegment
	{
		public int X1, Y1, X2, Y2;
		public Color Color;
		public Color Color2;
	}

	public static readonly ConVar net_scale = new("net_scale", "5", FCvar.Archive);
	public static readonly ConVar net_graphpos = new("net_graphpos", "1", FCvar.Archive);
	public static readonly ConVar net_graphsolid = new("net_graphsolid", "1", FCvar.Archive);
	public static readonly ConVar net_graphtext = new("net_graphtext", "1", FCvar.Archive, "Draw text fields");
	public static readonly ConVar net_graphmsecs = new("net_graphmsecs", "400", FCvar.Archive, "The latency graph represents this many milliseconds.");
	public static readonly ConVar net_graphshowlatency = new("net_graphshowlatency", "1", FCvar.Archive, "Draw the ping/packet loss graph.");
	public static readonly ConVar net_graphshowinterp = new("net_graphshowinterp", "1", FCvar.Archive, "Draw the interpolation graph.");
	public static readonly ConVar net_graph = new("net_graph", "0", 0, "Draw the network usage graph, = 2 draws data on payload, = 3 draws payload legend.");
	public static readonly ConVar net_graphheight = new("net_graphheight", "64", FCvar.Archive, "Height of netgraph panel");
	public static readonly ConVar net_graphproportionalfont = new("net_graphproportionalfont", "1", FCvar.Archive, "Determines whether netgraph font is proportional or not");

	public const int TIMINGS = 1024; // Number of values to track (must be power of 2) b/c of masking
	public const float FRAMERATE_AVG_FRAC = 0.9f;
	public const float PACKETLOSS_AVG_FRAC = 0.5f;
	public const float PACKETCHOKE_AVG_FRAC = 0.5f;
	public const int NUM_LATENCY_SAMPLES = 8;
	public const byte GRAPH_RED = (byte)(0.9 * 255);
	public const byte GRAPH_GREEN = (byte)(0.9 * 255);
	public const byte GRAPH_BLUE = (byte)(0.7 * 255);
	public const int LERP_HEIGHT = 24;
	public const int COLOR_DROPPED = 0;
	public const int COLOR_INVALID = 1;
	public const int COLOR_SKIPPED = 2;
	public const int COLOR_CHOKED = 3;
	public const int COLOR_NORMAL = 4;

	InlineArray24<Color> Colors;
	Color SendColor;
	Color HoldColor;
	Color ExtrapBaseColor;

	readonly PacketLatency[] m_PacketLatency = new PacketLatency[TIMINGS];
	readonly CmdInfo[] m_Cmdinfo = new CmdInfo[TIMINGS];
	readonly NetBandWidthGraph[] m_Graph = new NetBandWidthGraph[TIMINGS];

	TimeUnit_t Framerate;
	TimeUnit_t AvgLatency;
	TimeUnit_t AvgPacketLoss;
	TimeUnit_t AvgPacketChoke;
	int IncomingSequence;
	int OutgoingSequence;
	int UpdateWindowSize;
	TimeUnit_t IncomingData;
	TimeUnit_t OutgoingData;
	TimeUnit_t AvgPacketIn;
	TimeUnit_t AvgPacketOut;

	InlineArrayMaxFlows<int> StreamRecv;
	InlineArrayMaxFlows<int> StreamTotal;
	readonly NetColor[] NetColors = new NetColor[5];

	IFont? FontProportional;
	IFont? Font;
	IFont? FontSmall;
	ConVarRef cl_updaterate;
	ConVarRef cl_cmdrate;

	readonly List<LineSegment> Rects = [new()];

	int EstimatedWidth = 1;
	int NetGraphHeight = 100;
	TimeUnit_t ServerFramerate;
	TimeUnit_t ServerFramerateStdDeviation;

	public NetGraphPanel(IPanel parent) : base((Panel?)parent, "NetGraphPanel") {
		int w, h;
		Surface.GetScreenSize(out w, out h);

		SetParent(parent);
		SetSize(w, h);
		SetPos(0, 0);
		SetVisible(false);
		SetCursor(0);

		Font = null;
		FontProportional = null;
		FontSmall = null;
		EstimatedWidth = 1;
		NetGraphHeight = 100;

		SetPaintBackgroundEnabled(false);

		InitColors();

		cl_updaterate.Init("cl_updaterate");
		cl_cmdrate.Init("cl_cmdrate");
		Assert(!cl_updaterate.IsEmpty && !cl_cmdrate.IsEmpty);

		SendColor = new(255, 255, 0, 255);
		HoldColor = new(0, 0, 0, 255);
		ExtrapBaseColor = new(255, 255, 255, 255);

		for (int i = 0; i < 24; i++) {
			Colors[i][3] = 255;
		}

		Framerate = 0.0f;
		AvgLatency = 0.0f;
		AvgPacketLoss = 0.0f;
		AvgPacketChoke = 0.0f;
		IncomingSequence = 0;
		OutgoingSequence = 0;
		UpdateWindowSize = 0;
		IncomingData = 0;
		OutgoingData = 0;
		AvgPacketIn = 0.0f;
		AvgPacketOut = 0.0f;
		ServerFramerate = 0;
		ServerFramerateStdDeviation = 0;

		NetColors[COLOR_DROPPED][0] = 255;
		NetColors[COLOR_DROPPED][1] = 0;
		NetColors[COLOR_DROPPED][2] = 0;
		NetColors[COLOR_DROPPED][3] = 255;
		NetColors[COLOR_INVALID][0] = 0;
		NetColors[COLOR_INVALID][1] = 0;
		NetColors[COLOR_INVALID][2] = 255;
		NetColors[COLOR_INVALID][3] = 255;
		NetColors[COLOR_SKIPPED][0] = 240;
		NetColors[COLOR_SKIPPED][1] = 127;
		NetColors[COLOR_SKIPPED][2] = 63;
		NetColors[COLOR_SKIPPED][3] = 255;
		NetColors[COLOR_CHOKED][0] = 225;
		NetColors[COLOR_CHOKED][1] = 225;
		NetColors[COLOR_CHOKED][2] = 0;
		NetColors[COLOR_CHOKED][3] = 255;
		NetColors[COLOR_NORMAL][0] = 63;
		NetColors[COLOR_NORMAL][1] = 255;
		NetColors[COLOR_NORMAL][2] = 63;
		NetColors[COLOR_NORMAL][3] = 232;

		VGui.AddTickSignal(this, 500);

		g_NetGraphPanel = this;
	}
	public override void OnDelete() {
		g_NetGraphPanel = null;
		base.OnDelete();
	}
	public void OnFontChanged() {
		ReadOnlySpan<char> str = "fps:  435  ping: 533 ms lerp 112.3 ms   0/0";

		if (FontProportional == null)
			EstimatedWidth = 0;
		else
			Surface.GetTextSize(FontProportional, str, out EstimatedWidth, out int textTall);

		int w, h;
		Surface.GetScreenSize(out w, out h);
		SetSize(w, h);
		SetPos(0, 0);

		ComputeNetgraphHeight();
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("DefaultFixedOutline", false);
		FontProportional = scheme.GetFont("DefaultFixedOutline", true);
		FontSmall = scheme.GetFont("DefaultVerySmall", false);

		OnFontChanged();
	}

	void ComputeNetgraphHeight() {
		NetGraphHeight = net_graphheight.GetInt();

		var fnt = GetNetgraphFont();
		int tall = Surface.GetFontTall(fnt);

		int lines = 3;
		if (net_graph.GetInt() > 3)
			lines = 5;
		else if (net_graph.GetInt() > 2)
			lines = 4;

		NetGraphHeight = Math.Max(lines * tall, NetGraphHeight);
	}

	void GetColorValues(int color, out Color cv) {
		var pc = NetColors[color];
		cv = pc.Color;
	}

	void ColorForHeight(ref PacketLatency packet, out Color color, out int ping) {
		int h = packet.Latency;
		ping = 0;
		switch (h) {
			case 9999:
				GetColorValues(COLOR_DROPPED, out color);
				break;
			case 9998:
				GetColorValues(COLOR_INVALID, out color);
				break;
			case 9997:
				GetColorValues(COLOR_SKIPPED, out color);
				break;
			default:
				ping = 1;
				if (packet.Choked != 0) {
					GetColorValues(COLOR_CHOKED, out color);
				}
				else {
					GetColorValues(COLOR_NORMAL, out color);
				}
				break;
		}
	}

	void InitColors() {
		Span<Color> mincolor = stackalloc Color[2];
		Span<Color> maxcolor = stackalloc Color[2];
		Span<InlineArray3<float>> dc = stackalloc InlineArray3<float>[2];
		int hfrac;
		float f;

		mincolor[0][0] = 63;
		mincolor[0][1] = 0;
		mincolor[0][2] = 100;

		maxcolor[0][0] = 0;
		maxcolor[0][1] = 63;
		maxcolor[0][2] = 255;

		mincolor[1][0] = 255;
		mincolor[1][1] = 127;
		mincolor[1][2] = 0;

		maxcolor[1][0] = 250;
		maxcolor[1][1] = 0;
		maxcolor[1][2] = 0;

		for (int i = 0; i < 3; i++) {
			dc[0][i] = (float)(maxcolor[0][i] - mincolor[0][i]);
			dc[1][i] = (float)(maxcolor[1][i] - mincolor[1][i]);
		}

		hfrac = LERP_HEIGHT / 3;

		for (int i = 0; i < LERP_HEIGHT; i++) {
			if (i < hfrac) {
				f = (float)i / (float)hfrac;
				for (int j = 0; j < 3; j++) {
					Colors[i][j] = (byte)(mincolor[0][j] + f * dc[0][j]);
				}
			}
			else {
				f = (float)(i - hfrac) / (float)(LERP_HEIGHT - hfrac);
				for (int j = 0; j < 3; j++) {
					Colors[i][j] = (byte)(mincolor[1][j] + f * dc[1][j]);
				}
			}
		}
	}

	void DrawTimes(ref Rectangle vrect, CmdInfo[] cmdinfo, int x, int w, int graphtype) {
		if (!net_graphshowinterp.GetBool() || graphtype <= 1)
			return;

		int i, j;
		int extrap_point;
		int a, h;
		Rectangle rcFill = new();

		ResetLineSegments();

		extrap_point = LERP_HEIGHT / 3;

		for (a = 0; a < w; a++) {
			i = (OutgoingSequence - a) & (TIMINGS - 1);
			h = Math.Min((int)((cmdinfo[i].CmdLerp / 3.0) * LERP_HEIGHT), LERP_HEIGHT);
			if (h < 0) {
				h = LERP_HEIGHT;
			}

			rcFill.X = x + w - a - 1;
			rcFill.Width = 1;
			rcFill.Height = 1;

			rcFill.Y = vrect.Y + vrect.Height - 4;

			if (h >= extrap_point) {
				int start = 0;

				h -= extrap_point;
				rcFill.Y -= extrap_point;

				if (!net_graphsolid.GetBool()) {
					rcFill.Y -= (h - 1);
					start = (h - 1);
				}

				for (j = start; j < h; j++) {
					int index = j + extrap_point;
					DrawLine(in rcFill, Colors[index] with { A = 255 });
					rcFill.Y--;
				}
			}
			else {
				int oldh;
				oldh = h;
				rcFill.Y -= h;
				h = extrap_point - h;

				if (!net_graphsolid.GetBool()) {
					h = 1;
				}

				for (j = 0; j < h; j++) {
					int index = j + oldh;
					DrawLine(in rcFill, Colors[index] with { A = 255 });
					rcFill.Y--;
				}
			}

			rcFill.Y = vrect.Y + vrect.Height - 4 - extrap_point;

			DrawLine(in rcFill, ExtrapBaseColor with { A = 255 });

			rcFill.Y = vrect.Y + vrect.Height - 3;

			if (cmdinfo[i].Sent)
				DrawLine(in rcFill, SendColor with { A = 255 });
			else
				DrawLine(in rcFill, HoldColor with { A = 200 });
		}

		DrawLineSegments();
	}

	void GetFrameData(INetChannelInfo netchannel, out int biggest_message, out float avg_message, out float f95thpercentile) {
		biggest_message = 0;
		avg_message = 0.0f;
		f95thpercentile = 0.0f;

		int msg_count = 0;

		IncomingSequence = netchannel.GetSequenceNumber(NetFlow.FLOW_INCOMING);
		OutgoingSequence = netchannel.GetSequenceNumber(NetFlow.FLOW_OUTGOING);
		UpdateWindowSize = netchannel.GetBufferSize();
		AvgPacketLoss = netchannel.GetAverageLoss(NetFlow.FLOW_INCOMING);
		AvgPacketChoke = netchannel.GetAverageChoke(NetFlow.FLOW_INCOMING);
		AvgLatency = netchannel.GetAverageLatency(NetFlow.FLOW_OUTGOING);
		IncomingData = netchannel.GetAverageData(NetFlow.FLOW_INCOMING) / 1024.0f;
		OutgoingData = netchannel.GetAverageData(NetFlow.FLOW_OUTGOING) / 1024.0f;
		AvgPacketIn = netchannel.GetAveragePackets(NetFlow.FLOW_INCOMING);
		AvgPacketOut = netchannel.GetAveragePackets(NetFlow.FLOW_OUTGOING);

		for (int i = 0; i < NetFlow.MAX_FLOWS; i++) 
			netchannel.GetStreamProgress(i, out StreamRecv[i], out StreamTotal[i]);

		float flAdjust = 0.0f;

		if (!cl_updaterate.IsEmpty && cl_updaterate.GetFloat() > 0.001f) {
			flAdjust = -0.5f / cl_updaterate.GetFloat();
			AvgLatency += flAdjust;
		}

		// Can't be below zero
		AvgLatency = (float)Math.Max(0.0, AvgLatency);

		flAdjust *= 1000.0f;

		// Fill in frame data
		for (int seqnr = IncomingSequence - UpdateWindowSize + 1; seqnr <= IncomingSequence; seqnr++) {
			float frame_received_time = (float)netchannel.GetPacketTime(NetFlow.FLOW_INCOMING, seqnr);

			ref var nbwg = ref m_Graph[seqnr & (TIMINGS - 1)];
			ref var lat = ref m_PacketLatency[seqnr & (TIMINGS - 1)];

			netchannel.GetPacketResponseLatency(NetFlow.FLOW_INCOMING, seqnr, out lat.Latency, out lat.Choked);

			if (lat.Latency < 9995) {
				lat.Latency += (int)flAdjust;
				lat.Latency = Math.Max(lat.Latency, 0);
			}

			for (int i = 0; i <= (int)NetChannelGroup.Total; i++) {
				nbwg.MsgBytes[i] = (ushort)netchannel.GetPacketBytes(NetFlow.FLOW_INCOMING, seqnr, (NetChannelGroup)i);
			}

			if (nbwg.MsgBytes[(int)NetChannelGroup.Total] > biggest_message) {
				biggest_message = nbwg.MsgBytes[(int)NetChannelGroup.Total];
			}

			avg_message += (float)(nbwg.MsgBytes[(int)NetChannelGroup.Total]);
			msg_count++;
		}

		if (biggest_message > 1000) {
			biggest_message = 1000;
		}

		if (msg_count >= 1) {
			avg_message /= msg_count;

			int deviationsquared = 0;

			// Compute std deviation
			for (int seqnr = IncomingSequence - UpdateWindowSize + 1; seqnr <= IncomingSequence; seqnr++) {
				int bytes = m_Graph[seqnr & (TIMINGS - 1)].MsgBytes[(int)NetChannelGroup.Total] - (int)avg_message;
				deviationsquared += (bytes * bytes);
			}

			float var = (float)(deviationsquared) / (float)(msg_count - 1);
			float stddev = (float)Math.Sqrt(var);

			f95thpercentile = avg_message + 2.0f * stddev;
		}
	}

	void GetCommandInfo(INetChannelInfo netchannel, CmdInfo[] cmdinfo) {
		for (int seqnr = OutgoingSequence - UpdateWindowSize + 1; seqnr <= OutgoingSequence; seqnr++) {
			ref var ci = ref cmdinfo[seqnr & (TIMINGS - 1)];

			ci.CmdLerp = (float)netchannel.GetCommandInterpolationAmount(NetFlow.FLOW_OUTGOING, seqnr);
			ci.Sent = netchannel.IsValidPacket(NetFlow.FLOW_OUTGOING, seqnr);
			ci.Size = netchannel.GetPacketBytes(NetFlow.FLOW_OUTGOING, seqnr, NetChannelGroup.Total);
		}
	}

	void DrawTextFields(int graphvalue, int x, int y, int w, NetBandWidthGraph[] graph, CmdInfo[] cmdinfo) {
		if (!net_graphtext.GetBool())
			return;

		int lastout = 0;
		string sz;
		int outBytes;

		var font = GetNetgraphFont();

		Framerate = FRAMERATE_AVG_FRAC * Framerate + (1.0f - FRAMERATE_AVG_FRAC) * (float)gpGlobals.FrameTime;

		// Print it out
		y -= NetGraphHeight;

		int saveY = y;

		if (Framerate <= 0.0f)
			Framerate = 1.0f;

		if (engine.IsPlayingDemo())
			AvgLatency = 0.0f;

		int textTall = Surface.GetFontTall(font);

		sz = $"fps:{(int)(1.0f / Framerate),4}   ping: {(int)(AvgLatency * 1000.0f)} ms";

		Surface.DrawColoredText(font, x, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

		// Draw update rate
		DrawUpdateRate(x + w, y);

		y += textTall;

		outBytes = cmdinfo[(OutgoingSequence - 1) & (TIMINGS - 1)].Size;
		if (outBytes == 0) {
			outBytes = lastout;
		}
		else {
			lastout = outBytes;
		}

		int totalsize = graph[IncomingSequence & (TIMINGS - 1)].MsgBytes[(int)NetChannelGroup.Total];

		sz = $"in :{totalsize,4}   {IncomingData:F2} k/s ";

		Surface.GetTextSize(font, sz, out int textWidth, out _);

		Surface.DrawColoredText(font, x, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

		float flInterp = (float)gpGlobals.InterpolationAmount;
		sz = $"lerp: {flInterp * 1000.0f,5:F1} ms";

		Span<int> interpcolor = [GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE];
		if (flInterp > 0.001f) {
			// Server framerate is lower than interp can possibly deal with
			if (ServerFramerate < (1.0f / flInterp)) {
				interpcolor[0] = 255;
				interpcolor[1] = 255;
				interpcolor[2] = 31;
			}
			// flInterp is below recommended setting!!!
			else if (!cl_updaterate.IsEmpty && flInterp < (2.0f / cl_updaterate.GetFloat())) {
				interpcolor[0] = 255;
				interpcolor[1] = 125;
				interpcolor[2] = 31;
			}
		}

		Surface.DrawColoredText(font, x + textWidth, y, (byte)interpcolor[0], (byte)interpcolor[1], (byte)interpcolor[2], 255, sz);

		sz = $"{AvgPacketIn:F1}/s";
		Surface.GetTextSize(font, sz, out textWidth, out _);

		Surface.DrawColoredText(font, x + w - textWidth - 1, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

		y += textTall;

		sz = $"out:{outBytes,4}   {OutgoingData:F2} k/s";

		Surface.DrawColoredText(font, x, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

		sz = $"{AvgPacketOut:F1}/s";
		Surface.GetTextSize(font, sz, out textWidth, out _);

		Surface.DrawColoredText(font, x + w - textWidth - 1, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

		y += textTall;

		DrawCmdRate(x + w, y);

		if (graphvalue > 2) {
			sz = $"loss:{(int)(AvgPacketLoss * 100.0f),3}    choke: {(int)(AvgPacketChoke * 100.0f),2} ";

			Surface.GetTextSize(font, sz, out textWidth, out _);

			Surface.DrawColoredText(font, x, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);

			y += textTall;

			if (graphvalue > 3) {
				sz = $"sv  : {ServerFramerate,5:F1}   var: {ServerFramerateStdDeviation * 1000.0f,4:F2} msec";

				Span<int> servercolor = [GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE];

				if (ServerFramerate < 10.0f) {
					servercolor[0] = 255;
					servercolor[1] = 31;
					servercolor[2] = 31;
				}
				else if (ServerFramerate < 20.0f) {
					servercolor[0] = 255;
					servercolor[1] = 255;
					servercolor[2] = 0;
				}

				Surface.DrawColoredText(font, x, y, (byte)servercolor[0], (byte)servercolor[1], (byte)servercolor[2], 255, sz);

				y += textTall;
			}
		}

		// Draw legend
		if (graphvalue >= 3) {
			textTall = Surface.GetFontTall(FontSmall);

			y = saveY - textTall - 5;
			Surface.GetTextSize(FontSmall, "otherplayersWWW", out int cw, out int ch);
			if (x - cw < 0) {
				x += w + 5;
			}
			else {
				x -= cw;
			}

			Surface.DrawColoredText(FontSmall, x, y, 0, 0, 255, 255, "localplayer");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 0, 255, 0, 255, "otherplayers");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 255, 0, 0, 255, "entities");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 255, 255, 0, 255, "sounds");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 0, 255, 255, 255, "events");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 128, 128, 0, 255, "usermessages");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 0, 128, 128, 255, "entmessages");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 128, 0, 0, 255, "stringcmds");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 0, 128, 0, 255, "stringtables");
			y -= textTall;
			Surface.DrawColoredText(FontSmall, x, y, 0, 0, 128, 255, "voice");
			y -= textTall;
		}
	}


	int GraphValue() {
		int graphtype = net_graph.GetInt();

		if (graphtype == 0)
			return 0;

		return graphtype;
	}


	void GraphGetXY(in Rectangle rect, int width, out int x, out int y) {
		x = rect.X + 5;

		switch (net_graphpos.GetInt()) {
			case 0:
				break;
			case 1:
				x = rect.X + rect.Width - 5 - width;
				break;
			case 2:
				x = rect.X + (rect.Width - 10 - width) / 2;
				break;
			default:
				x = rect.X + Math.Clamp(net_graphpos.GetInt(), 5, rect.Width - width - 5);
				break;
		}

		y = rect.Y + rect.Height - LERP_HEIGHT - 5;
	}

	void DrawStreamProgress(int x, int y, int width) {
		Rectangle rcLine = new() {
			X = x,
			Height = 1
		};

		Color color = new(0, 200, 0);

		if (StreamTotal[0] > 0) {
			rcLine.Y = y - NetGraphHeight + 15 + 14;
			rcLine.Width = (StreamRecv[0] * width) / StreamTotal[0];
			DrawLine(in rcLine, color with { A = 255 });
		}

		if (StreamTotal[1] > 0) // FLOW_OUTGOING
		{
			rcLine.Y = y - NetGraphHeight + 2 * 15 + 14;
			rcLine.Width = (StreamRecv[1] * width) / StreamTotal[1];
			DrawLine(in rcLine, color with { A = 255 });
		}
	}

	void DrawHatches(int x, int y, int maxmsgbytes) {
		int starty;
		int ystep;
		Rectangle rcHatch = new();

		Color colorminor = new(0, 0, 0);
		Color color = new(0, 0, 0);

		ystep = (int)(10.0f / net_scale.GetFloat());
		ystep = Math.Max(ystep, 1);

		rcHatch.Y = y;
		rcHatch.Height = 1;
		rcHatch.X = x;
		rcHatch.Width = 4;

		color[0] = 0;
		color[1] = 200;
		color[2] = 0;

		colorminor[0] = 63;
		colorminor[1] = 63;
		colorminor[2] = 0;

		for (starty = rcHatch.Y; rcHatch.Y > 0 && ((starty - rcHatch.Y) * net_scale.GetFloat() < (maxmsgbytes + 50)); rcHatch.Y -= ystep) {
			if (((int)((starty - rcHatch.Y) * net_scale.GetFloat()) % 50) == 0) {
				DrawLine(in rcHatch, color with { A = 255 });
			}
			else if (ystep > 5) {
				DrawLine(in rcHatch, colorminor with { A = 200 });
			}
		}
	}

	void DrawUpdateRate(int xright, int y) {
		if (cl_updaterate.IsEmpty) return;

		string sz = $"{cl_updaterate.GetInt()}/s";

		Surface.GetTextSize(GetNetgraphFont(), sz, out int textWide, out int textTall);
		Surface.DrawColoredText(GetNetgraphFont(), xright - textWide - 1, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);
	}

	void DrawCmdRate(int xright, int y) {
		if (cl_cmdrate.IsEmpty) return;

		string sz = $"{cl_cmdrate.GetInt()}/s";

		Surface.GetTextSize(GetNetgraphFont(), sz, out int textWide, out int textTall);

		Surface.DrawColoredText(GetNetgraphFont(), xright - textWide - 1, y, GRAPH_RED, GRAPH_GREEN, GRAPH_BLUE, 255, sz);
	}

	bool DrawDataSegment(ref Rectangle rcFill, int bytes, byte r, byte g, byte b, byte alpha = 255) {
		int h;
		Color color = new(0, 0, 0);

		h = (int)(bytes / net_scale.GetFloat());

		color[0] = r;
		color[1] = g;
		color[2] = b;
		color[3] = 255;

		rcFill.Height = h;
		rcFill.Y -= h;

		if (rcFill.Y < 2)
			return false;

		DrawLine(in rcFill, color with { A = alpha });

		return true;
	}

	public override void OnTick() {
		bool bVisible = ShouldDraw();
		if (IsVisible() != bVisible) {
			SetVisible(bVisible);
		}
	}

	bool ShouldDraw() {
		if (GraphValue() != 0)
			return true;

		return false;
	}

	void DrawLargePacketSizes(int x, int w, int graphtype, float warning_threshold) {
		Rectangle rcFill = new();
		int a, i;

		for (a = 0; a < w; a++) {
			i = (IncomingSequence - a) & (TIMINGS - 1);

			rcFill.X = x + w - a - 1;
			rcFill.Width = 1;
			rcFill.Y = m_Graph[i].SampleY;
			rcFill.Height = m_Graph[i].SampleHeight;

			int nTotalBytes = m_Graph[i].MsgBytes[(int)NetChannelGroup.Total];

			if (warning_threshold != 0.0f && nTotalBytes > Math.Max(300, (int)warning_threshold)) {
				string sz = nTotalBytes.ToString();

				Surface.GetTextSize(Font, sz, out int len, out _);

				int textx = rcFill.X - len / 2;
				int texty = Math.Max(0, rcFill.Y - 11);

				Surface.DrawColoredText(Font, textx, texty, 255, 255, 255, 255, sz);
			}
		}
	}

	public override void Paint() {
		int graphtype;
		int x, y;
		int w;
		Rectangle vrect = new();
		int maxmsgbytes = 0;
		float avg_message = 0.0f;
		float warning_threshold = 0.0f;

		if ((graphtype = GraphValue()) == 0)
			return;

		// Since we divide by scale, make sure it's sensible
		if (net_scale.GetFloat() <= 0) {
			net_scale.SetValue(0.1f);
		}

		Surface.GetScreenSize(out int sw, out int sh);

		// Get screen rectangle
		vrect.X = 0;
		vrect.Y = 0;
		vrect.Width = sw;
		vrect.Height = sh;

		w = Math.Min(TIMINGS, EstimatedWidth);
		if (vrect.Width < w + 10) {
			w = vrect.Width - 10;
		}

		var nci = engine.GetNetChannelInfo();

		if (nci != null) {
			// update incoming data
			GetFrameData(nci, out maxmsgbytes, out avg_message, out warning_threshold);

			// update outgoing data
			GetCommandInfo(nci, m_Cmdinfo);

			UpdateEstimatedServerFramerate(nci);
		}

		GraphGetXY(in vrect, w, out x, out y);

		if (graphtype > 1) {
			PaintLineArt(x, y, w, graphtype, maxmsgbytes);

			DrawLargePacketSizes(x, w, graphtype, warning_threshold);
		}

		// Draw client frame timing info
		DrawTimes(ref vrect, m_Cmdinfo, x, w, graphtype);

		DrawTextFields(graphtype, x, y, w, m_Graph, m_Cmdinfo);
	}

	void PaintLineArt(int x, int y, int w, int graphtype, int maxmsgbytes) {
		ResetLineSegments();

		int lastvalidh = 0;

		Color color = new(255, 255, 255);
		Rectangle rcFill = new();

		int pingheight = NetGraphHeight - LERP_HEIGHT - 2;

		if (net_graphmsecs.GetInt() < 50)
			net_graphmsecs.SetValue(50);

		bool bShowLatency = net_graphshowlatency.GetBool() && graphtype >= 2;

		for (int a = 0; a < w; a++) {
			int i = (IncomingSequence - a) & (TIMINGS - 1);
			int h = bShowLatency ? m_PacketLatency[i].Latency : 0;

			ref var pl = ref m_PacketLatency[i];
			ColorForHeight(ref pl, out color, out int ping);

			if (ping == 0) 
				h = lastvalidh;
			else {
				h = (int)(pingheight * (float)h / net_graphmsecs.GetFloat());
				lastvalidh = h;
			}

			if (h > pingheight) 
				h = pingheight;

			rcFill.X = x + w - a - 1;
			rcFill.Y = y - h;
			rcFill.Width = 1;
			rcFill.Height = h;
			if (ping != 0) {
				rcFill.Height = pl.Choked != 0 ? 2 : 1;
			}

			if (ping == 0)
				DrawLine2(in rcFill, color with { A = 255 }, color with { A = 31});
			else
				DrawLine(in rcFill, color);

			rcFill.Y = y;
			rcFill.Height = 1;

			color[0] = 0;
			color[1] = 255;
			color[2] = 0;

			DrawLine(in rcFill, color with { A = 160 });

			if (graphtype < 2)
				continue;

			rcFill.Y = y - NetGraphHeight - 1;
			rcFill.Height = 1;

			color[0] = 255;
			color[1] = 255;
			color[2] = 255;

			DrawLine(in rcFill, color with { A = 255 });

			rcFill.Y -= 1;

			if (m_PacketLatency[i].Latency > 9995)
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.LocalPlayer], 0, 0, 255))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.OtherPlayers], 0, 255, 0))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.Entities], 255, 0, 0))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.Sounds], 255, 255, 0))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.Events], 0, 255, 255))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.UserMessage], 128, 128, 0))
				continue;
			
			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.EntMessage], 0, 128, 128))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.StringCmd], 128, 0, 0))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.StringTable], 0, 128, 0))
				continue;

			if (!DrawDataSegment(ref rcFill, m_Graph[i].MsgBytes[(int)NetChannelGroup.Voice], 0, 0, 128))
				continue;

			// Final data chunk is total size, don't use solid line routine for this
			h = (int)(m_Graph[i].MsgBytes[(int)NetChannelGroup.Total] / net_scale.GetFloat());

			color[0] = color[1] = color[2] = 240;

			rcFill.Height = 1;
			rcFill.Y = y - NetGraphHeight - 1 - h;

			if (rcFill.Y < 2)
				continue;

			DrawLine(in rcFill, color with { A = 128 });

			// Cache off height
			m_Graph[i].SampleY = rcFill.Y;
			m_Graph[i].SampleHeight = rcFill.Height;
		}

		if (graphtype >= 2) {
			// Draw hatches for first one: on the far right side
			DrawHatches(x, y - NetGraphHeight - 1, maxmsgbytes);

			DrawStreamProgress(x, y, w);
		}

		DrawLineSegments();
	}

	void ResetLineSegments() {
		Rects.Clear();
	}

	void DrawLineSegments() {
		int c = Rects.Count;
		if (c <= 0)
			return;

		Span<LineSegment> rects = Rects.AsSpan();
		for (int i = 0; i < rects.Length; i++) {
			ref LineSegment seg = ref rects[i];
			Surface.DrawSetColor(seg.Color);
			Surface.DrawLine(seg.X1, seg.Y1, seg.X2, seg.Y2);
		}

	}

	void DrawLine(in Rectangle rect, in Color color) {
		DrawLine2(in rect, color, color);
	}

	void DrawLine2(in Rectangle rect, in Color color, in Color color2) {
		var seg = new LineSegment();

		seg.Color = color;
		seg.Color2 = color2;

		if (rect.Width == 1) {
			seg.X1 = rect.X;
			seg.Y1 = rect.Y;
			seg.X2 = rect.X;
			seg.Y2 = rect.Y + rect.Height;
		}
		else if (rect.Height == 1) {
			seg.X1 = rect.X;
			seg.Y1 = rect.Y;
			seg.X2 = rect.X + rect.Width;
			seg.Y2 = rect.Y;
		}
		else {
			Assert(false);
			return;
		}

		Rects.Add(seg);
	}

	void UpdateEstimatedServerFramerate(INetChannelInfo netchannel) {
		netchannel.GetRemoteFramerate(out TimeUnit_t frameTime, out ServerFramerateStdDeviation);
		if (frameTime > float.Epsilon) {
			ServerFramerate = 1.0 / frameTime;
		}
	}

	IFont? GetNetgraphFont() {
		return net_graphproportionalfont.GetBool() ? FontProportional : Font;
	}
}

public interface INetGraphPanel
{
	void Create(IPanel parent);
	void Destroy();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public static INetGraphPanel NetGraph;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}


public class NetGraph : INetGraphPanel
{
	static NetGraph() {
		INetGraphPanel.NetGraph = new NetGraph();
	}

	NetGraphPanel? netGraphPanel;
	public void Create(IPanel parent) {
		netGraphPanel = new NetGraphPanel((Panel)parent);
	}
	public void Destroy() {
		if (netGraphPanel != null) {
			netGraphPanel.SetParent(null);
			netGraphPanel.MarkForDeletion();
			netGraphPanel = null;
		}
	}
}
