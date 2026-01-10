using Game.Shared;

namespace Game.Client.HUD;

// Since C# can't do multiple inheritance, this interface acts as the bridge, with other sub-panel types "EditableHudElement" for example
// being the actual base class
public interface IHudElement
{
	string? ElementName { get; }
	HideHudBits HiddenBits { get; set; }
	bool Active { get; set; }
	bool NeedsRemove { get; set; }
	bool IsParentedToClientDLLRootPanel { get; set; }
	List<int> HudRenderGroups { get; set; }

	void Init() { }
	void VidInit() { }
	void LevelInit() { }
	void LevelShutdown() { }
	void Reset() { }
	void ProcessInput() { }
	ReadOnlySpan<char> GetName() => ElementName;
	public bool ShouldDraw() {
		bool shouldDraw = !gHUD.IsHidden(HiddenBits);

		if (shouldDraw) {
			int numGroups = HudRenderGroups.Count;
			for (int i = 0; i < numGroups; i++) {
				if (gHUD.IsRenderGroupLockedFor(this, HudRenderGroups[i]))
					return false;
			}
		}

		return shouldDraw;
	}
	bool IsActive() => Active;
	public bool SetActive(bool active) => Active = active;
	void SetHiddenBits(HideHudBits bits) => HiddenBits = bits;
	public virtual int GetRenderGroupPriority() => 0;

	public static void HookMessage(ReadOnlySpan<char> message, UserMessageHook hookFn) => Singleton<UserMessages>().HookMessage(message, hookFn);
}
