// See https://aka.ms/new-console-template for more information
using CommandLine;
using CryptoFs.Cli.Models;

// TEMP
await Crypt(new()
{
    InputFileOrFolderPath = "test/input",
    TempFolderPath = "test/temp",
    IsEncrypting = true,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output"
});
await Crypt(new()
{
    InputFileOrFolderPath = "test/output",
    TempFolderPath = "test/temp",
    IsEncrypting = false,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output2"
});

await Crypt(new()
{
    InputFileOrFolderPath = "test/inplace",
    TempFolderPath = "test/temp",
    IsEncrypting = true,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/inplace",
    IsDeleteFilesAfterCrypting = true,
});
await Crypt(new()
{
    InputFileOrFolderPath = "test/inplace",
    TempFolderPath = "test/temp",
    IsEncrypting = false,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/inplace",
    IsDeleteFilesAfterCrypting = true,
});

await Crypt(new()
{
    InputFileOrFolderPath = "test/input/a.file",
    TempFolderPath = "test/temp",
    IsEncrypting = true,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output",
    IsDeleteFilesAfterCrypting = false,
});
await Crypt(new()
{
    InputFileOrFolderPath = "test/output",
    TempFolderPath = "test/temp",
    IsEncrypting = false,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output2",
    IsDeleteFilesAfterCrypting = false,
});

// // parse arguments
// var parseResult = Parser.Default.ParseArguments<Options>(args);
// parseResult.WithParsed(opts =>
// {
//     Crypt(opts).Wait();
// })
// .WithNotParsed(errs =>
// {
//     foreach (var err in errs)
//     {
//         Console.WriteLine(err.ToString());
//     }
// });

Console.WriteLine("Done");

async Task Crypt(Options opts)
{
    CryptoFs.CryptoFs cryptoFs = new CryptoFs.CryptoFs(1 << 20);
    using var keyFileStream = File.OpenRead(opts.KeyPath);
    // using var keyBinaryReader = new BinaryReader(keyFileStream);
    // byte[] key = keyBinaryReader.ReadBytes(keyFileStream.Length);
    byte[] key = new byte[keyFileStream.Length];
    await keyFileStream.ReadAsync(key, 0, key.Length);
    if (File.Exists(opts.InputFileOrFolderPath))
    {
        await cryptoFs.CryptFileAsync(
            opts.InputFileOrFolderPath,
            opts.TempFolderPath,
            opts.OutputFolderPath,
            key,
            opts.IsEncrypting,
            opts.IsDeleteFilesAfterCrypting);
    }
    else
    {
        await cryptoFs.CryptFilesInFolderRecursiveAsync(
            opts.InputFileOrFolderPath,
            opts.TempFolderPath,
            opts.OutputFolderPath,
            key,
            opts.IsEncrypting,
            opts.IsDeleteFilesAfterCrypting);
    }
}
