// See https://aka.ms/new-console-template for more information
using CommandLine;
using CryptoFs.Cli.Models;

// TEMP
await Crypt(new()
{
    InputFolderPath = "test/input",
    TempFolderPath = "test/temp",
    IsEncrypt = true,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output"
});
await Crypt(new()
{
    InputFolderPath = "test/output",
    TempFolderPath = "test/temp",
    IsEncrypt = false,
    KeyPath = "test/key.txt",
    OutputFolderPath = "test/output2"
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
    CryptoFs.CryptoFs cryptoFs = new CryptoFs.CryptoFs(5);
    using var keyFileStream = File.OpenRead(opts.KeyPath);
    // using var keyBinaryReader = new BinaryReader(keyFileStream);
    // byte[] key = keyBinaryReader.ReadBytes(keyFileStream.Length);
    byte[] key = new byte[keyFileStream.Length];
    await keyFileStream.ReadAsync(key, 0, key.Length);
    await cryptoFs.CryptFilesInFolderRecursiveAsync(
        opts.InputFolderPath, opts.TempFolderPath, opts.OutputFolderPath, key, opts.IsEncrypt);
}
