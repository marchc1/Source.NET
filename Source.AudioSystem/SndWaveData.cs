global using static Source.AudioSystem.SndWaveData;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Utilities;

using System.Runtime.CompilerServices;

namespace Source.AudioSystem;

public static class SndWaveData
{
	// PC single buffering implementation
	// UNDONE: Allocate this in cache instead?
	public const int SINGLE_BUFFER_SIZE = 16384;

	public const int DEFAULT_WAV_MEMORY_CACHE = 16 * 1024 * 1024;

	// Dev builds will be missing soundcaches and hitch sometimes, we only care if its being properly launched from steam where sound caches should be complete.
	public static readonly ConVar snd_async_spew_blocking = new("snd_async_spew_blocking", "1", 0, "Spew message to console any time async sound loading blocks on file i/o. ( 0=Off, 1=With -steam only, 2=Always");
	public static readonly ConVar snd_async_spew = new("snd_async_spew", "0", 0, "Spew all async sound reads, including success");
	public static readonly ConVar snd_async_fullyasync = new("snd_async_fullyasync", "0", 0, "All playback is fully async (sound doesn't play until data arrives).");
	public static readonly ConVar snd_async_stream_spew = new("snd_async_stream_spew", "0", 0, "Spew streaming info ( 0=Off, 1=streams, 2=buffers");

	public static bool SndAsyncSpewBlocking() {
		int pref = snd_async_spew_blocking.GetInt();
		return (pref >= 2) || (pref == 1 && CommandLine.FindParm("-steam") != 0);
	}

	public static readonly AsyncWavDataCache wavedatacache = new();

	[ConCommand(helpText: "Flush all unlocked async audio data")]
	static void snd_async_flush() {
		wavedatacache.Flush();
	}

	[ConCommand(helpText: "Show async memory stats")]
	static void snd_async_showmem() {
		wavedatacache.SpewMemoryUsage(1);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *pFileName -
	//			dataOffset -
	//			dataSize -
	//-----------------------------------------------------------------------------
	public static void PrefetchDataStream(ReadOnlySpan<char> fileName, int dataOffset, int dataSize) {
		wavedatacache.PrefetchCache(fileName, dataSize, dataOffset);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : &source -
	//			*pStreamSource -
	//			&io -
	//			*pFileName -
	//			dataOffset -
	//			dataSize -
	// Output : IWaveData
	//-----------------------------------------------------------------------------
	public static IWaveData? CreateWaveDataStream(AudioSourceBase source, IWaveStreamSource streamSource, ReadOnlySpan<char> fileName, int dataStart, int dataSize, SfxTable sfx, int startOffset) {
		WaveDataStreamAsync? stream = new WaveDataStreamAsync(source, streamSource, fileName, dataStart, dataSize, sfx, startOffset);
		if (!stream.IsValid()) {
			stream.Dispose();
			stream = null;
		}
		return stream;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : &source -
	// Output : IWaveData
	//-----------------------------------------------------------------------------
	public static IWaveData CreateWaveDataMemory(AudioSourceBase source) {
		WaveDataMemoryAsync mem = new WaveDataMemoryAsync(source);
		return mem;
	}
}

//-----------------------------------------------------------------------------
// Purpose:
//-----------------------------------------------------------------------------
public struct AsyncWaveParams
{
	public AsyncWaveParams() {
		Prefetch = false;
		CanBeQueued = false;
	}

	public FileNameHandle_t Filename;   // handle to sound item name (i.e. not with sound\ prefix)
	public int DataSize;
	public int SeekPos;
	public int Alignment;
	public bool Prefetch;
	public bool CanBeQueued;
}

//-----------------------------------------------------------------------------
// Purpose: Builds a cache of the data bytes for a specific .wav file
//-----------------------------------------------------------------------------
public class AsyncWaveData
{
	public int DataSize;            // bytes requested
	public int ReadSize;            // bytes actually read
	public byte[]? Alloc;           // memory of buffer (base may not match)
	public int AsyncOffset;
	public int AsyncBytes;
	public int AsyncPriority;
	public bool AsyncPending;
	public float Start;             // time at request invocation
	public float Arrival;           // time at data arrival
	public FileNameHandle_t FileNameHandle;
	public int BufferBytes;         // size of any pre-allocated target buffer
	public bool Loaded;
	public bool Missing;
	public bool PostProcessed;

	//-----------------------------------------------------------------------------
	// Purpose: C'tor
	//-----------------------------------------------------------------------------
	public AsyncWaveData() {
		DataSize = 0;
		ReadSize = 0;
		Alloc = null;
		AsyncPending = false;
		Start = 0.0f;
		Arrival = 0.0f;
		FileNameHandle = 0;
		BufferBytes = 0;
		Loaded = false;
		Missing = false;
		PostProcessed = false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: // APIS required by CDataLRU
	//-----------------------------------------------------------------------------
	public void DestroyResource() {
		AsyncPending = false;

		// delete buffers
		Alloc = null;
	}

	public Span<byte> Data => Alloc == null ? default : Alloc.AsSpan(AsyncOffset);

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : char const
	//-----------------------------------------------------------------------------
	public ReadOnlySpan<char> GetFileName() {
		if (FileNameHandle != 0) {
			ReadOnlySpan<char> sz = filesystem.String(FileNameHandle);
			if (!sz.IsEmpty)
				return sz;
		}

		Assert(false);
		return "";
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : unsigned int
	//-----------------------------------------------------------------------------
	public nuint Size() {
		nuint size = (nuint)IntPtr.Size * 16;

		size += (nuint)DataSize;

		return size;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Static method for CDataLRU
	// Input  : &params -
	// Output : CAsyncWaveData
	//-----------------------------------------------------------------------------
	public static AsyncWaveData CreateResource(in AsyncWaveParams parms) {
		AsyncWaveData data = new AsyncWaveData();
		data.StartAsyncLoading(parms);
		return data;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Static method
	// Input  : &params -
	// Output : static unsigned int
	//-----------------------------------------------------------------------------
	public static nuint EstimatedSize(in AsyncWaveParams parms) {
		nuint size = (nuint)IntPtr.Size * 16;

		size += (nuint)parms.DataSize;

		return size;
	}

	//-----------------------------------------------------------------------------
	// Purpose: NOTE: THIS IS CALLED FROM A THREAD SO YOU CAN'T CALL INTO ANYTHING NON-THREADSAFE
	//  such as CUtlSymbolTable/CUtlDict (many of the CUtl* are non-thread safe)!!!
	// Input  : asyncFilePtr -
	//			numReadBytes -
	//			err -
	//-----------------------------------------------------------------------------
	void OnAsyncCompleted(byte[]? data, int numReadBytes, bool fileOpenError) {
		// Take hold of pointer (we can just use delete[] across .dlls because we are using a shared memory allocator...)
		if (!fileOpenError) {
			Arrival = (float)Platform.Time;

			// Take over ptr
			Alloc = data;
			AsyncOffset = AsyncBytes - DataSize;
			AsyncBytes -= AsyncOffset;
			ReadSize = numReadBytes - AsyncOffset;

			// Needs to be post-processed
			PostProcessed = false;

			// Finished loading
			Loaded = true;
		}
		else {
			// SEE NOTE IN FUNCTION COMMENT ABOVE!!!
			// Tracker 22905, et al.
			// Because this api gets called from the other thread, don't spew warning here as it can
			//  cause a crash in searching CUtlSymbolTables since they use a global var for a LessFunc context!!!
			Missing = true;
		}
	}

	void AsyncFinish() {
		if (!AsyncPending)
			return;

		AsyncPending = false;

		Span<char> filename = stackalloc char[MAX_PATH];
		sprintf(filename, "sound/%s").S(GetFileName());

		IFileHandle? file = filesystem.Open(filename.SliceNullTerminatedString(), FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
		if (file == null) {
			OnAsyncCompleted(null, 0, true);
			return;
		}

		using (file) {
			int bytes = (int)Math.Min(AsyncBytes, Math.Max(0, file.Stream.Length - AsyncOffset));
			byte[] data = GC.AllocateUninitializedArray<byte>(Math.Max(AsyncBytes, 1));
			file.Stream.Seek(AsyncOffset, SeekOrigin.Begin);
			int numRead = 0;
			while (numRead < bytes) {
				int read = file.Stream.Read(data, numRead, bytes - numRead);
				if (read <= 0)
					break;
				numRead += read;
			}
			OnAsyncCompleted(data, numRead, false);
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *destbuffer -
	//			destbufsize -
	//			startoffset -
	//			count -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool BlockingCopyData(Span<byte> destbuffer, int destbufsize, int startoffset, int count) {
		if (!Loaded) {
			// Force it to finish
			// It could finish between the above line and here, but the AsyncFinish call will just have a bogus id, not a big deal
			if (SndAsyncSpewBlocking()) {
				// Force it to finish
				float st = (float)Platform.Time;
				AsyncFinish();
				float ed = (float)Platform.Time;
				Warning($"{Platform.Time:F6} BCD:  Async I/O Force {GetFileName()} ({1000.0f * (float)(ed - st):F2} msec / {1000.0f * (float)(Arrival - Start):F2} msec total)\n");
			}
			else
				AsyncFinish();
		}

		// notify on any error
		if (Missing) {
			// Only warn once
			Missing = false;

			ReadOnlySpan<char> fn = filesystem.String(FileNameHandle);
			if (!fn.IsEmpty)
				MaybeReportMissingWav(fn);
		}

		if (!Loaded)
			return false;
		else if (Arrival != 0 && snd_async_spew.GetBool()) {
			DevMsg($"{Platform.Time:F6} Async I/O Read successful {GetFileName()} ({1000.0f * (float)(Arrival - Start):F2} msec)\n");
			Arrival = 0;
		}

		// clamp requested to available
		if (count > ReadSize)
			count = ReadSize - startoffset;

		if (count < 0)
			return false;

		if (count > destbufsize) {
			AssertMsg(false, $"Buffer size ({destbufsize}) is less than copied bytes count ({count}).");
			return false;
		}

		// Copy data from stream buffer
		Data.Slice(startoffset - AsyncOffset, count).CopyTo(destbuffer);

		return true;
	}

	public bool IsCurrentlyLoading() {
		if (Loaded)
			return true;
		if (AsyncPending)
			return true;
		return false;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : **ppData -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool BlockingGetDataPointer(out Span<byte> data) {
		data = default;
		if (!Loaded) {
			// Force it to finish
			// It could finish between the above line and here, but the AsyncFinish call will just have a bogus id, not a big deal
			if (SndAsyncSpewBlocking()) {
				float st = (float)Platform.Time;
				AsyncFinish();
				float ed = (float)Platform.Time;
				Warning($"{Platform.Time:F6} BlockingGetDataPointer:  Async I/O Force {GetFileName()} ({1000.0f * (float)(ed - st):F2} msec / {1000.0f * (float)(Arrival - Start):F2} msec total )\n");
			}
			else
				AsyncFinish();
		}

		// notify on any error
		if (Missing) {
			// Only warn once
			Missing = false;

			ReadOnlySpan<char> fn = filesystem.String(FileNameHandle);
			if (!fn.IsEmpty)
				MaybeReportMissingWav(fn);
		}

		if (!Loaded)
			return false;
		else if (Arrival != 0 && snd_async_spew.GetBool()) {
			DevMsg($"{Platform.Time:F6} Async I/O Read successful {GetFileName()} ({1000.0f * (float)(Arrival - Start):F2} msec)\n");
			Arrival = 0;
		}

		data = Data;

		return true;
	}

	public void SetAsyncPriority(int priority) {
		if (AsyncPriority != priority) {
			AsyncPriority = priority;
			if (snd_async_spew.GetBool())
				DevMsg($"{Platform.Time:F6} Async I/O Bumped priority for {GetFileName()} ({1000.0f * (float)(Platform.Time - Start):F2} msec)\n");
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : params -
	//-----------------------------------------------------------------------------
	public void StartAsyncLoading(in AsyncWaveParams parms) {
		Assert(!Loaded);

		// expected to be relative to the sound\ dir
		FileNameHandle = parms.Filename;

		int priority = 1;
		if (parms.Prefetch) {
			// lower the priority of prefetched sounds, so they don't block immediate sounds from being loaded
			priority = 0;
		}

		AsyncOffset = 0;
		AsyncBytes = parms.SeekPos + parms.DataSize;
		AsyncPriority = priority;           // inter list priority, 0=lowest

		Loaded = false;
		Missing = false;
		DataSize = parms.DataSize;
		Start = (float)Platform.Time;
		Arrival = 0;
		ReadSize = 0;
		PostProcessed = false;

		// Commence async I/O
		AsyncPending = true;
		AsyncFinish();
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool GetPostProcessed() => PostProcessed;

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : proc -
	//-----------------------------------------------------------------------------
	public void SetPostProcessed(bool proc) => PostProcessed = proc;

	public static void MaybeReportMissingWav(ReadOnlySpan<char> wav) => AudioSourceWave.MaybeReportMissingWav(wav);
}

//-----------------------------------------------------------------------------
// Purpose: Implements a cache of .wav / .mp3 data based on filename
//-----------------------------------------------------------------------------
public class AsyncWavDataCache
{
	class CacheItem
	{
		public AsyncWaveData Data = null!;
		public int LockCount;
		public long LastUse;
	}

	readonly Dictionary<memhandle_t, CacheItem> cacheSection = [];
	memhandle_t nextHandle = 1;
	long useCounter;
	nuint maxBytes;
	nuint usedBytes;

	readonly SortedDictionary<FileNameHandle_t, memhandle_t> cacheHandles = [];

	bool initialized;
	bool queueCacheUnlocks;
	readonly List<memhandle_t> unlockQueue = [];

	AsyncWaveData? CacheGet(memhandle_t handle, bool touch = true) {
		if (!cacheSection.TryGetValue(handle, out CacheItem? item))
			return null;
		if (touch)
			item.LastUse = ++useCounter;
		return item.Data;
	}

	AsyncWaveData? CacheGetNoTouch(memhandle_t handle) => CacheGet(handle, false);

	AsyncWaveData? CacheLock(memhandle_t handle) {
		if (!cacheSection.TryGetValue(handle, out CacheItem? item))
			return null;
		item.LockCount++;
		item.LastUse = ++useCounter;
		return item.Data;
	}

	int CacheUnlock(memhandle_t handle) {
		if (!cacheSection.TryGetValue(handle, out CacheItem? item))
			return 0;
		if (item.LockCount > 0)
			item.LockCount--;
		int lockCount = item.LockCount;
		EnsureCapacity(0);
		return lockCount;
	}

	void CacheRemove(memhandle_t handle) {
		if (!cacheSection.Remove(handle, out CacheItem? item))
			return;
		usedBytes -= item.Data.Size();
		item.Data.DestroyResource();
	}

	void CacheAge(memhandle_t handle) {
		if (cacheSection.TryGetValue(handle, out CacheItem? item))
			item.LastUse = 0;
	}

	void EnsureCapacity(nuint bytes) {
		while (usedBytes + bytes > maxBytes) {
			memhandle_t oldest = 0;
			long oldestUse = long.MaxValue;
			foreach (KeyValuePair<memhandle_t, CacheItem> kvp in cacheSection) {
				if (kvp.Value.LockCount > 0)
					continue;
				if (kvp.Value.LastUse < oldestUse) {
					oldestUse = kvp.Value.LastUse;
					oldest = kvp.Key;
				}
			}
			if (oldest == 0)
				break;
			CacheRemove(oldest);
		}
	}

	memhandle_t CacheCreate(in AsyncWaveParams parms, bool lockItem = false) {
		EnsureCapacity(AsyncWaveData.EstimatedSize(parms));

		AsyncWaveData data = AsyncWaveData.CreateResource(parms);
		memhandle_t handle = nextHandle++;
		cacheSection[handle] = new CacheItem() { Data = data, LockCount = lockItem ? 1 : 0, LastUse = ++useCounter };
		usedBytes += data.Size();
		return handle;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool Init(nuint memSize) {
		if (initialized)
			return true;

		if (memSize < DEFAULT_WAV_MEMORY_CACHE)
			memSize = DEFAULT_WAV_MEMORY_CACHE;

		maxBytes = memSize;

		initialized = true;
		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void Shutdown() {
		if (!initialized)
			return;

		Clear();

		initialized = false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Creates initial cache object if it doesn't already exist, starts async loading the actual data
	//  in any case.
	// Input  : *filename -
	//			datasize -
	//			startpos -
	// Output : memhandle_t
	//-----------------------------------------------------------------------------
	public memhandle_t AsyncLoadCache(ReadOnlySpan<char> filename, int datasize, int startpos, bool isPrefetch = false) {
		FileNameHandle_t fnh = filesystem.FindOrAddFileName(filename);

		// find or create the handle
		if (!cacheHandles.TryGetValue(fnh, out memhandle_t handle))
			cacheHandles[fnh] = handle = 0;

		// Try and pull it into cache
		AsyncWaveData? data = CacheGet(handle);
		if (data == null) {
			// Try and reload it
			AsyncWaveParams parms = new();
			parms.Filename = fnh;
			parms.DataSize = datasize;
			parms.SeekPos = startpos;
			parms.Prefetch = isPrefetch;
			cacheHandles[fnh] = handle = CacheCreate(parms);
		}

		return handle;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *filename -
	//			datasize -
	//			startpos -
	//-----------------------------------------------------------------------------
	public void PrefetchCache(ReadOnlySpan<char> filename, int datasize, int startpos) {
		// Just do an async load, but don't get cache handle
		AsyncLoadCache(filename, datasize, startpos, true);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *filename -
	//			datasize -
	//			startpos -
	//			*buffer -
	//			bufsize -
	//			copystartpos -
	//			bytestocopy -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool CopyDataIntoMemory(ReadOnlySpan<char> filename, int datasize, int startpos, Span<byte> buffer, int bufsize, int copystartpos, int bytestocopy, out bool postProcessed) {
		postProcessed = false;
		bool bret = false;

		// Add to caching system
		AsyncLoadCache(filename, datasize, startpos);

		FileNameHandle_t fnh = filesystem.FindOrAddFileName(filename);

		// Now look it up, it should be in the system
		if (!cacheHandles.TryGetValue(fnh, out memhandle_t handle)) {
			Assert(false);
			return bret;
		}

		// Now see if the handle has been paged out...
		bret = CopyDataIntoMemory(ref handle, filename, datasize, startpos, buffer, bufsize, copystartpos, bytestocopy, out postProcessed);
		cacheHandles[fnh] = handle;
		return bret;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : handle -
	//			*filename -
	//			datasize -
	//			startpos -
	//			*buffer -
	//			bufsize -
	//			copystartpos -
	//			bytestocopy -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool CopyDataIntoMemory(ref memhandle_t handle, ReadOnlySpan<char> filename, int datasize, int startpos, Span<byte> buffer, int bufsize, int copystartpos, int bytestocopy, out bool postProcessed) {
		postProcessed = false;

		bool bret = false;

		AsyncWaveData? data = CacheLock(handle);
		if (data == null) {
			FileNameHandle_t fnh = filesystem.FindOrAddFileName(filename);

			// Now look it up, it should be in the system
			if (!cacheHandles.ContainsKey(fnh)) {
				Assert(false);
				return false;
			}

			// Try and reload it
			AsyncWaveParams parms = new();
			parms.Filename = fnh;
			parms.DataSize = datasize;
			parms.SeekPos = startpos;

			handle = cacheHandles[fnh] = CacheCreate(parms);
			data = CacheLock(handle);
			if (data == null)
				return bret;
		}

		// Cache entry exists, but if filesize == 0 then the file itself wasn't on disk...
		if (data.DataSize != 0)
			bret = data.BlockingCopyData(buffer, bufsize, copystartpos, bytestocopy);

		postProcessed = data.GetPostProcessed();

		// Release lock
		CacheUnlock(handle);
		return bret;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : handle -
	//			proc -
	//-----------------------------------------------------------------------------
	public void SetPostProcessed(memhandle_t handle, bool proc) {
		AsyncWaveData? data = CacheGet(handle);
		data?.SetPostProcessed(proc);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : handle -
	//-----------------------------------------------------------------------------
	public void Unload(memhandle_t handle) {
		// Don't actually unload, just mark it as stale
		CacheAge(handle);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : handle -
	//			*filename -
	//			datasize -
	//			startpos -
	//			**pData -
	//			copystartpos -
	//			*pbPostProcessed -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool GetDataPointer(ref memhandle_t handle, ReadOnlySpan<char> filename, int datasize, int startpos, out Span<byte> data, int copystartpos, out bool postProcessed) {
		postProcessed = false;

		bool bret = false;
		data = default;

		AsyncWaveData? waveData = CacheLock(handle);
		if (waveData == null) {
			FileNameHandle_t fnh = filesystem.FindOrAddFileName(filename);

			if (!cacheHandles.ContainsKey(fnh)) {
				Assert(false);
				return bret;
			}

			// Try and reload it
			AsyncWaveParams parms = new();
			parms.Filename = fnh;
			parms.DataSize = datasize;
			parms.SeekPos = startpos;

			handle = cacheHandles[fnh] = CacheCreate(parms);
			waveData = CacheLock(handle);
			if (waveData == null)
				return bret;
		}

		// Cache entry exists, but if filesize == 0 then the file itself wasn't on disk...
		if (waveData.DataSize != 0) {
			if (datasize != waveData.DataSize) {
				// We've had issues where we are called with datasize larger than what we read on disk.
				//  Ie: datasize is 277,180, data->m_nDataSize is 263,168
				// This can happen due to a corrupted audio cache, but it's more likely that somehow
				//  we wound up reading the cache data from one language and the file from another.
				DevMsg($"Cached datasize != sound datasize {datasize} - {waveData.DataSize}.\n");
			}
			else if (copystartpos < waveData.DataSize) {
				if (waveData.BlockingGetDataPointer(out data)) {
					data = data[copystartpos..];
					bret = true;
				}
			}
		}

		postProcessed = waveData.GetPostProcessed();

		// Release lock at the end of mixing
		QueueUnlock(handle);
		return bret;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : handle -
	//			*filename -
	//			datasize -
	//			startpos -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool IsDataLoadCompleted(memhandle_t handle, out bool isValid) {
		AsyncWaveData? data = CacheGet(handle);
		if (data == null) {
			isValid = false;
			return false;
		}
		isValid = true;
		// bump the priority
		data.SetAsyncPriority(1);

		return data.Loaded;
	}

	public void RestartDataLoad(ref memhandle_t handle, ReadOnlySpan<char> filename, int dataSize, int startpos) {
		AsyncWaveData? data = CacheGet(handle);
		if (data == null)
			handle = AsyncLoadCache(filename, dataSize, startpos);
	}

	public bool IsDataLoadInProgress(memhandle_t handle) {
		AsyncWaveData? data = CacheGet(handle);
		if (data != null)
			return data.IsCurrentlyLoading();
		return false;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void Flush() {
		foreach (memhandle_t handle in cacheSection.Where(x => x.Value.LockCount == 0).Select(x => x.Key).ToArray())
			CacheRemove(handle);
		SpewMemoryUsage(0);
	}

	public void QueueUnlock(memhandle_t handle) {
		// not queuing right now, just unlock
		if (!queueCacheUnlocks) {
			CacheUnlock(handle);
			return;
		}
		// queue to unlock at the end of mixing
		unlockQueue.Add(handle);
	}

	public void OnMixBegin() {
		Assert(!queueCacheUnlocks);
		queueCacheUnlocks = true;
		Assert(unlockQueue.Count == 0);
	}

	public void OnMixEnd() {
		queueCacheUnlocks = false;
		// flush the unlock queue
		for (int i = 0; i < unlockQueue.Count; i++)
			CacheUnlock(unlockQueue[i]);
		unlockQueue.Clear();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Spew a cache summary to the console
	//-----------------------------------------------------------------------------
	public void SpewMemoryUsage(int level) {
		long bytesUsed = (long)usedBytes;
		long bytesTotal = (long)maxBytes;

		float percent = 100.0f * (float)bytesUsed / (float)bytesTotal;

		Msg($"CAsyncWavDataCache:  {cacheHandles.Count} .wavs total {Q_pretifymem(bytesUsed, 2)}, {percent:F2} % of capacity\n");

		if (level >= 1) {
			foreach (KeyValuePair<FileNameHandle_t, memhandle_t> kvp in cacheHandles) {
				ReadOnlySpan<char> name = filesystem.String(kvp.Key);
				if (name.IsEmpty) {
					Assert(false);
					continue;
				}
				AsyncWaveData? data = CacheGetNoTouch(kvp.Value);
				if (data != null)
					Msg($"\t{Q_pretifymem((long)data.Size()),16} : {name}\n");
				else
					Msg($"\t{"not resident",16} : {name}\n");
			}
			Msg($"CAsyncWavDataCache:  {cacheHandles.Count} .wavs total {Q_pretifymem(bytesUsed, 2)}, {percent:F2} % of capacity\n");
		}
	}

	static string Q_pretifymem(long value, int digitsafterdecimal = 2) {
		double onekb = 1024.0;
		double onemb = onekb * 1024.0;

		string suffix;
		double val;
		if (value > onemb) {
			val = value / onemb;
			suffix = "Mb";
		}
		else if (value > onekb) {
			val = value / onekb;
			suffix = "Kb";
		}
		else {
			val = value;
			suffix = "bytes";
			return $"{value} {suffix}";
		}

		return $"{val.ToString("F" + digitsafterdecimal)} {suffix}";
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	void Clear() {
		foreach (memhandle_t handle in cacheHandles.Values)
			CacheRemove(handle);
		cacheHandles.Clear();
		foreach (memhandle_t handle in cacheSection.Keys.ToArray())
			CacheRemove(handle);
	}
}

//-----------------------------------------------------------------------------
// Purpose: This is an instance of a stream.
//			This contains the file handle and streaming buffer
//			The mixer doesn't know the file is streaming.  The IWaveData
//			abstracts the data access.  The mixer abstracts data encoding/format
//-----------------------------------------------------------------------------
public class WaveDataStreamAsync : IWaveData
{
	readonly AudioSourceBase source;                // wave source
	readonly IWaveStreamSource streamSource;        // streaming
	int sampleSize;                                 // size of a sample in bytes
	int waveSize;                                   // total number of samples in the file

	int bufferSize;                                 // size of buffer in samples
	byte[]? buffer;
	int sampleIndex;
	int bufferCount;
	readonly int dataStart;
	readonly int dataSize;

	memhandle_t cache;
	FileNameHandle_t fileName;

	bool valid;
	AudioSourceCachedInfoHandle audioCacheHandle;
	int cachedDataSize;
	readonly SfxTable sfx;

	public WaveDataStreamAsync(AudioSourceBase source, IWaveStreamSource streamSource, ReadOnlySpan<char> fileName, int fileStart, int fileSize, SfxTable sfx, int startOffset) {
		this.source = source;
		this.streamSource = streamSource;
		dataStart = fileStart;
		dataSize = fileSize;
		cache = 0;
		this.fileName = 0;
		valid = false;
		this.sfx = sfx;

		this.fileName = filesystem.FindOrAddFileName(fileName);

		// nothing in the buffer yet
		sampleIndex = 0;
		bufferCount = 0;

		buffer = new byte[SINGLE_BUFFER_SIZE];

		cachedDataSize = 0;

		if (dataSize <= 0) {
			DevMsg(1, $"Can't find streaming wav file: sound\\{GetFileName()}\n");
			return;
		}

		cache = wavedatacache.AsyncLoadCache(GetFileName(), dataSize, dataStart);

		// size of a sample
		sampleSize = source.SampleSize();
		// size in samples of the buffer
		bufferSize = SINGLE_BUFFER_SIZE / sampleSize;
		// size in samples (not bytes) of the wave itself
		waveSize = fileSize / sampleSize;

		audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_WAV, sfx.IsPrecachedSound(), sfx, ref cachedDataSize);

		valid = true;
	}

	public void Dispose() {
		if (source.IsPlayOnce() && source.CanDelete()) {
			source.SetPlayOnce(false); // in case it gets used again
			wavedatacache.Unload(cache);
		}

		buffer = null;
		GC.SuppressFinalize(this);
	}

	// return the source pointer (mixer needs this to determine some things like sampling rate)
	public AudioSourceBase Source() => source;

	public bool IsValid() => valid;

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : char const
	//-----------------------------------------------------------------------------
	ReadOnlySpan<char> GetFileName() {
		if (fileName != 0) {
			ReadOnlySpan<char> fn = filesystem.String(fileName);
			if (!fn.IsEmpty)
				return fn;
		}

		Assert(false);
		return "";
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool IsReadyToMix() {
		// If not async loaded, start mixing right away
		if (!source.IsAsyncLoad() && !snd_async_fullyasync.GetBool())
			return true;

		bool loaded = wavedatacache.IsDataLoadCompleted(cache, out bool cacheValid);
		if (!cacheValid)
			wavedatacache.RestartDataLoad(ref cache, GetFileName(), dataSize, dataStart);
		return loaded;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Read data from the source - this is the primary function of a IWaveData subclass
	//  Get the data from the buffer (or reload from disk)
	// Input  : **pData -
	//			sampleIndex -
	//			sampleCount -
	//			copyBuf[AUDIOSOURCE_COPYBUF_SIZE] -
	// Output : int
	//-----------------------------------------------------------------------------
	public int ReadSourceData(out ReadOnlySpan<byte> data, int sampleIndex, int sampleCount, Span<byte> copyBuf) {
		data = default;

		// Current file position
		int seekpos = dataStart + this.sampleIndex * sampleSize;

		// wrap position if looping
		if (source.IsLooped()) {
			sampleIndex = streamSource.UpdateLoopingSamplePosition(sampleIndex);
			if (sampleIndex < this.sampleIndex) {
				// looped back, buffer has no samples yet
				this.sampleIndex = sampleIndex;
				bufferCount = 0;

				// update file position
				seekpos = dataStart + sampleIndex * sampleSize;
			}
		}

		// UNDONE: This is an error!!
		// The mixer playing back the stream tried to go backwards!?!?!
		// BUGBUG: Just play the beginning of the buffer until we get to a valid linear position
		if (sampleIndex < this.sampleIndex)
			sampleIndex = this.sampleIndex;

		// calc sample position relative to the current buffer
		// m_sampleIndex is the sample position of the first byte of the buffer
		sampleIndex -= this.sampleIndex;

		// out of range? refresh buffer
		if (sampleIndex >= bufferCount) {
			// advance one buffer (the file is positioned here)
			this.sampleIndex += bufferCount;
			// next sample to load
			sampleIndex -= bufferCount;

			// if the remainder is greated than one buffer size, seek over it.  Otherwise, read the next chunk
			// and leave the remainder as an offset.

			// number of buffers to "skip" (as in the case where we are starting a streaming sound not at the beginning)
			int skips = sampleIndex / bufferSize;

			// If we are skipping over a buffer, do it with a seek instead of a read.
			if (skips != 0) {
				// skip directly to next position
				this.sampleIndex += sampleIndex;
				sampleIndex = 0;
			}

			// move the file to the new position
			seekpos = dataStart + (this.sampleIndex * sampleSize);

			// This is the maximum number of samples we could read from the file
			bufferCount = waveSize - this.sampleIndex;

			// past the end of the file?  stop the wave.
			if (bufferCount <= 0)
				return 0;

			// clamp available samples to buffer size
			if (bufferCount > bufferSize)
				bufferCount = bufferSize;

			// See if we can load in the intial data right out of the cached data lump instead.
			int cacheddatastartpos = seekpos - dataStart;

			// FastGet doesn't call into IsPrecachedSound if the handle appears valid...
			AudioSourceCachedInfo? info = audioCacheHandle.FastGet();
			if (info == null) {
				// Full recache
				info = audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_WAV, sfx.IsPrecachedSound(), sfx, ref cachedDataSize);
			}

			bool startupCacheUsed = false;

			if (info != null &&
				(cachedDataSize > 0) &&
				(cacheddatastartpos < cachedDataSize)) {
				// Get a ptr to the cached data
				byte[]? cacheddata = info.CachedData();
				if (cacheddata != null) {
					// See how many samples of cached data are available (cacheddatastartpos is zero on the first read)
					int availSamples = (cachedDataSize - cacheddatastartpos) / sampleSize;

					// Clamp to size of our internal buffer
					if (availSamples > bufferSize)
						availSamples = bufferSize;

					// Mark how many we are returning
					bufferCount = availSamples;
					// Copy raw sample data directly out of cache
					cacheddata.AsSpan(cacheddatastartpos, availSamples * sampleSize).CopyTo(buffer);

					startupCacheUsed = true;
				}
			}

			// Not in startup cache, grab data from async cache loader (will block if data hasn't arrived yet)
			if (!startupCacheUsed) {
				// read in the max bufferable, available samples
				if (!wavedatacache.CopyDataIntoMemory(
					ref cache,
					GetFileName(),
					dataSize,
					dataStart,
					buffer,
					bufferSize * sampleSize,
					seekpos,
					bufferCount * sampleSize,
					out bool postprocessed)) {
					return 0;
				}

				// do any conversion the source needs (mixer will decode/decompress)
				if (!postprocessed) {
					// Note that we don't set the postprocessed flag on the underlying data, since for streaming we're copying the
					//  original data into this buffer instead.
					streamSource.UpdateSamples(buffer, bufferCount);
				}
			}
		}

		// If we have some samples in the buffer that are within range of the request
		// Use unsigned comparisons so that if sampleIndex is somehow negative that
		// will be treated as out of range.
		if ((uint)sampleIndex < (uint)bufferCount) {
			// Get the desired starting sample
			data = buffer.AsSpan(sampleIndex * sampleSize);

			// max available
			int available = bufferCount - sampleIndex;
			// clamp available to max requested
			if (available > sampleCount)
				available = sampleCount;

			return available;
		}

		return 0;
	}
}

//-----------------------------------------------------------------------------
// Purpose: Iterator for wave data (this is to abstract streaming/buffering)
//-----------------------------------------------------------------------------
public class WaveDataMemoryAsync : IWaveData
{
	readonly AudioSourceBase source;    // pointer to source

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : &source -
	//-----------------------------------------------------------------------------
	public WaveDataMemoryAsync(AudioSourceBase source) {
		this.source = source;
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}

	public AudioSourceBase Source() => source;

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : **pData -
	//			sampleIndex -
	//			sampleCount -
	//			copyBuf[AUDIOSOURCE_COPYBUF_SIZE] -
	// Output : int
	//-----------------------------------------------------------------------------
	public int ReadSourceData(out ReadOnlySpan<byte> data, int sampleIndex, int sampleCount, Span<byte> copyBuf) {
		return source.GetOutputData(out data, sampleIndex, sampleCount, copyBuf);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool IsReadyToMix() {
		if (!source.IsAsyncLoad() && !snd_async_fullyasync.GetBool()) {
			// Wait until we're pending at least
			if (source.GetCacheStatus() == AudioSourceCacheStatus.AUDIO_NOT_LOADED)
				return false;
			return true;
		}

		if (source.IsCached())
			return true;

		source.CacheLoad();

		return false;
	}
}
