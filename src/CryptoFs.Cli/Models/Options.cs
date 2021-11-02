using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace CryptoFs.Cli.Models
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Path to input folder")]
        public string InputFolderPath { get; set; }
        [Option('t', "temp", Required = true, HelpText = "Path to temp folder")]
        public string TempFolderPath { get; set; }
        [Option('o', "output", Required = true, HelpText = "Path to output folder")]
        public string OutputFolderPath { get; set; }
        [Option('k', "key", Required = true, HelpText = "Path to the key file")]
        public string KeyPath { get; set; }
        [Option('e', "encrypt", Required = true, HelpText = "Is encrypting?")]
        public bool IsEncrypting { get; set; }
        [Option('d', "delete", Required = false, Default = false, HelpText = "Is deleting the input files after crypting?")]
        public bool IsDeleteFilesAfterCrypting { get; set; }
    }
}
