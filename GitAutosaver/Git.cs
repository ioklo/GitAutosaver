using System.Diagnostics;

namespace GitAutosaver
{
    class Git : IGit
    {
        public void Clone(string srcRepoPath, string destRepoPath)
        {
            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.FileName = "git";
            
            psi.ArgumentList.Add("clone");
            psi.ArgumentList.Add(srcRepoPath);
            psi.ArgumentList.Add(destRepoPath); // 

            var process = Process.Start(psi);

            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new GitCloneException();
        }

        public IGitRepo OpenGitRepo(string repoPath)
        {
            return new GitRepo(repoPath);
        }
    }
}
