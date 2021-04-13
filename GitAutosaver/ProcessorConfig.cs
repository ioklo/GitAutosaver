using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitAutosaver
{
    // 직접 파일에 씁니다
    class ProcessorConfig : IProcessorConfig
    {
        class Data
        {
            public Dictionary<string, string> IntermediateRepos { get; set; }

            public Data()
            {
                IntermediateRepos = new Dictionary<string, string>();
            }
        }

        // persistent
        Data data;

        public static async ValueTask<ProcessorConfig> CreateAsync()
        {
            // local appdata folder
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrEmpty(localAppData))
                return new ProcessorConfig(new Data());

            var configPath = Path.Combine(localAppData, "GitAutosaver", "appsettings.json");

            Data data;
            if (File.Exists(configPath))
            {
                using (var file = File.OpenRead(configPath))
                {
                    data = await JsonSerializer.DeserializeAsync<Data>(file);
                }
            }
            else
            {
                data = new Data();
            }

            return new ProcessorConfig(data);
        }
        
        ProcessorConfig(Data data)
        {
            this.data = data;
        }

        public void AddIntermediateRepo(string srcRepoPath, string intermediateRepoPath)
        {
            data.IntermediateRepos[srcRepoPath] = intermediateRepoPath;
            Save();
        }
        
        void Save()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var configDirPath = Path.Combine(localAppData, "GitAutosaver");
            var configFilePath = Path.Combine(configDirPath, "appsettings.json");

            if (!Directory.Exists(configDirPath))
                Directory.CreateDirectory(configDirPath);

            using (var file = File.OpenWrite(configFilePath))
            {
                var options = new JsonSerializerOptions();
                options.WriteIndented = true;

                JsonSerializer.SerializeAsync<Data>(file, data, options).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public string GetBaseIntermediateRepoPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var intermediateRepos = Path.Combine(localAppData, "GitAutosaver", "IntermediateRepos");

            return intermediateRepos;
        }

        public string? GetIntermediateRepoPath(string srcRepoPath)
        {
            return data.IntermediateRepos.GetValueOrDefault(srcRepoPath);
        }
    }
}
