using Source.Common.Formats.Keyvalues;

interface IHudLCD
{
	void SetGlobalStat(ReadOnlySpan<char> name, ReadOnlySpan<char> value);
	void AddChatLine(ReadOnlySpan<char> txt);
}

class LCDItem
{

}

class LCDItemText : LCDItem
{

}

class LCDItemAggregate : LCDItem
{

}

class LCDPage
{

}

class LCD : IHudLCD
{
	LCD() { }

	void Reload() { }

	void Init() { }

	void Shutdown() { }

	// int FindTitlePage() { }

	// bool IsPageValid(int currentPage, BasePlayer player) { }

	void Update() { }

	// bool IsConnected() { }

	void ShowItems_R(LCDPage page, TimeUnit_t curTime, List<LCDItem> list, bool showItems) { }

	void DisplayCurrentPage(TimeUnit_t curTime) { }

	// LCDItemIcon ParseItemIcon(LCDPage page, bool bCreateHandles, KeyValues sub) { }

	// LCDItemText ParseItemText(LCDPage page, bool bCreateHandles, KeyValues sub) { }

	// void ParseItems_R(LCDPage page, bool bCreateHandles, KeyValues kv, List<LCDItem> list) { }

	void ParsePage(KeyValues kv) { }

	void ParseIconMappings(KeyValues kv) { }

	void DumpPlayer() { }

	// bool ExtractArrayIndex(string str, UInt64 bufsize, int index) { }

	void LookupToken(ReadOnlySpan<char> _in, string value) { }

	// void BuildUpdatedText(ReadOnlySpan<char> in, CUtlString& out ) { }

	// bool Replace(string str, ReadOnlySpan<char> search, ReadOnlySpan<char> replace) { }

	public void SetGlobalStat(ReadOnlySpan<char> name, ReadOnlySpan<char> value) { }

	public void AddChatLine(ReadOnlySpan<char> txt) { }

	void UpdateChat() { }

	void DoGlobalReplacements(string str) { }

	void ReduceParentheses(string str) { }

	void ParseReplacements(KeyValues kv) { }
}