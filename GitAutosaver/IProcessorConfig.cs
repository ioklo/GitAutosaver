namespace GitAutosaver
{
    interface IProcessorConfig
    {
        void AddIntermediateRepo(string srcRepoPath, string intermediateRepoPath);
        string? GetIntermediateRepoPath(string srcRepoPath);
        string GetBaseIntermediateRepoPath();
    }    
}
