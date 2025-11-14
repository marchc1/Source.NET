using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Client;


public class ClientRenderablesList : IPoolableObject {

	public static readonly ObjectPool<ClientRenderablesList> Shared = new();

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

	public void Init() {
		for (int x = 0; x < RENDER_GROUP_COUNT; x++) {
			for (int y = 0; y < MAX_GROUP_ENTITIES; y++) 
				RenderGroups[x, y] = default;
			RenderGroupCounts[x] = default;
		}
	}

	public void Reset() {}
}

public struct SetupRenderInfo {
	public ClientRenderablesList? RenderList;
	public Vector3 RenderOrigin;
	public Vector3 RenderForward;
	public long RenderFrame;
	public long DetailBuildFrame;
	public float RenderDistSq;
	public bool DrawDetailObjects;
	public bool DrawTranslucentObjects;

	public SetupRenderInfo() {
		DrawDetailObjects = true;
		DrawTranslucentObjects = true;
	}
}

public interface IClientLeafSystem : IClientLeafSystemEngine, IGameSystem {
	void AddRenderable(IClientRenderable renderable, RenderGroup group);
	void BuildRenderablesList(in SetupRenderInfo setupInfo);
	void EnableAlternateSorting(ClientRenderHandle_t renderHandle, bool alternateSorting);
	bool IsRenderableInPVS(IClientRenderable renderable);

	void RenderableChanged(ClientRenderHandle_t handle);
	void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group);
}
