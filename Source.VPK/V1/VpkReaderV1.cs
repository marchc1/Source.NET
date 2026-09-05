using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.VPK.V1
{
	internal class VpkReaderV1 : VpkReaderBase
    {
        public VpkReaderV1(string filename) 
            : base(filename)
        {
        }
        public VpkReaderV1(byte[] file)
            : base(file)
        {
        }

        public override IVpkArchiveHeader ReadArchiveHeader()
        {
            var hdrStructSize = Unsafe.SizeOf<VpkArchiveHeaderV1>();
            var hdrBuff = ReadBytes(stackalloc byte[hdrStructSize]);
            return BytesToStructure<VpkArchiveHeaderV1>(hdrBuff);
        }

        public override uint CalculateEntryOffset(uint offset)
        {
            throw new System.NotImplementedException();
        }
    }
}
