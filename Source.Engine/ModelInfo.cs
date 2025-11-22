using Source.Common;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Engine.Client;

using System.Reflection.Metadata.Ecma335;

namespace Source.Engine;

public abstract class ModelInfo(IFileSystem filesystem, IModelLoader modelloader, IMDLCache mdlCache) : IModelInfoClient
{
	public abstract Model? GetModel(int modelIndex);

	protected abstract INetworkStringTable? GetDynamicModelStringTable();
	protected abstract int LookupPrecachedModelIndex(ReadOnlySpan<char> name);

	protected static int CLIENTSIDE_TO_MODEL(int i) => i >= 0 ? (-2 - (i * 2 + 1)) : -1;
	protected static int NETDYNAMIC_TO_MODEL(int i) => i >= 0 ? (-2 - (i * 2)) : -1;
	protected static int MODEL_TO_CLIENTSIDE(int i) => (i <= -2 && (i & 1) == 1) ? (-2 - i) >> 1 : -1;
	protected static int MODEL_TO_NETDYNAMIC(int i) => (i <= -2 && (i & 1) == 0) ? (-2 - i) >> 1 : -1;

	public ReadOnlySpan<char> GetModelName(Model? model) {
		if (model == null)
			return "?";

		return modelloader.GetName(model);
	}
	public bool IsTranslucentTwoPass(Model? model) {
		return model != null && (model.Flags & ModelFlag.TranslucentTwoPass) != 0;
	}
	public ModelType GetModelType(Model? model) {
		if (model == null)
			return (ModelType)(-1);

		if (model.Type == ModelType.Invalid) {
			if (ClientDynamicModels.ContainsKey(model.FileNameHandle))
				return ModelType.Studio;
			INetworkStringTable? table = GetDynamicModelStringTable();
			if (table != null && table.FindStringIndex(model.StrName) != INetworkStringTable.INVALID_STRING_INDEX)
				return ModelType.Studio;
		}

		return model.Type;
	}
	public int GetModelIndex(ReadOnlySpan<char> name) {
		if (name.IsEmpty)
			return -1;

		int index = LookupPrecachedModelIndex(name);
		if (index != -1)
			return index;

		INetworkStringTable? table = GetDynamicModelStringTable();
		if (table != null) {
			int netdyn = table.FindStringIndex(name);
			if (netdyn != INetworkStringTable.INVALID_STRING_INDEX) {
				Assert(!NetworkedDynamicModels.IsValidIndex(netdyn) || NetworkedDynamicModels[netdyn].StrName.Equals(name, StringComparison.OrdinalIgnoreCase));
				return NETDYNAMIC_TO_MODEL(netdyn);
			}
		}

		return GetModelClientSideIndex(name);
	}

	protected readonly Dictionary<FileNameHandle_t, Model> ClientDynamicModels = [];

	private int GetModelClientSideIndex(ReadOnlySpan<char> name) {
		if (ClientDynamicModels.Count != 0) {
			FileNameHandle_t file = filesystem.FindFileName(name);
			if (file != FILENAMEHANDLE_INVALID && ClientDynamicModels.TryGetValue(file, out var model)) {
				Assert(model.StrName.Equals(name, StringComparison.OrdinalIgnoreCase));
				return CLIENTSIDE_TO_MODEL((int)file); // evil cast
			}
		}

		return -1;
	}

	protected bool IsDynamicModelIndex(int modelIndex) => modelIndex < -1;
	protected Model? LookupDynamicModel(int modelIndex) {
		throw new NotImplementedException("LookupDynamicModel not yet implemented!");
	}

	public VirtualModel? GetVirtualModel(StudioHeader studioHdr) {
		return mdlCache.GetVirtualModelFast(studioHdr, studioHdr.VirtualModel);
	}

	public virtual MDLHandle_t GetCacheHandle(Model model) {
		return model.Type == ModelType.Studio ? model.Studio : MDLHANDLE_INVALID;
	}

	public Memory<byte> GetAnimBlock(StudioHeader studioHdr, int block) {
		return mdlCache.GetAnimBlock(studioHdr.VirtualModel, block);
	}
	public int GetAutoplayList(StudioHeader studioHdr, out Span<short> autoplayList) {
		MDLHandle_t handle = studioHdr.VirtualModel;
		autoplayList = mdlCache.GetAutoplayList(handle).Span;
		return autoplayList.Length;
	}

	readonly List<Model> NetworkedDynamicModels = [];
}

public class ModelInfoClient(ClientState cl, IFileSystem filesystem, IModelLoader modelloader, IMDLCache mdlCache) : ModelInfo(filesystem, modelloader, mdlCache)
{
	public override Model? GetModel(int modelIndex) {
		if (IsDynamicModelIndex(modelIndex))
			return LookupDynamicModel(modelIndex);

		return cl.GetModel(modelIndex);
	}

	protected override INetworkStringTable? GetDynamicModelStringTable() {
		return cl.DynamicModelsTable;
	}

	protected override int LookupPrecachedModelIndex(ReadOnlySpan<char> name) {
		return cl.LookupModelIndex(name);
	}
}
