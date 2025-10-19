using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Networking;

namespace Source.Engine;
public class EntityInfo {
	public bool AsDelta;
	public ClientFrame? From;
	public ClientFrame? To;
	public UpdateType UpdateType;
	public int OldEntity = -1;
	public int NewEntity = -1;
	public int HeaderBase = -1;
	public int HeaderCount;


	public void NextOldEntity() {
		if (From != null) {
			OldEntity = From.TransmitEntity.FindNextSetBit(OldEntity + 1);

			if (OldEntity < 0)
				OldEntity = int.MaxValue;
		}
		else
			OldEntity = int.MaxValue;
	}

	public void NextNewEntity() {
		NewEntity = To!.TransmitEntity.FindNextSetBit(NewEntity + 1);
		if (NewEntity < 0)
			NewEntity = int.MaxValue;
	}

	public virtual void Reset() {
		AsDelta = false;
		From = null;
		To = null;
		UpdateType = 0;
		OldEntity = -1;
		NewEntity = -1;
		HeaderBase = -1;
		HeaderCount = 0;
	}
}

public struct PostDataUpdateCall {
	public int Ent;
	public DataUpdateType UpdateType;
}

public class EntityReadInfo : EntityInfo, IPoolableObject {
	static readonly ObjectPool<EntityReadInfo> Pool = new();

	public EntityReadInfo() {
		Reset();
	}

	/// <summary>
	/// Finds or allocates a new EntityReadInfo. Must be followed up with a <see cref="Free(EntityReadInfo)"/> later
	/// </summary>
	public static EntityReadInfo Alloc() => Pool.Alloc();
	/// <summary>
	/// Resets the EntityReadInfo in question and marks it as ready for use.
	/// </summary>
	/// <param name="info"></param>
	public static void Free(EntityReadInfo info) => Pool.Free(info);

	public bf_read? Buf;
	public DeltaEncodingFlags UpdateFlags;
	public bool IsEntity;
	public int Baseline;
	public bool UpdateBaselines;
	public int LocalPlayerBits;
	public int OtherPlayerBits;
	public InlineArrayMaxEdicts<PostDataUpdateCall> PostDataUpdateCalls;
	public int NumPostDataUpdateCalls;

	public void Init() => Reset();
	public override void Reset() {
		base.Reset();

		Buf = null;
		UpdateFlags = 0;
		IsEntity = false;
		Baseline = 0;
		UpdateBaselines = false;
		LocalPlayerBits = 0;
		OtherPlayerBits = 0;
		memreset((Span<PostDataUpdateCall>)PostDataUpdateCalls);
		NumPostDataUpdateCalls = 0;
	}
}
