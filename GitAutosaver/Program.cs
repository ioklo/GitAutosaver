using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace GitAutosaver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("GitAutosaver [repository path]");
                return;
            }

            IProcessorConfig processorConfig = await ProcessorConfig.CreateAsync();
            IGit git = new Git();

            var fullPath = Path.GetFullPath(args[0]);

            // 행동 
            // Setup (미리 해놔야 할 것들) - Save            
            IProcessor processor = new Processor(processorConfig, git, fullPath);
            processor.Process();
        }
    }
}
