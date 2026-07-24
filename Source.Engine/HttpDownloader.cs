using ICSharpCode.SharpZipLib.BZip2;

using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.GarrysMod;
using Source.Engine.Server;

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

		if (!GMOD.IsValidPath(relativePath))
			return false;

		string filter = cvar.FindVar("cl_downloadfilter")?.GetString().ToString() ?? "all";
		if (filter.Equals("none", StringComparison.OrdinalIgnoreCase))
			return false;
		if (filter.Equals("mapsonly", StringComparison.OrdinalIgnoreCase) && !relativePath.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
			return false;
		if (filter.Equals("nosounds", StringComparison.OrdinalIgnoreCase) && (relativePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)))
			return false;

		ReadOnlySpan<char> baseUrl = BaseServer.sv_downloadurl.GetString();
		if (baseUrl.IsEmpty || baseUrl.IsWhiteSpace()) {
			Warning($"Cannot download '{relativePath}': sv_downloadurl not set by server\n");
			return false;
		}

		if (TryFetch(baseUrl, $"{relativePath}.bz2", quietNotFound: true, out byte[]? compressed)) {
			byte[] decompressed;
			try {
				using MemoryStream compressedStream = new(compressed);
				using MemoryStream outputStream = new();
				BZip2.Decompress(compressedStream, outputStream, false);
				decompressed = outputStream.ToArray();
			}
			catch (Exception ex) {
				Warning($"Failed to decompress '{relativePath}.bz2': {ex.Message}\n");
				return false;
			}

			return WriteFile(relativePath, decompressed);
		}

		return false;
	}

	bool TryFetch(ReadOnlySpan<char> baseUrl, ReadOnlySpan<char> relativePath, bool quietNotFound, out byte[]? data) {
		data = null;
		ReadOnlySpan<char> url = BuildUrl(baseUrl, relativePath);

		ConMsg($"Downloading '{relativePath}' from {url}\n");

		try {
			using HttpResponseMessage response = WaitWithPump(http.GetAsync(new string(url)));
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

	bool WriteFile(ReadOnlySpan<char> relative, byte[] data) {
		int lastSlash = relative.LastIndexOf('/');
		if (lastSlash > 0)
			fileSystem.CreateDirHierarchy(relative[..lastSlash], "download");

		using (IFileHandle? handle = fileSystem.Open(relative, FileOpenOptions.WriteEx, "download")) {
			if (handle == null || !handle.IsOK()) {
				Warning($"Could not open '{relative}' for writing after download\n");
				return false;
			}

			handle.Stream.Write(data, 0, data.Length);
		}

		ConMsg($"Downloaded '{relative}' ({data.Length} bytes) to download/{relative}\n");
		return true;
	}

	static ReadOnlySpan<char> BuildUrl(ReadOnlySpan<char> baseUrl, ReadOnlySpan<char> relativePath) {
		ReadOnlySpan<char> trimmedBase = baseUrl.TrimEnd('/');
		ReadOnlySpan<char> encoded = string.Join("/", relativePath.Split('/').Select(Uri.EscapeDataString));
		return $"{trimmedBase}/{encoded}";
	}
}
