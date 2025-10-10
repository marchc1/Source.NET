using Source.Common.GUI;
using CommunityToolkit.HighPerformance;
using Source.Common.Mathematics;
using Source.Common.Utilities;
using Source.Common.Formats.Keyvalues;

namespace Source.GUI.Controls;

public enum Interpolators
{
	Linear,
	Accel,
	Deaccel,
	Pulse,
	Flicker,
	SimpleSpline,
	Bounce,
	Bias,
	Gain
}

// Animation controller singleton. But is a panel to receive messages.

public struct AnimValue
{
	public float A;
	public float B;
	public float C;
	public float D;
}

public enum AnimCommandType
{
	Animate,
	RunEvent,
	StopEvent,
	StopAnimation,
	StopPanelAnimations,
	SetFont,
	SetTexture,
	SetString,
	RunEventChild,
	FireCommand,
	PlaySound,
	SetVisible,
	SetInputEnabled,
}

public struct AnimAlign
{
	public bool RelativePosition;
	public ulong AlignPanel;
	public Alignment RelativeAlignment;

}
public struct AnimCmdAnimate
{
	public Panel? Panel;
	public ulong Variable;
	public AnimValue Target;
	public Interpolators InterpolationFunction;
	public float InterpolationParameter;
	public double StartTime;
	public double Duration;
	public AnimAlign Align;
}

public struct ActiveAnimation
{
	public Panel? Panel;
	public ulong SeqName;
	public ulong Variable;
	public bool Started;
	public AnimValue StartValue;
	public AnimValue EndValue;
	public Interpolators Interpolator;
	public float InterpolatorParam;
	public double StartTime;
	public double EndTime;
	public bool CanBeCancelled;
	public AnimAlign Align;
}

public struct PostedMessage {
	public AnimCommandType CommandType;
	public UtlSymId_t SeqName;
	public UtlSymId_t Event;
	public UtlSymId_t Variable;
	public UtlSymId_t Variable2;
	public 	double StartTime;
	public IPanel Parent;
	public bool CanBeCancelled;
}

public class AnimationController : Panel, IAnimationController
{
	List<ActiveAnimation> ActiveAnimations = [];
	List<PostedMessage> PostedMessages = [];

	ulong Position;
	ulong Size;
	ulong FgColor;
	ulong BgColor;
	ulong XPos;
	ulong YPos;
	ulong Wide;
	ulong Tall;

	// Static instance
	public AnimationController() {
		Init();
	}
	// Dynamic instance
	public AnimationController(Panel? parent) : base(parent, null) {
		Init();
	}

	public void Init() {
		SetVisible(false);
		SetProportional(true);

		Position = ScriptSymbols.AddString("position");
		Size = ScriptSymbols.AddString("size");
		FgColor = ScriptSymbols.AddString("fgcolor");
		BgColor = ScriptSymbols.AddString("bgcolor");
		XPos = ScriptSymbols.AddString("xpos");
		YPos = ScriptSymbols.AddString("ypos");
		Wide = ScriptSymbols.AddString("wide");
		Tall = ScriptSymbols.AddString("tall");

		CurrentTime = 0;
	}

	public void RunAnimationCommand(Panel panel, ReadOnlySpan<char> variable, float target, double startDelaySeconds, double durationSeconds, Interpolators interpolator, float animParameter = 0) {
		ulong var = ScriptSymbols.AddString(variable);
		RemoveQueuedAnimationByType(panel, var, 0);

		AnimCmdAnimate animateCmd = new();
		animateCmd.Panel = null;
		animateCmd.Variable = var;
		animateCmd.Target.A = target;
		animateCmd.InterpolationFunction = interpolator;
		animateCmd.InterpolationParameter = animParameter;
		animateCmd.StartTime = startDelaySeconds;
		animateCmd.Duration = durationSeconds;

		StartCmd_Animate(panel, 0, in animateCmd, true);
	}

	double CurrentTime;

	public void UpdateAnimations(double curTime) {
		CurrentTime = curTime;

		UpdatePostedMessages(false);
		UpdateActiveAnimations(false);
	}

	// Todo
	private void UpdatePostedMessages(bool runToCompletion) {

	}
	public void CancelAllAnimations() {
		for (int i = ActiveAnimations.Count - 1; i >= ActiveAnimations.Count; i--)
			if (ActiveAnimations[i].CanBeCancelled)
				ActiveAnimations.RemoveAt(i);

		for (int i = PostedMessages.Count - 1; i >= PostedMessages.Count; i--)
			if (PostedMessages[i].CanBeCancelled)
				PostedMessages.RemoveAt(i);

	}

	private void UpdateActiveAnimations(bool runToCompletion) {
		for (int i = 0; i < ActiveAnimations.Count; i++) {
			Span<ActiveAnimation> anims = ActiveAnimations.AsSpan();
			ref ActiveAnimation anim = ref anims[i];

			if (!anim.CanBeCancelled && runToCompletion)
				continue;

			if (CurrentTime < anim.StartTime && !runToCompletion)
				continue;

			if (!IsValid(anim.Panel)) {
				ActiveAnimations.RemoveAt(i);
				--i;
				continue;
			}

			if (!anim.Started && !runToCompletion) {
				anim.StartValue = GetValue(anim, anim.Panel, anim.Variable);
				anim.Started = true;
			}

			AnimValue val;
			if (CurrentTime >= anim.EndTime || runToCompletion)
				val = anim.EndValue;
			else
				val = GetInterpolatedValue(anim.Interpolator, anim.InterpolatorParam, CurrentTime, anim.StartTime, anim.EndTime, in anim.StartValue, in anim.EndValue);

			SetValue(anim, anim.Panel, anim.Variable, val);

			if (CurrentTime >= anim.EndTime || runToCompletion) {
				ActiveAnimations.RemoveAt(i);
				--i;
			}
		}
	}

	private void SetValue(ActiveAnimation anim, Panel panel, ulong variable, AnimValue value) {
		if (variable == Position) {
			int x = (int)value.A + GetRelativeOffset(anim.Align, true);
			int y = (int)value.B + GetRelativeOffset(anim.Align, false);
			panel.SetPos(x, y);
		}
		else if (variable == Size) 
			panel.SetSize((int)value.A, (int)value.B);
		else if (variable == FgColor) {
			Color col = panel.GetFgColor();
			col[0] = (byte)Math.Clamp((int)value.A, 0, 255);
			col[1] = (byte)Math.Clamp((int)value.B, 0, 255);
			col[2] = (byte)Math.Clamp((int)value.C, 0, 255);
			col[3] = (byte)Math.Clamp((int)value.D, 0, 255);
			panel.SetFgColor(col);
		}
		else if (variable == BgColor) {
			Color col = panel.GetBgColor();
			col[0] = (byte)Math.Clamp((int)value.A, 0, 255);
			col[1] = (byte)Math.Clamp((int)value.B, 0, 255);
			col[2] = (byte)Math.Clamp((int)value.C, 0, 255);
			col[3] = (byte)Math.Clamp((int)value.D, 0, 255);
			panel.SetBgColor(col);
		}
		else if (variable == XPos) 
			panel.SetPos((int)value.A + GetRelativeOffset(anim.Align, true), panel.GetY());
		else if (variable == YPos) 
			panel.SetPos(panel.GetX(), (int)value.A + GetRelativeOffset(anim.Align, false));
		else if (variable == Wide) 
			panel.SetSize((int)value.A, panel.GetTall());
		else if (variable == Tall) 
			panel.SetSize(panel.GetWide(), (int)value.A);
		else {
			KeyValues inputData = new KeyValues(ScriptSymbols.String(variable));
			if (value.B == 0.0f && value.C == 0.0f && value.D == 0.0f) {
				inputData.SetFloat(ScriptSymbols.String(variable), value.A);
			}
			else {
				Color col = new();
				col[0] = (byte)Math.Clamp((int)value.A, 0, 255);
				col[1] = (byte)Math.Clamp((int)value.B, 0, 255);
				col[2] = (byte)Math.Clamp((int)value.C, 0, 255);
				col[3] = (byte)Math.Clamp((int)value.D, 0, 255);
				inputData.SetColor(ScriptSymbols.String(variable), col);
			}

			panel.SetInfo(inputData);
		}
	}

	private AnimValue GetValue(ActiveAnimation anim, Panel panel, ulong variable) {
		AnimValue val = new();
		if(variable == Position) {
			int x, y;
			panel.GetPos(out x, out y);
			val.A = x - GetRelativeOffset(in anim.Align, true);
			val.B = y - GetRelativeOffset(in anim.Align, false);
		}
		else if(variable == Size) { }
		else if(variable == FgColor) { }
		else if(variable == BgColor) { }
		else if(variable == XPos) { }
		else if(variable == YPos) { }
		else if(variable == Wide) { }
		else if(variable == Tall) { }
		else {
			KeyValues outputData = new KeyValues(ScriptSymbols.String(variable));
			if (panel.RequestInfo(outputData)) {
				KeyValues? kv = outputData.FindKey(ScriptSymbols.String(variable));
				if (kv != null && kv.Type == KeyValues.Types.Double) {
					val.A = kv.GetFloat();
					val.B = 0.0f;
					val.C = 0.0f;
					val.D = 0.0f;
				}
				else if (kv != null && kv.Type == KeyValues.Types.Color) {
					Color col = kv.GetColor();
					val.A = col[0];
					val.B = col[1];
					val.C = col[2];
					val.D = col[3];
				}
			}
		}
		return val;
	}

	private int GetRelativeOffset(in AnimAlign align, bool xcoord) {
		if (!align.RelativePosition)
			return 0;

		Panel? panel = GetParent()?.FindChildByName(ScriptSymbols.String(align.AlignPanel), true);
		if (panel == null)
			return 0;
		panel.GetBounds(out int x, out int y, out int w, out int h);

		int offset = 0;
		switch (align.RelativeAlignment) {
			default:
			case Alignment.Northwest:
				offset = xcoord ? x : y;
				break;
			case Alignment.North:
				offset = xcoord ? (x + w) / 2 : y;
				break;
			case Alignment.Northeast:
				offset = xcoord ? (x + w) : y;
				break;
			case Alignment.West:
				offset = xcoord ? x : (y + h) / 2;
				break;
			case Alignment.Center:
				offset = xcoord ? (x + w) / 2 : (y + h) / 2;
				break;
			case Alignment.East:
				offset = xcoord ? (x + w) : (y + h) / 2;
				break;
			case Alignment.Southwest:
				offset = xcoord ? x : (y + h);
				break;
			case Alignment.South:
				offset = xcoord ? (x + w) / 2 : (y + h);
				break;
			case Alignment.Southeast:
				offset = xcoord ? (x + w) : (y + h);
				break;
		}

		return offset;
	}

	private AnimValue GetInterpolatedValue(Interpolators interpolator, float interpolatorParam, double currentTime, double startTime, double endTime, in AnimValue startValue, in AnimValue endValue) {
		double pos = (currentTime - startTime) / (endTime - startTime);

		switch (interpolator) {
			case Interpolators.Accel:
				pos *= pos;
				break;
			case Interpolators.Deaccel:
				pos = Math.Sqrt(pos);
				break;
			case Interpolators.SimpleSpline:
				pos = MathLib.SimpleSpline(pos);
				break;
			case Interpolators.Pulse:
				pos = 0.5f + 0.5f * (Math.Cos(pos * 2.0f * Math.PI * interpolatorParam));
				break;
			case Interpolators.Bias:
				pos = MathLib.Bias(pos, interpolatorParam);
				break;
			case Interpolators.Gain:
				pos = MathLib.Gain(pos, interpolatorParam);
				break;
			case Interpolators.Flicker:
				if (Random.Shared.NextSingle() < interpolatorParam) {
					pos = 1.0f;
				}
				else {
					pos = 0.0f;
				}
				break;
			case Interpolators.Bounce:
				const double hit1 = 0.33;
				const double hit2 = 0.67;
				const double hit3 = 1.0;

				if (pos < hit1) {
					pos = 1.0f - Math.Sin(Math.PI * pos / hit1);
				}
				else if (pos < hit2) {
					pos = 0.5f + 0.5f * (1.0f - Math.Sin(Math.PI * (pos - hit1) / (hit2 - hit1)));
				}
				else {
					pos = 0.8f + 0.2f * (1.0f - Math.Sin(Math.PI * (pos - hit2) / (hit3 - hit2)));
				}
				break;
			case Interpolators.Linear:
			default:
				break;
		}

		AnimValue val;
		val.A = (float)(((endValue.A - startValue.A) * pos) + startValue.A);
		val.B = (float)(((endValue.B - startValue.B) * pos) + startValue.B);
		val.C = (float)(((endValue.C - startValue.C) * pos) + startValue.C);
		val.D = (float)(((endValue.D - startValue.D) * pos) + startValue.D);
		return val;
	}

	private void StartCmd_Animate(Panel panel, ulong seqName, in AnimCmdAnimate cmd, bool canBeCancelled) {
		ActiveAnimations.Add(new());

		Span<ActiveAnimation> anims = ActiveAnimations.AsSpan();
		ref ActiveAnimation anim = ref anims[ActiveAnimations.Count - 1];

		anim.Panel = panel;
		anim.SeqName = seqName.Hash();
		anim.Variable = cmd.Variable;
		anim.Interpolator = cmd.InterpolationFunction;
		anim.InterpolatorParam = cmd.InterpolationParameter;
		anim.StartTime = CurrentTime + cmd.StartTime;
		anim.EndTime = anim.StartTime + cmd.Duration;
		anim.Started = false;
		anim.EndValue = cmd.Target;

		anim.CanBeCancelled = canBeCancelled;

		anim.Align = cmd.Align;
	}

	private void RemoveQueuedAnimationByType(Panel panel, ulong variable, ulong sequenceToIgnore) {
		Span<ActiveAnimation> anims = ActiveAnimations.AsSpan();
		for (int i = 0; i < anims.Length; i++) {
			ref ActiveAnimation anim = ref anims[i];
			if (anim.Panel == panel && anim.Variable == variable && anim.SeqName != sequenceToIgnore) {
				ActiveAnimations.RemoveAt(i);
				break;
			}
		}
	}

	static readonly UtlSymbolTable ScriptSymbols = new(true);
	Panel? SizePanel;

	public bool SetScriptFile(Panel sizingPanel, ReadOnlySpan<char> fileName, bool wipeAll = false) {
		SizePanel = sizingPanel;

		if (wipeAll) {
			// todo
			CancelAllAnimations();
		}

		// todo
		return true; // just lie for now
	}

	public void StartAnimationSequence(string v) {

	}
}
