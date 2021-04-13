using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutosaver
{
    class GitRepo : IGitRepo
    {
        string repoPath;

        public GitRepo(string repoPath)
        {
            this.repoPath = repoPath;
        }

        (string Output, int ExitCode) Run(params string[] args)
        {
            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.FileName = "git";
            psi.WorkingDirectory = repoPath;
            psi.RedirectStandardOutput = true;
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                return (process.StandardOutput.ReadToEnd(), process.ExitCode);
            }
        }

        public string GetCurrentBranchName()
        {
            var result = Run("branch", "--show-current");

            using (var reader = new StringReader(result.Output))
            {
                var line = reader.ReadLine();
                if (line == null)
                    throw new GitRepoException();

                return line;
            }
        }

        public bool BranchExists(string branchName)
        {
            var result = Run("show-ref", "--verify", "--quiet", $"refs/heads/{branchName}");
            return result.ExitCode == 0;
        }

        public void FetchBranch(string remoteName, string branchName)
        {
            Run("fetch", "origin", $"+refs/heads/{branchName}:refs/remotes/{remoteName}/{branchName}");
        }

        public void CheckoutBranch(string branchName)
        {
            Run("checkout", branchName);
        }

        public void CheckoutNewBranchTrackingOriginBranch(string newBranchName, string trackingBranch)
        {
            Run("checkout", "-b", newBranchName, "--track", trackingBranch);
        }

        public void MergeOurs(string remoteName, string branchName)
        {
            // git merge -s ours origin/master --no-ff --no-commit // 일단 ours로 머지 표시
            Run("merge", "-s", "ours", $"refs/remotes/{remoteName}/{branchName}", "--no-ff", "--no-commit");
        }

        public void ReadTree(string remoteName, string branchName)
        {
            // git read-tree -um origin/master
            Run("read-tree", "-um", $"refs/remotes/{remoteName}/{branchName}");
        }

        public void AddAll()
        {
            Run("add", ".");
        }

        public void Commit(string message)
        {
            Run("commit", "-m", message);
        }

        public void PushBranch(string remoteName, string branchName)
        {
            // git push origin refs/heads/autosave/master:refs/heads/autosave/master
            Run("push", remoteName, $"+refs/heads/{branchName}:refs/heads/{branchName}");
        }

        public HashSet<string> GetIgnores(string[] paths)
        {
            if (paths.Length == 0) return new HashSet<string>();

            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.FileName = "git";
            psi.WorkingDirectory = repoPath;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.ArgumentList.Add("check-ignore");
            psi.ArgumentList.Add("--stdin");
            psi.ArgumentList.Add("-z");            

            using (var process = Process.Start(psi))
            {
                var writeTask = Task.Run(() =>
                {
                    foreach (var path in paths)
                    {
                        process.StandardInput.Write(path);
                        process.StandardInput.Write('\0');
                    }

                    process.StandardInput.Close();
                });

                var outputs = process.StandardOutput.ReadToEnd();                
                process.WaitForExit();

                return outputs.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            }
        }
    }
}
