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

public class ClientFrameManager
{
	ClientFrame? Frames;
	ClientFrame? LastFrame;
	int FrameCount;
	readonly ClassMemoryPool<ClientFrame> ClientFramePool;

	public ClientFrameManager() {
		ClientFramePool = new();
		Frames = null;
		LastFrame = null;
		FrameCount = 0;
	}

	public int AddClientFrame(ClientFrame frame) {
		Assert(frame.TickCount > 0);

		if (Frames == null) {
			Assert(LastFrame == null && FrameCount == 0);
			Frames = frame;
			LastFrame = frame;
			FrameCount = 1;
			return 1;
		}

		Assert(Frames != null && FrameCount > 0);
		Assert(LastFrame!.Next == null);

		LastFrame.Next = frame;
		LastFrame = frame;
		return ++FrameCount;
	}

	public ClientFrame? GetClientFrame(int tick, bool exact = true) {
		if (tick < 0)
			return null;

		ClientFrame? frame = Frames;
		ClientFrame? lastFrame = frame;

		while (frame != null) {
			if (frame.TickCount >= tick) {
				if (frame.TickCount == tick)
					return frame;

				if (exact)
					return null;

				return lastFrame;
			}

			lastFrame = frame;
			frame = frame.Next;
		}

		if (exact)
			return null;

		return lastFrame;
	}

	public int CountClientFrames() {
#if DEBUG
		int count = 0;
		ClientFrame? f = Frames;

		while (f != null) {
			count++;
			f = f.Next;
		}

		Assert(FrameCount == count);
#endif
		return FrameCount;
	}

	public void RemoveOldestFrame() {
		ClientFrame? frame = Frames;

		if (frame == null)
			return;

		Assert(FrameCount > 0);

		Frames = frame.Next;
		FreeFrame(frame);

		if (--FrameCount == 0) {
			Assert(LastFrame == frame && Frames == null);
			LastFrame = null;
		}
	}

	public void DeleteClientFrames(int tick) {
		if (tick < 0) {
			while (FrameCount > 0)
				RemoveOldestFrame();
		}
		else {
			ClientFrame? frame = Frames;
			LastFrame = null;

			while (frame != null) {
				if (frame.TickCount < tick) {
					ClientFrame? next = frame.Next;

					if (Frames == frame)
						Frames = next;

					FreeFrame(frame);

					if (--FrameCount == 0) {
						Assert(next == null);
						LastFrame = null;
						Frames = null;
						break;
					}

					Assert(LastFrame != frame && FrameCount > 0);

					frame = next;

					LastFrame?.Next = next;
				}
				else {
					Assert(LastFrame == null || LastFrame.Next == frame);

					LastFrame = frame;
					frame = frame.Next;
				}
			}
		}
	}

	public ClientFrame AllocateFrame() => ClientFramePool.Alloc();

	public void FreeFrame(ClientFrame frame) {
		if (ClientFramePool.IsMemoryPoolAllocated(frame))
			ClientFramePool.Free(frame);
	}
}