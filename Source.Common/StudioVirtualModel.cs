using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public partial class VirtualModel
{
	static readonly ObjectPool<ModelLookup> ModelLookups = new();
	public class ModelLookup : IPoolableObject
	{
		public readonly Dictionary<short, short> SeqTable = [];
		public readonly Dictionary<short, short> AnimTable = [];
		public void Init() {
			SeqTable.Clear();
			AnimTable.Clear();
		}
		public void Reset() { }
	}
	public static readonly List<ModelLookup> g_ModelLookup = [];
	public static int g_ModelLookupIndex = -1;
	public static bool HasLookupTable() => g_ModelLookupIndex >= 0;

	public static readonly object g_pSeqTableLock = new();
	public static Dictionary<short, short> GetSeqTable() => g_ModelLookup[g_ModelLookupIndex].SeqTable;

	public static readonly object g_pAnimTableLock = new();
	public static Dictionary<short, short> GetAnimTable() => g_ModelLookup[g_ModelLookupIndex].AnimTable;

	public struct ModelLookupContext : IDisposable
	{
		public ModelLookupContext(int group, StudioHeader studioHdr) {
			LookupIndex = -1;
			if (group == 0 && studioHdr.NumIncludeModels != 0) {
				LookupIndex = g_ModelLookup.Count;
				g_ModelLookupIndex = g_ModelLookup.Count;
				g_ModelLookup.Add(ModelLookups.Alloc());
			}
		}

		public void Dispose() {
			if (LookupIndex >= 0) {
				Assert(LookupIndex == (g_ModelLookup.Count - 1));
				ModelLookups.Free(g_ModelLookup[LookupIndex]);
				g_ModelLookup.RemoveAt(LookupIndex);
				g_ModelLookupIndex = g_ModelLookup.Count - 1;
			}
		}

		private int LookupIndex;
	}

	public void AppendSequences(int group, StudioHeader studioHDR) {

	}
	public void AppendAnimations(int group, StudioHeader studioHDR) {

	}
	public void AppendAttachments(int group, StudioHeader studioHDR) {

	}
	public void AppendPoseParameters(int group, StudioHeader studioHDR) {

	}
	public void AppendBonemap(int group, StudioHeader studioHDR) {

	}
	public void AppendNodes(int group, StudioHeader studioHDR) {

	}
	public void AppendTransitions(int group, StudioHeader studioHDR) {

	}
	public void AppendIKLocks(int group, StudioHeader studioHDR) {

	}
	public void AppendModels(int group, StudioHeader studioHDR) {
		using ModelLookupContext ctx = new ModelLookupContext(group, studioHDR);

		AppendSequences(group, studioHDR);
		AppendAnimations(group, studioHDR);
		AppendBonemap(group, studioHDR);
		AppendAttachments(group, studioHDR);
		AppendPoseParameters(group, studioHDR);
		AppendNodes(group, studioHDR);
		AppendIKLocks(group, studioHDR);
		// todo

		UpdateAutoplaySequences(studioHDR);
	}
	public void UpdateAutoplaySequences(StudioHeader studioHDR) {

	}
}
