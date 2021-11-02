using CryptoFs.Models;
using NaCl.Core;

namespace CryptoFs;
public class CryptoFs
{
    private readonly Random random;
    private readonly int plaintextMessageLength;
    private readonly string encryptedFileExtension = ".cryptoFs";

    public CryptoFs(int plaintextMessageLength)
    {
        this.random = new Random(Environment.ProcessId + DateTime.Now.Millisecond);
        this.plaintextMessageLength = plaintextMessageLength;
    }

    public async Task CryptFilesInFolderRecursiveAsync(
        string folderPath,
        string tempFolderPath,
        string outputFolderPath,
        byte[] key,
        bool isEncrypt)
    {
        if (key.Length != XChaCha20.KEY_SIZE_IN_BYTES)
        {
            throw new ArgumentException("Key must be 32 bytes long");
        }
        var crypto = new XChaCha20Poly1305(key);
        await CryptFilesInFolderRecursiveAsync(
            folderPath,
            tempFolderPath,
            outputFolderPath,
            outputFolderPath,
            crypto,
            key,
            isEncrypt);
    }

    public async Task CryptFilesInFolderRecursiveAsync(
        string inputFolderPath,
        string tempFolderPath,
        string outputFolderPath,
        string originalOutputFolderPath,
        XChaCha20Poly1305 crypto,
        byte[] key,
        bool isEncrypt)
    {
        var files = Directory.GetFiles(inputFolderPath);
        foreach (var file in files)
        {
            string outputFileName;
            if (isEncrypt)
            {
                outputFileName = Path.GetFileNameWithoutExtension(file) + encryptedFileExtension;
            }
            else  // it is decrypting
            {
                if (file.EndsWith(encryptedFileExtension))
                {
                    outputFileName = Path.GetFileNameWithoutExtension(file);
                }
                else
                {
                    // it's not encrypted
                    continue;
                }
            }
            string outputFilePath = Path.Combine(outputFolderPath, outputFileName);
            if (File.Exists(outputFilePath))
            {
                // the output file already exists
                continue;
            }
            string tempFilePath = Path.Combine(tempFolderPath, outputFileName);
            Directory.CreateDirectory(tempFolderPath);
            Directory.CreateDirectory(outputFolderPath);
            File.Delete(tempFilePath);
            using (var tempFileStream = File.Create(tempFilePath))
            {
                using var inputFileStream = File.OpenRead(file);
                await CryptFileAsync(inputFileStream, tempFileStream, crypto, key, isEncrypt);
            }
            File.Move(tempFilePath, outputFilePath);
        }

        var folders = Directory.GetDirectories(inputFolderPath);
        foreach (var subfolder in folders)
        {
            if (Path.GetFullPath(subfolder) == Path.GetFullPath(tempFolderPath) ||
                Path.GetFullPath(subfolder) == Path.GetFullPath(originalOutputFolderPath))
            {
                // skip the temp and original output folders
                continue;
            }
            string outputSubfolderPath = Path.Combine(outputFolderPath, Path.GetFileName(subfolder));
            await CryptFilesInFolderRecursiveAsync(
                subfolder,
                tempFolderPath,
                outputSubfolderPath,
                originalOutputFolderPath,
                crypto,
                key,
                isEncrypt);
        }
    }

    public async Task CryptFileAsync(
        FileStream inputStream,
        FileStream outputStream,
        XChaCha20Poly1305 crypto,
        byte[] key,
        bool isEncrypt)
    {
        // treat each block as a message

        int inputFileBlockSize;
        if (isEncrypt)
        {
            inputFileBlockSize = this.plaintextMessageLength;
        }
        else
        {
            inputFileBlockSize = XChaCha20.NONCE_SIZE_IN_BYTES +
                                 this.plaintextMessageLength +
                                 Poly1305.MAC_TAG_SIZE_IN_BYTES;
        }
        byte[] inputFileBlock = new byte[inputFileBlockSize];

        byte[]? previousEncryptedBlock = null;
        int previousEncryptedBlockDataSize = 0;
        int i;
        for (i = 0; i < inputStream.Length; i += inputFileBlockSize)
        {
            int numReadByte = await inputStream.ReadAsync(inputFileBlock);
            byte[] outputBlock = CryptBlock(inputFileBlock, numReadByte, crypto, key, isEncrypt,
                previousEncryptedBlock == null ?
                null :
                new EncryptedFileBlock(previousEncryptedBlock, previousEncryptedBlockDataSize).Tag);
            await outputStream.WriteAsync(outputBlock);
            if (isEncrypt)
            {
                previousEncryptedBlock = outputBlock;
                previousEncryptedBlockDataSize = outputBlock.Length;
            }
            else
            {
                // the previous encrypted block is now `inputFileBlock`
                // swap `previousEncryptedBlock` and `inputFileBlock`
                if (previousEncryptedBlock == null)
                {
                    previousEncryptedBlock = inputFileBlock;
                    inputFileBlock = new byte[inputFileBlockSize];
                }
                else
                {
                    var tmp = inputFileBlock;
                    inputFileBlock = previousEncryptedBlock;
                    previousEncryptedBlock = tmp;
                }
                previousEncryptedBlockDataSize = numReadByte;
            }
        }
    }

    private byte[] CryptBlock(
        byte[] rawBlock,
        int blockDataSize,
        XChaCha20Poly1305 crypto,
        byte[] key,
        bool isEncrypt,
        ReadOnlySpan<byte> previousTag)
    {
        if (isEncrypt)
        {
            return EncryptBlock(rawBlock, blockDataSize, crypto, key, previousTag);
        }
        else
        {
            return DecryptBlock(rawBlock, blockDataSize, crypto, key, previousTag);
        }
    }

    private byte[] EncryptBlock(
        byte[] rawBlock,
        int blockDataSize,
        XChaCha20Poly1305 crypto,
        byte[] key,
        ReadOnlySpan<byte> previousTag)
    {
        var encryptedFileBlock = new EncryptedFileBlock(blockDataSize, null, null);
        this.random.NextBytes(encryptedFileBlock.Nonce);

        crypto.Encrypt(
            nonce: encryptedFileBlock.Nonce,
            plaintext: rawBlock.AsSpan()[..blockDataSize],
            ciphertext: encryptedFileBlock.Ciphertext,
            tag: encryptedFileBlock.Tag,
            associatedData: previousTag);
        return encryptedFileBlock.RawData;
    }

    private byte[] DecryptBlock(
        byte[] rawBlock,
        int blockDataSize,
        XChaCha20Poly1305 crypto,
        byte[] key,
        ReadOnlySpan<byte> previousTag)
    {
        var encryptedFileBlock = new EncryptedFileBlock(rawBlock, blockDataSize);
        var plaintext = new byte[encryptedFileBlock.Ciphertext.Length];

        crypto.Decrypt(
            nonce: encryptedFileBlock.Nonce,
            ciphertext: encryptedFileBlock.Ciphertext,
            tag: encryptedFileBlock.Tag,
            plaintext: plaintext,
            associatedData: previousTag);
        return plaintext;
    }
}
