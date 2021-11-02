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
        [Option('o', "output", Required = true, HelpText = "Path to output folder")]
        public string OutputPath { get; set; }
        [Option('k', "key", Required = true, HelpText = "Path to the key file")]
        public string KeyPath { get; set; }
        [Option('e', "encrypt", Required = true, HelpText = "Is encrypted?")]
        public bool IsEncrypt { get; set; }

    }
}
