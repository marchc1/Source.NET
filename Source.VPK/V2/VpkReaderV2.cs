using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.VPK.V2
{
	internal class VpkReaderV2 : VpkReaderBase
    {
        public VpkReaderV2(string filename) 
            : base(filename)
        {
        }

        public VpkReaderV2(byte[] file)
            : base(file)
        {
        }

        public override IVpkArchiveHeader ReadArchiveHeader()
        {
            var hdrStructSize = Unsafe.SizeOf<VpkArchiveHeaderV2>();
            var hdrBuff = ReadBytes(stackalloc byte[hdrStructSize]);
            // skip unknown values
            Read<int>();
            var hdr = BytesToStructure<VpkArchiveHeaderV2>(hdrBuff);
            hdr.FooterLength = (uint)Read<int>();
            Read<int>();
            return hdr;
        }

        public override uint CalculateEntryOffset(uint offset)
        {
            throw new System.NotImplementedException();
        }
    }
}
