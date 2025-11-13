using System.Numerics;

namespace Source.Common;

public class OverlayText {
	// This kinda sucks and we need to implement these in Host and shut them down when appropriate
	// But it's the only way I can think of where we keep OverlayText in Common but implement engine
	// specific functionality in Engine.
	public static event Func<OverlayText, bool>? IsDeadFn;
	public static event Action<OverlayText, TimeUnit_t>? SetEndTimeFn;

	public OverlayText() {
		NextOverlayText = null;
		Origin = default;
		UseOrigin = false;
		LineOffset = 0;
		XPos = 0;
		YPos = 0;
		EndTime = 0.0;
		ServerCount = -1;
		CreationTick = -1;
		R = G = B = A = 255;
	}

	public bool IsDead() => IsDeadFn?.Invoke(this) ?? false;
	public void SetEndTime(TimeUnit_t duration) => SetEndTimeFn?.Invoke(this, duration);

	public Vector3 Origin;
	public bool UseOrigin;
	public int LineOffset;
	public float XPos;
	public float YPos;
	public InlineArray512<char> Text;
	public TimeUnit_t EndTime;          
	public int CreationTick;       
	public int ServerCount;        
	public int R;
	public int G;
	public int B;
	public int A;
	public OverlayText? NextOverlayText;
}
