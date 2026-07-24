using ICSharpCode.SharpZipLib.BZip2;

using Source.Common.Commands;
using Source.Common.Filesystem;

namespace Source.Engine;

public class HttpDownloader(IFileSystem fileSystem, ICvar cvar)
{
    static readonly HttpClient http = new() {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public bool EnsureFile(ReadOnlySpan<char> relativePath) {
        if (fileSystem.FileExists(relativePath))   
            return true;
        
        return TryDownloadFile(relativePath);
    } 

    public bool TryDownloadFile(ReadOnlySpan<char> relativePath) {
		ConVar? allowDownload = cvar.FindVar("cl_allowdownload");
		if (allowDownload != null && !allowDownload.GetBool())
			return false;

		string relative = new(relativePath);

		string filter = cvar.FindVar("cl_downloadfilter")?.GetString().ToString() ?? "all";
		if (filter.Equals("none", StringComparison.OrdinalIgnoreCase))
			return false;
		if (filter.Equals("mapsonly", StringComparison.OrdinalIgnoreCase) && !relative.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
			return false;
		if (filter.Equals("nosounds", StringComparison.OrdinalIgnoreCase) &&
			(relative.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)))
			return false;

		string baseUrl = cvar.FindVar("sv_downloadurl")?.GetString().ToString() ?? "";
		if (string.IsNullOrWhiteSpace(baseUrl)) {
			Warning($"Cannot download '{relative}': sv_downloadurl not set by server\n");
			return false;
		}

		if (TryFetch(baseUrl, relative + ".bz2", quietNotFound: true, out byte[]? compressed)) {
			byte[] decompressed;
			try {
				using MemoryStream compressedStream = new(compressed);
				using MemoryStream outputStream = new();
				BZip2.Decompress(compressedStream, outputStream, false);
				decompressed = outputStream.ToArray();
			}
			catch (Exception ex) {
				Warning($"Failed to decompress '{relative}.bz2': {ex.Message}\n");
				return false;
			}

			return WriteFile(relative, decompressed);
		}

		return false;
	}

	bool TryFetch(string baseUrl, string relativePath, bool quietNotFound, out byte[]? data) {
		data = null;
		string url = BuildUrl(baseUrl, relativePath);

		ConMsg($"Downloading '{relativePath}' from {url}\n");

		try {
			using HttpResponseMessage response = WaitWithPump(http.GetAsync(url));
			if (!response.IsSuccessStatusCode) {
				if (!(quietNotFound && response.StatusCode == System.Net.HttpStatusCode.NotFound))
					Warning($"Download failed ({(int)response.StatusCode} {response.ReasonPhrase}): {url}\n");
				return false;
			}

			data = WaitWithPump(response.Content.ReadAsByteArrayAsync());
			return true;
		}
		catch (Exception ex) {
			Warning($"Download failed: {url} ({ex.Message})\n");
			return false;
		}
	}

	static T WaitWithPump<T>(Task<T> task) {
		while (!task.Wait(15))
			Singleton<ClientLauncherAPI>().PumpMessages();

		return task.GetAwaiter().GetResult();
	}

	bool WriteFile(string relative, byte[] data) {
		int lastSlash = relative.LastIndexOf('/');
		if (lastSlash > 0)
			fileSystem.CreateDirHierarchy(relative[..lastSlash], "MOD");

		using (IFileHandle? handle = fileSystem.Open(relative, FileOpenOptions.WriteEx, "MOD")) {
			if (handle == null || !handle.IsOK()) {
				Warning($"Could not open '{relative}' for writing after download\n");
				return false;
			}

			handle.Stream.Write(data, 0, data.Length);
		}

		ConMsg($"Downloaded '{relative}' ({data.Length} bytes)\n");
		return true;
	}

	static string BuildUrl(string baseUrl, string relativePath) {
		string trimmedBase = baseUrl.TrimEnd('/');
		string encoded = string.Join("/", relativePath.Split('/').Select(Uri.EscapeDataString));
		return $"{trimmedBase}/{encoded}";
	}
}