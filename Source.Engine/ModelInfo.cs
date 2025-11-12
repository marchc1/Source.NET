using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Engine.Client;

namespace Source.Engine;

public abstract class ModelInfo(IFileSystem filesystem) : IModelInfoClient
{
	public Model? GetModel(int modelIndex) {
		throw new NotImplementedException();
	}

	protected abstract INetworkStringTable? GetDynamicModelStringTable();
	protected abstract int LookupPrecachedModelIndex(ReadOnlySpan<char> name);

	protected static int CLIENTSIDE_TO_MODEL(int i) => i >= 0 ? (-2 - (i * 2 + 1)) : -1;
	protected static int NETDYNAMIC_TO_MODEL(int i) => i >= 0 ? (-2 - (i * 2)) : -1;
	protected static int MODEL_TO_CLIENTSIDE(int i) => (i <= -2 && (i & 1) == 1) ? (-2 - i) >> 1 : -1;
	protected static int MODEL_TO_NETDYNAMIC(int i) => (i <= -2 && (i & 1) == 0) ? (-2 - i) >> 1 : -1;

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

	readonly List<Model> NetworkedDynamicModels = [];
}

public class ModelInfoClient(ClientState cl, IFileSystem filesystem) : ModelInfo(filesystem)
{
	protected override INetworkStringTable? GetDynamicModelStringTable() {
		return cl.DynamicModelsTable;
	}

	protected override int LookupPrecachedModelIndex(ReadOnlySpan<char> name) {
		return cl.LookupModelIndex(name);
	}
}
