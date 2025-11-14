using Game.Shared;

using Source.Common;
using Source.Common.Engine;

namespace Game.Client;


public class ClientRenderablesList
{
	public struct Entry
	{
		public IClientRenderable? Renderable;
		public ushort WorldListInfoLeaf;
		public ushort TwoPass;
		public ClientRenderHandle_t RenderHandle;
	}

	public const int RENDER_GROUP_COUNT = (int)RenderGroup.Count;
	public const int MAX_GROUP_ENTITIES = 4096;

	public readonly Entry[,] RenderGroups = new Entry[RENDER_GROUP_COUNT, MAX_GROUP_ENTITIES];
	public readonly int[] RenderGroupCounts = new int[RENDER_GROUP_COUNT];
}

public interface IClientLeafSystem : IClientLeafSystemEngine, IGameSystem {
	void AddRenderable(IClientRenderable renderable, RenderGroup group);
	bool IsRenderableInPVS(IClientRenderable renderable);

	void RenderableChanged(ClientRenderHandle_t handle);
	void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group);
}
