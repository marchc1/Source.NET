using Source.Common;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

using Color = Source.Color;

namespace Game.Client;

/// <summary>
/// Entity report panel for debugging entity states
/// </summary>
public class EntityReportPanel : Panel
{
	private struct EntityBits
	{
		public int Bits;
		public float Average;
		public int Peak;
		public float PeakTime;
		public int Flags;
		public float EffectFinishTime;
		public ClientClass? DeletedClientClass;
	}

	public static readonly ConVar cl_entityreport = new("cl_entityreport", "0", FCvar.Cheat, "For debugging, draw entity states to console");
	public static readonly ConVar cl_entityreport_sorted = new("cl_entityreport_sorted", "0", FCvar.Cheat, "For debugging, draw entity states to console in sorted order. [0 = disabled, 1 = average, 2 = current, 3 = peak");

	private const int ENTITYSORT_NONE = 0;
	private const int ENTITYSORT_AVG = 1;
	private const int ENTITYSORT_CURRENT = 2;
	private const int ENTITYSORT_PEAK = 3;

	private const float BITCOUNT_AVERAGE = 0.95f;
	private const float EFFECT_TIME = 1.5f;
	private const float PEAK_LATCH_TIME = 2.0f;

	//private const int FENTITYBITS_ZERO = 0;
	private const int FENTITYBITS_ADD = 0x01;
	private const int FENTITYBITS_LEAVEPVS = 0x02;
	private const int FENTITYBITS_DELETE = 0x04;

	private const int MAX_EDICTS = 4096;

	private static readonly EntityBits[] s_EntityBits = new EntityBits[MAX_EDICTS];

	private IFont? Font;

	public EntityReportPanel(IPanel parent) : base((Panel?)parent, "EntityReportPanel")
	{
		Surface.GetScreenSize(out int width, out int height);
		SetSize(width, height);
		SetPos(0, 0);
		SetVisible(true);
		SetCursor(0);

		Font = null;

		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public override void ApplySchemeSettings(IScheme scheme)
	{
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("DefaultVerySmall", false);
		Assert(Font != null);
	}

	public static void ResetEntityBits()
	{
		Array.Clear(s_EntityBits);
	}

	public static void RecordEntityBits(int entnum, int bitcount)
	{
		if (entnum < 0 || entnum >= MAX_EDICTS)
			return;

		ref var slot = ref s_EntityBits[entnum];

		slot.Bits = bitcount;
		slot.Average = BITCOUNT_AVERAGE * slot.Average + (1.0f - BITCOUNT_AVERAGE) * bitcount;

		if (gpGlobals.RealTime >= slot.PeakTime)
		{
			slot.Peak = 0;
			slot.PeakTime = (float)gpGlobals.RealTime + PEAK_LATCH_TIME;
		}

		if (bitcount > slot.Peak)
		{
			slot.Peak = bitcount;
		}
	}

	public static void RecordAddEntity(int entnum)
	{
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= MAX_EDICTS)
			return;

		ref var slot = ref s_EntityBits[entnum];
		slot.Flags = FENTITYBITS_ADD;
		slot.EffectFinishTime = (float)gpGlobals.RealTime + EFFECT_TIME;
	}

	public static void RecordLeavePVS(int entnum)
	{
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= MAX_EDICTS)
			return;

		ref var slot = ref s_EntityBits[entnum];
		slot.Flags = FENTITYBITS_LEAVEPVS;
		slot.EffectFinishTime = (float)gpGlobals.RealTime + EFFECT_TIME;
	}

	public static void RecordDeleteEntity(int entnum, ClientClass? clientClass)
	{
		if (!cl_entityreport.GetBool() || entnum < 0 || entnum >= MAX_EDICTS)
			return;

		ref var slot = ref s_EntityBits[entnum];
		slot.Flags = FENTITYBITS_DELETE;
		slot.EffectFinishTime = (float)gpGlobals.RealTime + EFFECT_TIME;
		slot.DeletedClientClass = clientClass;
	}

	bool ShouldDraw()
	{
		return cl_entityreport.GetInt() != 0;
	}

	private static int MungeColorValue(float cycle, int value)
	{
		int midpoint;
		int remaining;
		bool invert = false;

		if (value < 128)
		{
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

	private void ApplyEffect(ref EntityBits entry, ref int r, ref int g, ref int b)
	{
		bool effectActive = gpGlobals.RealTime <= entry.EffectFinishTime;
		if (!effectActive)
			return;

		float frequency = 3.0f;

		float frac = (EFFECT_TIME - (entry.EffectFinishTime - (float)gpGlobals.RealTime)) / EFFECT_TIME;
		frac = Math.Min(1.0f, frac);
		frac = Math.Max(0.0f, frac);

		frac *= 2.0f * (float)Math.PI;
		frac = (float)Math.Sin(frequency * frac);

		if ((entry.Flags & FENTITYBITS_LEAVEPVS) != 0)
			r = MungeColorValue(frac, r);
		else if ((entry.Flags & FENTITYBITS_ADD) != 0)
			g = MungeColorValue(frac, g);
		else if ((entry.Flags & FENTITYBITS_DELETE) != 0)
		{
			r = MungeColorValue(frac, r);
			g = MungeColorValue(frac, g);
			b = MungeColorValue(frac, b);
		}
	}

	private bool DrawEntry(int row, int col, int rowHeight, int colWidth, int entityIdx)
	{
		IClientNetworkable? net;
		ClientClass? clientClass;
		bool inpvs;
		int r, g, b, a;

		int top = 5;
		int left = 5;

		net = cl_entitylist.GetClientNetworkable(entityIdx);
		ref var entry = ref s_EntityBits[entityIdx];

		if (net != null && (clientClass = net.GetClientClass()) != null)
		{
			inpvs = !net.IsDormant();
			if (inpvs)
			{
				if (entry.Average >= 5)
				{
					r = 200; g = 200; b = 250;
					a = 255;
				}
				else
				{
					r = 200; g = 255; b = 100;
					a = 255;
				}
			}
			else
			{
				r = 255; g = 150; b = 100;
				a = 255;
			}

			ApplyEffect(ref entry, ref r, ref g, ref b);

			string text = $"({entityIdx}) {clientClass.NetworkName}";

			Surface.DrawColoredText(Font, left + col * colWidth, top + row * rowHeight, (byte)r, (byte)g, (byte)b, (byte)a, text);

			if (inpvs)
			{
				float[] fracs =
				[
					(float)(entry.Bits >> 3) / 100.0f,
					(float)(entry.Peak >> 3) / 100.0f,
					(float)((int)entry.Average >> 3) / 100.0f,
				];
				for (int j = 0; j < 3; j++)
				{
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

	private static int CompareEntityBits(int indexA, int indexB)
	{
		ref var entryA = ref s_EntityBits[indexA];
		ref var entryB = ref s_EntityBits[indexB];

		var netA = cl_entitylist.GetClientNetworkable(indexA);
		var netB = cl_entitylist.GetClientNetworkable(indexB);

		bool dormantA = netA == null || netA.IsDormant();
		bool dormantB = netB == null || netB.IsDormant();

		if (dormantA != dormantB)
		{
			return dormantA ? 1 : -1;
		}

		switch (cl_entityreport_sorted.GetInt())
		{
			case ENTITYSORT_AVG:
				if (entryA.Average > entryB.Average)
					return -1;
				if (entryA.Average < entryB.Average)
					return 1;
				break;
			case ENTITYSORT_CURRENT:
				if (entryA.Bits > entryB.Bits)
					return -1;
				if (entryA.Bits < entryB.Bits)
					return 1;
				break;
			case ENTITYSORT_PEAK:
			default:
				if (entryA.Peak > entryB.Peak)
					return -1;
				if (entryA.Peak < entryB.Peak)
					return 1;
				break;
		}

		return 0;
	}

	public override void Paint()
	{
		if (!ShouldDraw() || Font == null || cl_entitylist == null)
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
		int lastUsed = cl_entitylist.GetHighestEntityIndex();

		while (lastUsed > 0)
		{
			net = cl_entitylist.GetClientNetworkable(lastUsed);
			ref var entry = ref s_EntityBits[lastUsed];

			effectActive = gpGlobals.RealTime <= entry.EffectFinishTime;

			if (net != null && net.GetClientClass() != null)
			{
				break;
			}

			if (effectActive)
				break;

			lastUsed--;
		}

		int start = 0;
		if (cl_entityreport.GetInt() > 1)
			start = cl_entityreport.GetInt();

		if (cl_entityreport_sorted.GetInt() != ENTITYSORT_NONE)
		{
			int[] entityIndices = new int[lastUsed - start + 1];
			int count = lastUsed - start + 1;
			for (int i = 0, entityIdx = start; entityIdx <= lastUsed; ++i, ++entityIdx)
				entityIndices[i] = entityIdx;

			Array.Sort(entityIndices, CompareEntityBits);

			for (int i = 0; i < count; ++i)
			{
				int entityIdx = entityIndices[i];

				if (DrawEntry(row, col, rowHeight, colWidth, entityIdx))
				{
					row++;
					Surface.GetScreenSize(out int screenWidth, out int screenHeight);
					if (top + row * rowHeight > screenHeight - rowHeight)
					{
						row = 0;
						col++;
						if (left + (col + 1) * 200 > screenWidth)
							return;
					}
				}
			}
		}
		else
		{
			for (int i = start; i <= lastUsed; i++)
			{
				DrawEntry(row, col, rowHeight, colWidth, i);

				row++;
				Surface.GetScreenSize(out int screenWidth, out int screenHeight);
				if (top + row * rowHeight > screenHeight - rowHeight)
				{
					row = 0;
					col++;
					if (left + (col + 1) * 200 > screenWidth)
						return;
				}
			}
		}
	}
}

public interface IEntityReportPanel
{
	void Create(IPanel parent);
	void Destroy();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public static IEntityReportPanel EntityReport;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}

public class EntityReport : IEntityReportPanel
{
	static EntityReport()
	{
		IEntityReportPanel.EntityReport = new EntityReport();
	}

	private EntityReportPanel? entityReportPanel;

	public void Create(IPanel parent)
	{
		entityReportPanel = new EntityReportPanel(parent);
	}

	public void Destroy()
	{
		if (entityReportPanel != null)
		{
			entityReportPanel.SetParent(null);
			entityReportPanel.MarkForDeletion();
			entityReportPanel = null;
		}
	}
}
