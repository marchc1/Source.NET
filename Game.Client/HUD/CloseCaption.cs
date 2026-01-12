#if false // TODO
using Source.Common.Bitbuffers;
using Source.Common.Hashing;
using Source.GUI.Controls;

using System.Buffers;
using System.Drawing;

namespace Game.Client.HUD;

using CaptionDictionary = List<CaptionLookup>;

// fixme: some of these dont belong here, e.g CaptionLookup

struct VisibleStreamItem
{
	public int Height;
	public int Width;
	public CloseCaptionItem Item;
}

struct CaptionLookup
{
	public uint Hash;
	public int BlockNum;
	public ushort Offset;
	public ushort Length;

	public unsafe void SetHash(string str) {
		int len = str.Length;
		Span<byte> temp = stackalloc byte[len];

		for (int i = 0; i < len; i++)
			temp[i] = (byte)char.ToLowerInvariant(str[i]);

		CRC32_t crc = default;
		CRC32.Init(ref crc);

		fixed (byte* p = temp)
			CRC32.ProcessBuffer(ref crc, p, len);

		CRC32.Final(ref crc);
		Hash = crc;
	}
}

struct CompiledCaptionHeader
{
	public int Magic;
	public int Version;
	public int NumBlocks;
	public int Blocksize;
	public int DirectorySize;
	public int DataOffset;
}

struct AsyncCaption
{
	public CaptionDictionary CaptionDirectory;
	public CompiledCaptionHeader Header;
	public UtlSymId_t DataBaseFile;
	public SortedSet<BlockInfo> RequestedBlocks;

	public AsyncCaption() {
		CaptionDirectory = [];
		Header = default;
		DataBaseFile = UTL_INVAL_SYMBOL;
		RequestedBlocks = new SortedSet<BlockInfo>(BlockInfo.Comparer);
	}

	public void Assign(in AsyncCaption rhs) {
		CaptionDirectory = rhs.CaptionDirectory;
		Header = rhs.Header;
		DataBaseFile = rhs.DataBaseFile;

		RequestedBlocks.Clear();
		foreach (var block in rhs.RequestedBlocks)
			RequestedBlocks.Add(block);
	}

	public struct BlockInfo
	{
		public int FileIndex;
		public int BlockNum;
		public MemoryHandle Handle;
		public static readonly IComparer<BlockInfo> Comparer =
			Comparer<BlockInfo>.Create((lhs, rhs) => {
				int c = lhs.FileIndex.CompareTo(rhs.FileIndex);
				if (c != 0)
					return c;
				return lhs.BlockNum.CompareTo(rhs.BlockNum);
			});
	}
}

struct AsyncCaptionParams
{
	public string DBFile;
	public int FileIndex;
	public int BlockToLoad;
	public int BlockOffset;
	public int BlockSize;
}

struct AsyncCaptionData()
{
	public int BlockNum = -1;
	public byte BlockData = 0;
	public int FileIndex = -1;
	public int BlockSize = 0;
	public bool LoadPending = false;
	public bool LoadComplete = false;
	// public FSAsyncControl_t? AsyncControl = null;

	void DestroyResource() {

	}

	void ReleaseData() {

	}

	void WipeData() {

	}

	AsyncCaptionData GetData() => this;
	uint Size() => (uint)BlockSize;

	void AysncLoad(ReadOnlySpan<char> fileName, int blockOffset) {

	}

	AsyncCaptionData CreateResource(AsyncCaptionParams _params) {
		return new();
	}

	static uint EstimatedSize(AsyncCaptionParams _params) => (uint)_params.BlockSize;
}

class CloseCaptionItem
{

}

class AysncCaptionResourceManager
{

}

[DeclareHudElement(Name = "CHudCloseCaption")]
class HudCloseCaption : EditableHudElement, IHudElement
{
	struct CaptionRepeat()
	{
		public int TokenIndex = 0;
		public int LastEmitTick = 0;
		public TimeUnit_t LastEmitTime = 0;
		public float Interval = 0;
	}

	int LineHeight;
	int GoalHeight;
	int CurrentHeight;
	float GoalAlpha;
	float CurrentAlpha;
	float GoalHeightStartTime;
	float GoalHeightFinishTime;

	[PanelAnimationVar("BgAlpha", "192", "float")] protected float BackgroundAlpha;
	[PanelAnimationVar("GrowTime", "0.25", "float")] protected float GrowTime;
	[PanelAnimationVar("ItemHiddenTime", "0.2", "float")] protected float ItemHiddenTime;
	[PanelAnimationVar("ItemFadeInTime", "0.15", "float")] protected float ItemFadeInTime;
	[PanelAnimationVar("ItemFadeOutTime", "0.3", "float")] protected float ItemFadeOutTime;
	[PanelAnimationVar("topoffset", "40", "int")] protected int TopOffset;

	List<AsyncCaption> AsyncCaptions = [];
	bool Locked;
	bool VisibleDueToDirect;
	bool PaintDebugInfo;
	UtlSymId_t CurrentLanguage;

	public HudCloseCaption(string? panelName) : base(null, "HudCloseCaption") {
		var parent = clientMode.GetViewport();
		SetParent(parent);
	}

	public void LevelInit() { }

	void TogglePaintDebug() { }

	public override void Paint() { }

	public override void OnTick() { }

	public void Reset() { }

	// bool SplitCommand(ReadOnlySpan<char> ppIn, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args) { }

	// bool GetFloatCommandValue(ReadOnlySpan<char> stream, ReadOnlySpan<char> findcmd, float value) { }

	// bool StreamHasCommand(ReadOnlySpan<char> stream, ReadOnlySpan<char> findcmd) { }

	// bool StreamHasCommand(ReadOnlySpan<char> stream, ReadOnlySpan<char> search) { }

	void Process(ReadOnlySpan<char> stream, float duration, ReadOnlySpan<char> tokenstream, bool fromplayer, bool direct) { }

	void CreateFonts() { }

	// void AddWorkUnit(CloseCaptionItem item, WorkUnitParams params) { }

	void ComputeStreamWork(int available_width, CloseCaptionItem item) { }

	void DrawStream(Rectangle rcText, Rectangle rcWindow, CloseCaptionItem item, int fadeLine, float fadeLineAlpha) { }

	// bool GetNoRepeatValue(ReadOnlySpan<char> caption, float retval ) { }

	// bool CaptionTokenLessFunc(CaptionRepeat lhs, CaptionRepeat rhs ) { }

	void ProcessAsyncWork() { }

	void ClearAsyncWork() { }

	void ProcessCaptionDirect(ReadOnlySpan<char> tokenname, float duration, bool fromplayer = false) { }

	void PlayRandomCaption() { }

	// bool AddAsyncWork(ReadOnlySpan<char> tokenstream, bool isStream, float duration, bool fromPlayer, bool direct = false) { }

	void ProcessSentenceCaptionStream(ReadOnlySpan<char> tokenstream) { }

	void _ProcessSentenceCaptionStream(int wordCount, ReadOnlySpan<char> tokenstream, ReadOnlySpan<char> caption_full) { }

	// bool ProcessCaption(ReadOnlySpan<char> tokenName, float duration, bool fromPlayer = false, bool direct = false) { }

	void _ProcessCaption(ReadOnlySpan<char> caption, ReadOnlySpan<char> tokenName, float duration, bool fromPlayer, bool direct) { }

	void MsgFunc_CloseCaption(bf_read msg) { }

	// int GetFontNumber(bool bold, bool italic) { }

	void Flush() { }

	void InitCaptionDictionary(ReadOnlySpan<char> dbfile) { }

	void OnFinishAsyncLoad(int fileIndex, int blockNum, AsyncCaptionData pData) { }

	void Lock() { }

	void Unlock() { }

	void FindSound() { }
}
#endif