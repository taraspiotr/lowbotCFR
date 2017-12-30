using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace lowbotCFR
{
    class Program
    {
        class Options
        {
            [Option('r', "read", Required = false,
              HelpText = "Input file to be processed.")]
            public string InputFile { get; set; }

            [Option('v', "verbose", DefaultValue = false,
              HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }
             
            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        static void Main(string[] args)
        {

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Stopwatch StopWatch = new Stopwatch();
                StopWatch.Start();

                //string Path = @"E:\Lowbot\strategy.xml";
                //DrawTrainer T = new DrawTrainer(200000000, 6, Path);
                //T.main();

                StopWatch.Stop();
                Console.WriteLine("Runtime = {0}", DrawTrainer.GetTime(StopWatch.ElapsedMilliseconds / 1000));
                Console.ReadKey();
            }
        }
    }
}
