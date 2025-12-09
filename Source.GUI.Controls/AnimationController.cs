using Source.Common.GUI;
using CommunityToolkit.HighPerformance;
using Source.Common.Mathematics;
using Source.Common.Utilities;
using Source.Common.Formats.Keyvalues;
using Source.Common.Filesystem;
using Source.Common;

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
	public UtlSymId_t AlignPanel;
	public Alignment RelativeAlignment;

}
public struct AnimCmdAnimate
{
	public UtlSymId_t Panel;
	public UtlSymId_t Variable;
	public AnimValue Target;
	public Interpolators InterpolationFunction;
	public float InterpolationParameter;
	public TimeUnit_t StartTime;
	public TimeUnit_t Duration;
	public AnimAlign Align;
}

public struct ActiveAnimation
{
	public Panel? Panel;
	public UtlSymId_t SeqName;
	public UtlSymId_t Variable;
	public bool Started;
	public AnimValue StartValue;
	public AnimValue EndValue;
	public Interpolators Interpolator;
	public float InterpolatorParam;
	public TimeUnit_t StartTime;
	public TimeUnit_t EndTime;
	public bool CanBeCancelled;
	public AnimAlign Align;
}

public struct PostedMessage
{
	public AnimCommandType CommandType;
	public UtlSymId_t SeqName;
	public UtlSymId_t Event;
	public UtlSymId_t Variable;
	public UtlSymId_t Variable2;
	public double StartTime;
	public IPanel Parent;
	public bool CanBeCancelled;
}

public struct RanEvent
{
	public UtlSymId_t SeqName;
	public Panel Parent;
	public override bool Equals(object? obj) => obj is RanEvent other && SeqName == other.SeqName && Parent == other.Parent;
	public override int GetHashCode() => HashCode.Combine(SeqName, Parent);
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

	public void RunAnimationCommand(Panel panel, ReadOnlySpan<char> variable, float target, TimeUnit_t startDelaySeconds, TimeUnit_t durationSeconds, Interpolators interpolator, float animParameter = 0) {
		ulong var = ScriptSymbols.AddString(variable);
		RemoveQueuedAnimationByType(panel, var, 0);

		AnimCmdAnimate animateCmd = new();
		animateCmd.Panel = 0;
		animateCmd.Variable = var;
		animateCmd.Target.A = target;
		animateCmd.InterpolationFunction = interpolator;
		animateCmd.InterpolationParameter = animParameter;
		animateCmd.StartTime = startDelaySeconds;
		animateCmd.Duration = durationSeconds;

		StartCmd_Animate(panel, 0, in animateCmd, true);
	}

	public void RunAnimationCommand(Panel panel, ReadOnlySpan<char> variable, Color target, TimeUnit_t startDelaySeconds, TimeUnit_t durationSeconds, Interpolators interpolator, float animParameter = 0) {
		ulong var = ScriptSymbols.AddString(variable);
		RemoveQueuedAnimationByType(panel, var, 0);

		AnimCmdAnimate animateCmd = new();
		animateCmd.Panel = 0;
		animateCmd.Variable = var;
		animateCmd.Target.A = target[0];
		animateCmd.Target.B = target[1];
		animateCmd.Target.C = target[2];
		animateCmd.Target.D = target[3];
		animateCmd.InterpolationFunction = interpolator;
		animateCmd.InterpolationParameter = animParameter;
		animateCmd.StartTime = startDelaySeconds;
		animateCmd.Duration = durationSeconds;

		StartCmd_Animate(panel, 0, in animateCmd, true);
	}

	TimeUnit_t CurrentTime;

	public void UpdateAnimations(TimeUnit_t curTime) {
		CurrentTime = curTime;

		UpdatePostedMessages(false);
		UpdateActiveAnimations(false);
	}

	// todo
	private void UpdatePostedMessages(bool runToCompletion) {
		List<RanEvent> eventsRanThisFrame = [];

		for (int i = 0; i < PostedMessages.Count; i++) {
			Span<PostedMessage> msgs = PostedMessages.AsSpan();
			ref PostedMessage msgRef = ref msgs[i];

			if (!msgRef.CanBeCancelled && runToCompletion)
				continue;

			if (CurrentTime < msgRef.StartTime && !runToCompletion)
				continue;

			PostedMessage msg = msgRef;
			PostedMessages.RemoveAt(i);
			--i;

			if (!msg.Parent.IsValid())
				continue;

			switch (msg.CommandType) {
				case AnimCommandType.RunEvent:
					break;
				case AnimCommandType.RunEventChild:
					break;
				case AnimCommandType.FireCommand:
					break;
				case AnimCommandType.PlaySound:
					break;
				case AnimCommandType.SetVisible:
					break;
				case AnimCommandType.SetInputEnabled:
					break;
				case AnimCommandType.StopEvent:
					break;
				case AnimCommandType.StopPanelAnimations:
					break;
				case AnimCommandType.StopAnimation:
					break;
				case AnimCommandType.SetFont:
					break;
				case AnimCommandType.SetTexture:
					break;
				case AnimCommandType.SetString:
					break;
			}
		}
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

			if (!anim.Panel.IsValid()) {
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
		if (variable == Position) {
			int x, y;
			panel.GetPos(out x, out y);
			val.A = x - GetRelativeOffset(in anim.Align, true);
			val.B = y - GetRelativeOffset(in anim.Align, false);
		}
		else if (variable == Size) { }
		else if (variable == FgColor) { }
		else if (variable == BgColor) { }
		else if (variable == XPos) { }
		else if (variable == YPos) { }
		else if (variable == Wide) { }
		else if (variable == Tall) { }
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
	IPanel? SizePanel;

	public bool StartAnimationSequence(ReadOnlySpan<char> sequenceName, bool canBeCancelled = true) {
		return StartAnimationSequence(GetParent(), sequenceName, canBeCancelled);
	}

	public struct AnimCmdEvent
	{
		public UtlSymId_t Event;
		public UtlSymId_t Variable;
		public UtlSymId_t Variable2;
		public TimeUnit_t TimeDelay;
	}

	public struct AnimCommand
	{
		public AnimCommandType CommandType;
		public AnimCmdAnimate Animate;
		public AnimCmdEvent RunEvent;
	}

	public struct AnimSequence
	{
		public UtlSymId_t Name;
		public TimeUnit_t Duration;
		public List<AnimCommand> CmdList;
	}
	public bool AutoReloadScript;
	readonly List<AnimSequence> Sequences = [];

	public bool StartAnimationSequence(Panel withinParent, ReadOnlySpan<char> sequenceName, bool canBeCancelled = true) {
		if (AutoReloadScript)
			ReloadScriptFile();

		UtlSymId_t seqName = ScriptSymbols.Find(sequenceName);
		if (seqName == UTL_INVAL_SYMBOL)
			return false;

		RemoveQueuedAnimationCommands(seqName, withinParent);

		int i;
		for (i = 0; i < Sequences.Count; i++) {
			if (Sequences[i].Name == seqName)
				break;
		}

		if (i >= Sequences.Count)
			return false;

		for (int cmdIndex = 0; cmdIndex < Sequences[i].CmdList.Count; cmdIndex++)
			ExecAnimationCommand(seqName, ref Sequences[i].CmdList.AsSpan()[cmdIndex], withinParent, canBeCancelled);

		return true;
	}

	private void ExecAnimationCommand(ulong seqName, ref AnimCommand animCommand, Panel withinParent, bool canBeCancelled) {
		if (animCommand.CommandType == AnimCommandType.Animate) {
			StartCmd_Animate(seqName, ref animCommand.Animate, withinParent, canBeCancelled);
		}
		else {
			PostedMessages.Add(default);
			ref PostedMessage msg = ref PostedMessages.AsSpan()[PostedMessages.Count - 1];
			msg.SeqName = seqName;
			msg.CommandType = animCommand.CommandType;
			msg.Event = animCommand.RunEvent.Event;
			msg.Variable = animCommand.RunEvent.Variable;
			msg.Variable2 = animCommand.RunEvent.Variable2;
			msg.StartTime = CurrentTime + animCommand.RunEvent.TimeDelay;
			msg.Parent = withinParent;
			msg.CanBeCancelled = canBeCancelled;
		}
	}

	private void StartCmd_Animate(UtlSymId_t seqName, ref AnimCmdAnimate cmd, Panel withinParent, bool canBeCancelled) {
		if (withinParent == null)
			return;

		Panel? panel = withinParent.FindChildByName(ScriptSymbols.String(cmd.Panel), true);
		if (panel == null) {
			Panel? parent = GetParent();
			if (parent != null && parent.GetName().Equals(ScriptSymbols.String(cmd.Panel), StringComparison.OrdinalIgnoreCase))
				panel = parent;
		}

		if (panel == null)
			return;

		StartCmd_Animate(panel, seqName, cmd, canBeCancelled);
	}

	private void RemoveQueuedAnimationCommands(UtlSymId_t seqName, Panel withinParent) {
		for (int i = 0; i < PostedMessages.Count; i++) {
			if ((PostedMessages[i].SeqName == seqName) && (withinParent == null || (PostedMessages[i].Parent == withinParent))) {
				PostedMessages.RemoveAt(i);
				--i;
			}
		}

		for (int i = 0; i < ActiveAnimations.Count; i++) {
			if (ActiveAnimations[i].SeqName != seqName)
				continue;

			if (withinParent != null) {
				Panel? animPanel = ActiveAnimations[i].Panel;

				if (animPanel == null)
					continue;

				Panel? foundPanel = withinParent.FindChildByName(animPanel.GetName(), true);

				if (foundPanel != animPanel)
					continue;
			}

			ActiveAnimations.RemoveAt(i);
			--i;
		}
	}

	readonly List<UtlSymId_t> ScriptFileNames = [];

	public void ReloadScriptFile() {
		Sequences.Clear();
		UpdateScreenSize();

		for (int i = 0; i < ScriptFileNames.Count; i++) {
			ReadOnlySpan<char> filename = ScriptSymbols.String(ScriptFileNames[i]);
			if (!filename.IsEmpty) {
				if (LoadScriptFile(filename) == false) {
					Assert(false);
				}
			}
		}
	}

	public bool SetScriptFile(IPanel sizingPanel, ReadOnlySpan<char> fileName, bool wipeAll = false) {
		SizePanel = sizingPanel;

		if (wipeAll) {
			Sequences.Clear();
			ScriptFileNames.Clear();

			CancelAllAnimations();
		}

		UtlSymId_t sFilename = ScriptSymbols.AddString(fileName);
		bool found = false;
		for (int i = 0; i < ScriptFileNames.Count; i++) {
			if (ScriptFileNames[i] == sFilename) {
				found = true;
				break;
			}
		}
		if (!found)
			ScriptFileNames.Add(sFilename);

		UpdateScreenSize();

		return LoadScriptFile(fileName);
	}

	static readonly IFileSystem fileSystem = Singleton<IFileSystem>();

	public bool LoadScriptFile(ReadOnlySpan<char> filename) {
		using IFileHandle? f = fileSystem.Open(filename, FileOpenOptions.Read | FileOpenOptions.Text);

		if (f == null) {
			Warning($"Couldn't find script file {filename}\n");
			return false;
		}

		return ParseScriptFile(f);
	}

	private bool ParseScriptFile(IFileHandle f) {
		return true;
	}

	public void UpdateScreenSize() {

	}
}
