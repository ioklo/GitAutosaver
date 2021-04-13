namespace GitAutosaver
{
    interface IGit
    {
        // throws GitCloneException
        void Clone(string srcRepoPath, string destRepoPath);
        IGitRepo OpenGitRepo(string repoPath);
    }
}
