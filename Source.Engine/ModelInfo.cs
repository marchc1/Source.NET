using Source.Common;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Engine.Client;

using System.Reflection;
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

	int ModelFrameCount(Model? model) {
		int count = 1;

		if (model == null)
			return count;

		if (model.Type == ModelType.Sprite) {
			return model.Sprite.NumFrames;
		}
		else if (model.Type == ModelType.Studio) {
			count = R_StudioBodyVariations((StudioHeader?)modelloader.GetExtraData(model));
		}

		if (count < 1)
			count = 1;

		return count;
	}

	private int R_StudioBodyVariations(StudioHeader? studiohdr) {
		Span<MStudioBodyParts> pbodypart;
		int i, count;

		if (studiohdr == null)
			return 0;

		count = 1;
		pbodypart = studiohdr.BodyParts(0);

		// Each body part has nummodels variations so there are as many total variations as there
		// are in a matrix of each part by each other part
		for (i = 0; i < studiohdr.NumBodyParts; i++) 
			count = count * pbodypart[i].NumModels;
		
		return count;
	}

	public int GetModelFrameCount(Model? model) {
		return ModelFrameCount(model);
	}

	public StudioHeader? GetStudiomodel(Model? model) {
		if (model!.Type == ModelType.Studio)
			return mdlCache.GetStudioHdr(model.Studio);

		return null;
	}

	public int GetModelSpriteWidth(Model model) {
		if (model.Type != ModelType.Sprite)
			return 0;

		return model.Sprite.Width;
	}

	public int GetModelSpriteHeight(Model model) {
		if (model.Type != ModelType.Sprite)
			return 0;

		return model.Sprite.Height;
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
