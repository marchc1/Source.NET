using Game.Shared;

using Source.Common;
using Source.Common.Engine;

using System.Numerics;

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
	void EnableAlternateSorting(uint renderHandle, bool alternateSorting);
	bool IsRenderableInPVS(IClientRenderable renderable);

	void RenderableChanged(ClientRenderHandle_t handle);
	void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group);
}
