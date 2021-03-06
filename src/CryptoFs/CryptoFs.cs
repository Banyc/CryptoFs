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
        string inputFolderPath,
        string tempFolderPath,
        string outputFolderPath,
        byte[] key,
        bool isEncrypting,
        bool isDeleteFilesAfterCrypting)
    {
        if (key.Length != XChaCha20.KEY_SIZE_IN_BYTES)
        {
            throw new ArgumentException("Key must be 32 bytes long");
        }
        var crypto = new XChaCha20Poly1305(key);
        await CryptFilesInFolderRecursiveAsync(
            inputFolderPath,
            tempFolderPath,
            outputFolderPath,
            outputFolderPath,
            crypto,
            key,
            isEncrypting,
            isDeleteFilesAfterCrypting);
    }

    public async Task CryptFileAsync(
        string inputFilePath,
        string tempFolderPath,
        string outputFolderPath,
        byte[] key,
        bool isEncrypting,
        bool isDeleteFilesAfterCrypting)
    {
        if (key.Length != XChaCha20.KEY_SIZE_IN_BYTES)
        {
            throw new ArgumentException("Key must be 32 bytes long");
        }
        Directory.CreateDirectory(tempFolderPath);
        Directory.CreateDirectory(outputFolderPath);
        string outputFileName;
        if (isEncrypting)
        {
            outputFileName = Path.GetFileName(inputFilePath) + encryptedFileExtension;
        }
        else
        {
            outputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
        }
        string outputFilePath = Path.Combine(outputFolderPath, outputFileName);
        File.Delete(outputFilePath);
        string tempFilePath = Path.Combine(tempFolderPath, outputFileName);
        File.Delete(tempFilePath);
        var crypto = new XChaCha20Poly1305(key);
        using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
        {
            using var inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            await CryptFileAsync(
                inputFileStream,
                tempFileStream,
                crypto,
                key,
                isEncrypting);
        }
        File.Move(tempFilePath, outputFilePath);
        if (isDeleteFilesAfterCrypting)
        {
            File.Delete(inputFilePath);
        }
    }

    private async Task CryptFilesInFolderRecursiveAsync(
        string inputFolderPath,
        string tempFolderPath,
        string outputFolderPath,
        string originalOutputFolderPath,
        XChaCha20Poly1305 crypto,
        byte[] key,
        bool isEncrypting,
        bool isDeleteFilesAfterCrypting)
    {
        var files = Directory.GetFiles(inputFolderPath);
        foreach (var file in files)
        {
            string outputFileName;
            if (isEncrypting)
            {
                if (file.EndsWith(encryptedFileExtension))
                {
                    // Skip files that are already encrypted
                    continue;
                }
                outputFileName = Path.GetFileName(file) + encryptedFileExtension;
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
            if (!File.Exists(outputFilePath))
            {
                // the output file does not exist, so we can encrypt/decrypt input file
                string tempFilePath = Path.Combine(tempFolderPath, outputFileName);
                Directory.CreateDirectory(tempFolderPath);
                Directory.CreateDirectory(outputFolderPath);
                File.Delete(tempFilePath);
                using (var tempFileStream = File.Create(tempFilePath))
                {
                    using var inputFileStream = File.OpenRead(file);
                    await CryptFileAsync(inputFileStream, tempFileStream, crypto, key, isEncrypting);
                }
                File.Move(tempFilePath, outputFilePath);
            }
            else
            {
                // the output file already exists, no need to encrypt/decrypt input file again
            }
            if (isDeleteFilesAfterCrypting)
            {
                File.Delete(file);
            }
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
                isEncrypting,
                isDeleteFilesAfterCrypting);
        }
    }

    private async Task CryptFileAsync(
        FileStream inputStream,
        FileStream outputStream,
        XChaCha20Poly1305 crypto,
        byte[] key,
        bool isEncrypting)
    {
        // treat each block as a message

        int inputFileBlockSize;
        if (isEncrypting)
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
            byte[] outputBlock = CryptBlock(inputFileBlock, numReadByte, crypto, key, isEncrypting,
                previousEncryptedBlock == null ?
                null :
                new EncryptedFileBlock(previousEncryptedBlock, previousEncryptedBlockDataSize).Tag);
            await outputStream.WriteAsync(outputBlock);
            if (isEncrypting)
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
        bool isEncrypting,
        ReadOnlySpan<byte> previousTag)
    {
        if (isEncrypting)
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
