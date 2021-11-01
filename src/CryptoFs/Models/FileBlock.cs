using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoFs.Models
{
    public class EncryptedFileBlock
    {
        public byte[] RawData { get; }
        public Span<byte> Nonce { get => this.Data[..this.nonceSize]; }
        public Span<byte> Ciphertext { get => this.Data[this.nonceSize..^this.tagSize]; }
        public Span<byte> Tag { get => this.Data[^this.tagSize..]; }

        private readonly int nonceSize = 24;
        private readonly int tagSize = 16;
        private readonly int dataSize;

        private Span<byte> Data { get => this.RawData.AsSpan()[..dataSize]; }
        // private int messageSize { get => this.RawData.Length - this.tagSize - this.nonceSize; }

        public EncryptedFileBlock(byte[] fileBlock, int dataSize)
        {
            this.RawData = fileBlock;
            this.dataSize = dataSize;
        }
        public EncryptedFileBlock(int fileBlockSize)
        {
            this.RawData = new byte[fileBlockSize];
            this.dataSize = fileBlockSize;
        }
        public EncryptedFileBlock(
            int messageLength,
            int? nonceSize = null,
            int? tagSize = null)
        {
            if (nonceSize != null)
            {
                this.nonceSize = nonceSize.Value;
            }
            if (tagSize != null)
            {
                this.tagSize = tagSize.Value;
            }
            this.dataSize = this.nonceSize + this.tagSize + messageLength;
            this.RawData = new byte[this.dataSize];
        }
    }
}
