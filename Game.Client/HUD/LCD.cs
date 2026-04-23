global using static Game.Client.HUD.LCDGlobals;

using Source.Common.Formats.Keyvalues;

namespace Game.Client.HUD;

public static class LCDGlobals
{
	public static readonly LCD gLCD = new();
	public static IHudLCD hudlcd => gLCD;
}

public interface IHudLCD
{
	void SetGlobalStat(ReadOnlySpan<char> name, ReadOnlySpan<char> value);
	void AddChatLine(ReadOnlySpan<char> txt);
}

public class LCDItem
{

}

public class LCDItemText : LCDItem
{

}

public class LCDItemAggregate : LCDItem
{

}

public class LCDPage
{

}

public class LCD : IHudLCD
{
	public static Dictionary<int, string> GlobalStats = [];

	public LCD() { }

	public void Reload() { }

	public void Init() { }

	void Shutdown() { }

	// int FindTitlePage() { }

	// bool IsPageValid(int currentPage, BasePlayer player) { }

	public void Update() { }

	// bool IsConnected() { }

	public void ShowItems_R(LCDPage page, TimeUnit_t curTime, List<LCDItem> list, bool showItems) { }

	public void DisplayCurrentPage(TimeUnit_t curTime) { }

	// LCDItemIcon ParseItemIcon(LCDPage page, bool bCreateHandles, KeyValues sub) { }

	// LCDItemText ParseItemText(LCDPage page, bool bCreateHandles, KeyValues sub) { }

	// void ParseItems_R(LCDPage page, bool bCreateHandles, KeyValues kv, List<LCDItem> list) { }

	public void ParsePage(KeyValues kv) { }

	public void ParseIconMappings(KeyValues kv) { }

	public void DumpPlayer() { }

	// bool ExtractArrayIndex(string str, UInt64 bufsize, int index) { }

	public void LookupToken(ReadOnlySpan<char> _in, string value) { }

	// void BuildUpdatedText(ReadOnlySpan<char> in, CUtlString& out ) { }

	// bool Replace(string str, ReadOnlySpan<char> search, ReadOnlySpan<char> replace) { }

	public void SetGlobalStat(ReadOnlySpan<char> name, ReadOnlySpan<char> value) {
		GlobalStats[name.ToString().GetHashCode()] = value.ToString();
	}

	public void AddChatLine(ReadOnlySpan<char> txt) { }

	public void UpdateChat() { }

	public void DoGlobalReplacements(string str) { }

	public void ReduceParentheses(string str) { }

	public void ParseReplacements(KeyValues kv) { }
}
