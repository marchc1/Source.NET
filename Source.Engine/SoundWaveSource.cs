using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.DataCache;

using System;

using static Source.Engine.SoundGlobals;

namespace Source.Engine;

public class AudioSourceWave : AudioSource
{
	public AudioSourceWave(SfxTable sfx, AudioSourceCachedInfo info) {

	}

	int format;

	public override void CacheLoad() {
		throw new NotImplementedException();
	}

	public override void CacheUnload() {
		throw new NotImplementedException();
	}

	public override AudioStatus GetCacheStatus() {
		throw new NotImplementedException();
	}

	public override ReadOnlySpan<char> GetSentence() {
		throw new NotImplementedException();
	}

	public override bool IsLooped() {
		throw new NotImplementedException();
	}

	public override bool IsStereoWav() {
		throw new NotImplementedException();
	}

	public override bool IsStreaming() {
		throw new NotImplementedException();
	}

	public override bool IsVoiceSource() {
		throw new NotImplementedException();
	}

	public override int SampleCount() {
		throw new NotImplementedException();
	}

	public override int SampleRate() {
		throw new NotImplementedException();
	}

	public override int SampleSize() {
		throw new NotImplementedException();
	}

	public int Format() {
		return format;
	}
}

public class AudioSourceStreamWave : AudioSourceWave, IWaveStreamSource
{
	public AudioSourceStreamWave(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) { }
	public int GetLoopingInfo(out int pLoopBlock, out int pNumLeadingSamples, out int pNumTrailingSamples) {
		throw new NotImplementedException();
	}

	public int UpdateLoopingSamplePosition(int samplePosition) {
		throw new NotImplementedException();
	}

	public void UpdateSamples(Span<char> pData, int sampleCount) {
		throw new NotImplementedException();
	}
}

public class AudioSourceMemWave : AudioSourceWave
{
	public AudioSourceMemWave(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) { }
}

public partial class Sound
{
	public static AudioSource? Audio_CreateStreamedWave(SfxTable sfx) {
		if (Audio_IsMP3(sfx.GetFileName()))
			return Audio_CreateStreamedMP3(sfx);
		return CreateWave(sfx, true);
	}

	public static AudioSource? Audio_CreateMemoryWave(SfxTable sfx) {
		if (Audio_IsMP3(sfx.GetFileName()))
			return Audio_CreateMemoryMP3(sfx);
		return CreateWave(sfx, false);
	}

	public static AudioSource? CreateWave(SfxTable sfx, bool streaming) {
		Assert(sfx != null);

		AudioSourceWave? pWave = null;

		// Caching should always work, so if we failed to cache, it's a problem reading the file data, etc.
		bool bIsMapSound = sfx.IsPrecachedSound();
		AudioSourceCachedInfo? pInfo = audiosourcecache.GetInfo(AudioSourceType.WAV, bIsMapSound, sfx);

		if (pInfo != null && pInfo.Type() != AudioSourceType.Unknown) {
			// create the source from this file
			if (streaming) {
				pWave = new AudioSourceStreamWave(sfx, pInfo);
			}
			else {
				pWave = new AudioSourceMemWave(sfx, pInfo);
			}
		}

		if (pWave != null && pWave.Format() == 0)
			// lack of format indicates failure
			pWave = null;


		return pWave;
	}
}

public class AudioSourceCache : IAudioSourceCache
{
	class SearchPathCache : UtlCachedFileData<AudioSourceCachedInfo>
	{
		public string SearchPath = null!;

		public SearchPathCache(ReadOnlySpan<char> repositoryFileName, ReadOnlySpan<char> searchPath, UtlCachedFileDataType fileCheckType) 
			: base(repositoryFileName, 5, null, fileCheckType) {
			SearchPath = new(searchPath.SliceNullTerminatedString());
			IsUpToDate();
			base.Init();
		}
	}

	public void ForceRecheckDiskInfo() {
		throw new NotImplementedException();
	}

	public AudioSourceCachedInfo? GetInfo(AudioSourceType audiosourcetype, bool soundisprecached, ISfxTable sfx) {
		Span<char> fn = stackalloc char[512];
		GetSoundFilename(fn, sfx.GetFileName());

		AudioSourceCachedInfo? info = null;
		SearchPathCache? pCache = LookUpCacheEntry(fn, audiosourcetype, soundisprecached, sfx);
		if (pCache == null)
			return null;

		info = pCache.Get(fn);

		return info;
	}

	readonly List<SearchPathCache> Caches = [];

	private SearchPathCache? LookUpCacheEntry(Span<char> fn, AudioSourceType audiosourcetype, bool soundisprecached, ISfxTable sfx) {
		Span<char> relFilename = stackalloc char[256];
		GetSoundFilename(relFilename, sfx.GetFileName());

		Span<char> absFilename = stackalloc char[256];
		if (filesystem.RelativePathToFullPath(relFilename.SliceNullTerminatedString(), "game", absFilename).IsEmpty)
			return null;

		absFilename = absFilename.SliceNullTerminatedString();
		foreach (var cache in Caches) {
			if (absFilename[..cache.SearchPath.Length].CompareTo(cache.SearchPath, StringComparison.InvariantCultureIgnoreCase) == 0)
				return cache;
		}

		Warning($"Cannot figure out which search path {relFilename.SliceNullTerminatedString()} came from.  Absolute path is {absFilename}\n");

		return null;
	}

	private void GetSoundFilename(Span<char> result, ReadOnlySpan<char> inputFilename) {
		sprintf(result, "sound/%s").S(inputFilename);
		StrTools.FixSlashes(result);
		StrTools.RemoveDotSlashes(result);
		StrTools.StrLower(result);
	}

	readonly ICommandLine CommandLine = Singleton<ICommandLine>();
	bool SndCacheDebug;
	public bool Init() {
#if DEBUG
		Msg("AudioSourceCache: Init\n");
#endif
		SndCacheDebug = CommandLine.FindParm("-sndcachedebug") != 0 ? true : false;

		// Gather up list of search paths
		List<string> vecSearchPaths = ListPool<string>.Shared.Alloc();
		filesystem.GetSearchPaths(vecSearchPaths, "game");

		// Create corresponding caches
		Span<char> searchPathTemp = stackalloc char[MAX_PATH];
		foreach (var searchPath in vecSearchPaths) {

			// Standardize the name
			searchPath.CopyTo(searchPathTemp);
			StrTools.FixSlashes(searchPathTemp);
			StrTools.AppendSlash(searchPathTemp);

			// See if we already have a cache for this search path.
			bool bFound = false;
			ReadOnlySpan<char> szSearchPath = searchPathTemp.SliceNullTerminatedString();
			foreach (var c in Caches) {
				if (szSearchPath.CompareTo(c.SearchPath, StringComparison.InvariantCultureIgnoreCase) == 0) {
					Assert(szSearchPath.CompareTo(c.SearchPath, StringComparison.InvariantCulture) == 0);
					bFound = true;
					break;
				}
			}

			if (bFound)
				continue;

			// Add a ceche
			SearchPathCache cache = CreateCacheForSearchPath(szSearchPath);
			Caches.Add(cache);
		}

		ListPool<string>.Shared.Free(vecSearchPaths);
		return true;
	}

	private SearchPathCache CreateCacheForSearchPath(ReadOnlySpan<char> pszSearchPath) {
		Span<char> szSearchPath = stackalloc char[MAX_PATH];
		pszSearchPath.ClampedCopyTo(szSearchPath);
		StrTools.AppendSlash(szSearchPath);

		// Set the filename for the cache.
		UtlCachedFileDataType eOutOfDateMethod = UtlCachedFileDataType.UseFilesize;
		Span<char> szCacheName = stackalloc char[MAX_PATH + 32];
		szSearchPath.SliceNullTerminatedString().ClampedCopyTo(szCacheName);
		Span<char> dotVpkSlash = StrTools.Stristr(szCacheName, ".vpk/");
		if (!dotVpkSlash.IsEmpty) {
			Assert(dotVpkSlash[5] == '\0');
			Span<char> d = dotVpkSlash[4..]; // backup to where the slash is
			Assert(d[0] == StrTools.CORRECT_PATH_SEPARATOR);
			".sound.cache".CopyTo(d);
		}
		else {
			StrTools.StrConcat(szCacheName, "sound/sound.cache");
			eOutOfDateMethod = UtlCachedFileDataType.UseTimestamp;
		}

		return new SearchPathCache(szCacheName, szSearchPath, eOutOfDateMethod);
	}

	public void LevelInit(ReadOnlySpan<char> mapname) {
		throw new NotImplementedException();
	}

	public void LevelShutdown() {
		throw new NotImplementedException();
	}

	public void RebuildCacheEntry(AudioSourceType audiosourcetype, bool soundisprecached, ISfxTable sfx) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}
}
