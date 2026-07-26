
using Microsoft.Win32.SafeHandles;

namespace Source.VPK
{
	internal class ArchivePart : IDisposable
    {
        public readonly uint Size;
		public readonly int Index;
		public readonly string Filename;
		public readonly SafeFileHandle FileHandle;

        public ArchivePart(uint size, int index, string filename)
        {
            Size = size;
            Index = index;
            Filename = filename;
			FileHandle = File.OpenHandle(filename, FileMode.Open, FileAccess.Read);
		}

		public void Dispose() {
			FileHandle.Dispose();
		}
	}
}
