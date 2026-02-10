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

	internal void Init(int tickcount) {
		TickCount = tickcount;
	}
	internal void Init(FrameSnapshot snapshot) {
		TickCount = snapshot.TickCount;
	}
}
