global using static Source.Engine.ClientFrame;
namespace Source.Engine;

public class ClientFrame
{
#if GMOD_DLL
	public const int MAX_CLIENT_FRAMES = 256;
#else
	public const int MAX_CLIENT_FRAMES = 128;
#endif

	public int LastEntity;
	public long TickCount;

	public MaxEdictsBitVec TransmitEntity;
	public MaxEdictsBitVec FromBaseline;
	public MaxEdictsBitVec TransmitAlways;
	public ClientFrame? Next;

	public FrameSnapshot? Snapshot;

	internal void Init(int tickcount) {
		TickCount = tickcount;
	}
	internal void Init(FrameSnapshot snapshot) {
		TickCount = snapshot.TickCount;
		SetSnapshot(snapshot);
	}

	internal FrameSnapshot? GetSnapshot() => Snapshot;

	internal void SetSnapshot(FrameSnapshot? snapshot) {
		if (Snapshot == snapshot)
			return;

		snapshot?.AddReference();
		Snapshot?.ReleaseReference();

		Snapshot = snapshot;
	}
}
