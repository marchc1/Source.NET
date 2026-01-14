using Source.Common;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

struct EntityBits
{
	public int Bits;
	public float Average;
	public int Peak;
	public float PeakTime;
	public int Flags;
	public float EffectFinishTime;
	public ClientClass? DeletedClientClass;
}


public partial class CL
{
	internal readonly EntityBits[] EntityBits = new EntityBits[Constants.MAX_EDICTS];
	public static readonly ConVar cl_entityreport = new("cl_entityreport", "0", FCvar.Cheat, "For debugging, draw entity states to console");
	public static readonly ConVar cl_entityreport_sorted = new("cl_entityreport_sorted", "0", FCvar.Cheat, "For debugging, draw entity states to console in sorted order. [0 = disabled, 1 = average, 2 = current, 3 = peak");

	IClientEntityList entitylist = Singleton<IClientEntityList>();

	internal const float BITCOUNT_AVERAGE = 0.95f;
	internal const float EFFECT_TIME = 1.5f;
	internal const float PEAK_LATCH_TIME = 2.0f;

	internal const int FENTITYBITS_ADD = 0x01;
	internal const int FENTITYBITS_LEAVEPVS = 0x02;
	internal const int FENTITYBITS_DELETE = 0x04;

	EntityReportPanel? entityReportPanel;
	public void CreateEntityReportPanel(Panel parent) {
		entityReportPanel = new(parent);
	}

	internal int CompareEntityBits(int indexA, int indexB) {
		ref var entryA = ref EntityBits[indexA];
		ref var entryB = ref EntityBits[indexB];

		var netA = entitylist.GetClientNetworkable(indexA);
		var netB = entitylist.GetClientNetworkable(indexB);

		bool dormantA = netA == null || netA.IsDormant();
		bool dormantB = netB == null || netB.IsDormant();

		if (dormantA != dormantB) {
			return dormantA ? 1 : -1;
		}

		switch (cl_entityreport_sorted.GetInt()) {
			case (int)EntitySort.AVG:
				if (entryA.Average > entryB.Average)
					return -1;
				if (entryA.Average < entryB.Average)
					return 1;
				break;
			case (int)EntitySort.CURRENT:
				if (entryA.Bits > entryB.Bits)
					return -1;
				if (entryA.Bits < entryB.Bits)
					return 1;
				break;
			case (int)EntitySort.PEAK:
			default:
				if (entryA.Peak > entryB.Peak)
					return -1;
				if (entryA.Peak < entryB.Peak)
					return 1;
				break;
		}

		return 0;
	}

	public void ResetEntityBits() {
		Array.Clear(EntityBits);
	}

	public void RecordEntityBits(int entnum, int bitcount) {
		if (entnum < 0 || entnum >= Constants.MAX_EDICTS)
			return;

		ref var slot = ref EntityBits[entnum];

		slot.Bits = bitcount;
		slot.Average = BITCOUNT_AVERAGE * slot.Average + (1.0f - BITCOUNT_AVERAGE) * bitcount;

		if (Host.RealTime >= slot.PeakTime) {
			slot.Peak = 0;
			slot.PeakTime = (float)Host.RealTime + PEAK_LATCH_TIME;
		}

		if (bitcount > slot.Peak) {
			slot.Peak = bitcount;
		}
	}

	public void RecordAddEntity(int entnum) {
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= Constants.MAX_EDICTS)
			return;

		ref var slot = ref EntityBits[entnum];
		slot.Flags = FENTITYBITS_ADD;
		slot.EffectFinishTime = (float)Host.RealTime + EFFECT_TIME;
	}

	public void RecordLeavePVS(int entnum) {
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= Constants.MAX_EDICTS)
			return;

		ref var slot = ref EntityBits[entnum];
		slot.Flags = FENTITYBITS_LEAVEPVS;
		slot.EffectFinishTime = (float)Host.RealTime + EFFECT_TIME;
	}

	public void RecordDeleteEntity(int entnum, ClientClass? clientClass) {
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= Constants.MAX_EDICTS)
			return;

		ref var slot = ref EntityBits[entnum];
		slot.Flags = FENTITYBITS_DELETE;
		slot.EffectFinishTime = (float)Host.RealTime + EFFECT_TIME;
		slot.DeletedClientClass = clientClass;
	}

	internal void ReallocateDynamicData(int maxClients) {
		if (entitylist != null)
			entitylist.SetMaxEntities(Constants.MAX_EDICTS);
	}

	internal void SetupMapName(ReadOnlySpan<char> readOnlySpan, Span<char> mapname) {
		// todo
	}
}

enum EntitySort
{
	NONE = 0,
	AVG = 1,
	CURRENT = 2,
	PEAK = 3
}
/// <summary>
/// Entity report panel for debugging entity states
/// </summary>
public class EntityReportPanel : Panel
{
	Host Host = Singleton<Host>();
	CL CL = Singleton<CL>();
	IClientEntityList entitylist = Singleton<IClientEntityList>();
	private IFont? Font;

	public EntityReportPanel(IPanel parent) : base((Panel?)parent, "EntityReportPanel") {
		Surface.GetScreenSize(out int width, out int height);
		SetSize(width, height);
		SetPos(0, 0);
		SetVisible(true);
		SetCursor(0);

		Font = null;

		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("DefaultVerySmall", false);
		Assert(Font != null);
	}

	bool ShouldDraw() {
		return CL.cl_entityreport.GetInt() != 0;
	}

	private static int MungeColorValue(float cycle, int value) {
		int midpoint;
		int remaining;
		bool invert = false;

		if (value < 128) {
			invert = true;
			value = 255 - value;
		}

		midpoint = value / 2;
		remaining = value - midpoint;
		midpoint += remaining / 2;

		value = midpoint + (int)((remaining / 2) * cycle);
		if (invert)
			value = 255 - value;

		value = Math.Max(0, value);
		value = Math.Min(255, value);
		return value;
	}

	private void ApplyEffect(ref EntityBits entry, ref int r, ref int g, ref int b) {
		bool effectActive = Host.RealTime <= entry.EffectFinishTime;
		if (!effectActive)
			return;

		float frequency = 3.0f;

		float frac = (CL.EFFECT_TIME - (entry.EffectFinishTime - (float)Host.RealTime)) / CL.EFFECT_TIME;
		frac = Math.Min(1.0f, frac);
		frac = Math.Max(0.0f, frac);

		frac *= 2.0f * (float)Math.PI;
		frac = (float)Math.Sin(frequency * frac);

		if ((entry.Flags & CL.FENTITYBITS_LEAVEPVS) != 0)
			r = MungeColorValue(frac, r);
		else if ((entry.Flags & CL.FENTITYBITS_ADD) != 0)
			g = MungeColorValue(frac, g);
		else if ((entry.Flags & CL.FENTITYBITS_DELETE) != 0) {
			r = MungeColorValue(frac, r);
			g = MungeColorValue(frac, g);
			b = MungeColorValue(frac, b);
		}
	}

	private bool DrawEntry(int row, int col, int rowHeight, int colWidth, int entityIdx) {
		IClientNetworkable? net;
		ClientClass? clientClass;
		bool inpvs;
		int r, g, b, a;

		int top = 5;
		int left = 5;

		net = entitylist.GetClientNetworkable(entityIdx);
		ref var entry = ref CL.EntityBits[entityIdx];

		if (net != null && (clientClass = net.GetClientClass()) != null) {
			inpvs = !net.IsDormant();
			if (inpvs) {
				if (entry.Average >= 5) {
					r = 200; g = 200; b = 250;
					a = 255;
				}
				else {
					r = 200; g = 255; b = 100;
					a = 255;
				}
			}
			else {
				r = 255; g = 150; b = 100;
				a = 255;
			}

			ApplyEffect(ref entry, ref r, ref g, ref b);
			string text = $"({entityIdx}) {clientClass.NetworkName}";

			Surface.DrawColoredText(Font, left + col * colWidth, top + row * rowHeight, (byte)r, (byte)g, (byte)b, (byte)a, text);

			if (inpvs) {
				float[] fracs =
				[
					(float)(entry.Bits >> 3) / 100.0f,
					(float)(entry.Peak >> 3) / 100.0f,
					(float)((int)entry.Average >> 3) / 100.0f,
				];
				for (int j = 0; j < 3; j++) {
					fracs[j] = Math.Max(0.0f, fracs[j]);
					fracs[j] = Math.Min(1.0f, fracs[j]);
				}

				int rcright = left + col * colWidth + colWidth - 2;
				int wide = colWidth / 3;
				int rcleft = rcright - wide;
				int rctop = top + row * rowHeight;
				int rcbottom = rctop + rowHeight - 1;

				Surface.DrawSetColor(new Color(63, 63, 63, 127));
				Surface.DrawFilledRect(rcleft, rctop, rcright, rcbottom);

				Surface.DrawSetColor(new Color(200, 200, 200, 127));
				Surface.DrawOutlinedRect(rcleft, rctop, rcright, rcbottom);

				Surface.DrawSetColor(new Color(200, 255, 100, 192));
				Surface.DrawFilledRect(rcleft, rctop + rowHeight / 2, rcleft + (int)(wide * fracs[0]), rcbottom - 1);

				Surface.DrawSetColor(new Color(192, 192, 192, 255));
				Surface.DrawFilledRect(rcleft + (int)(wide * fracs[2]), rctop + rowHeight / 2, rcleft + (int)(wide * fracs[2]) + 1, rcbottom - 1);

				Surface.DrawSetColor(new Color(192, 0, 0, 255));
				Surface.DrawFilledRect(rcleft + (int)(wide * fracs[1]), rctop + 1, rcleft + (int)(wide * fracs[1]) + 1, rctop + rowHeight / 2);
			}

			return true;
		}

		return false;
	}

	public override void Paint() {
		if (!ShouldDraw() || Font == null || entitylist == null)
			return;

		int top = 5;
		int left = 5;
		int row = 0;
		int col = 0;
		int colWidth = 160;
		int rowHeight = Surface.GetFontTall(Font);

		IClientNetworkable? net;
		bool effectActive;

		// todo: swap once GetMaxEntities has been implemented!
		//int lastUsed = cl_entitylist.GetMaxEntities() - 1;
		int lastUsed = entitylist.GetHighestEntityIndex();

		while (lastUsed > 0) {
			net = entitylist.GetClientNetworkable(lastUsed);
			ref var entry = ref CL.EntityBits[lastUsed];

			effectActive = Host.RealTime <= entry.EffectFinishTime;

			if (net != null && net.GetClientClass() != null) {
				break;
			}

			if (effectActive)
				break;

			lastUsed--;
		}

		int start = 0;
		if (CL.cl_entityreport.GetInt() > 1)
			start = CL.cl_entityreport.GetInt();

		if (CL.cl_entityreport_sorted.GetInt() != (int)EntitySort.NONE) {
			int[] entityIndices = new int[lastUsed - start + 1];
			int count = lastUsed - start + 1;
			for (int i = 0, entityIdx = start; entityIdx <= lastUsed; ++i, ++entityIdx)
				entityIndices[i] = entityIdx;

			Array.Sort(entityIndices, CL.CompareEntityBits);

			for (int i = 0; i < count; ++i) {
				int entityIdx = entityIndices[i];

				if (DrawEntry(row, col, rowHeight, colWidth, entityIdx)) {
					row++;
					Surface.GetScreenSize(out int screenWidth, out int screenHeight);
					if (top + row * rowHeight > screenHeight - rowHeight) {
						row = 0;
						col++;
						if (left + (col + 1) * 200 > screenWidth)
							return;
					}
				}
			}
		}
		else {
			for (int i = start; i <= lastUsed; i++) {
				DrawEntry(row, col, rowHeight, colWidth, i);

				row++;
				Surface.GetScreenSize(out int screenWidth, out int screenHeight);
				if (top + row * rowHeight > screenHeight - rowHeight) {
					row = 0;
					col++;
					if (left + (col + 1) * 200 > screenWidth)
						return;
				}
			}
		}
	}
}
