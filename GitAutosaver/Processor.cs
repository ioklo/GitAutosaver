using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitAutosaver
{
    class Processor : IProcessor
    {
        IProcessorConfig config;
        IGit git;
        string srcRepoPath;

        public Processor(IProcessorConfig config, IGit git, string srcRepoPath)
        {
            this.config = config;
            this.git = git;
            this.srcRepoPath = srcRepoPath;
        }
        
        // throws ProcessException
        public void Process()
        {
            try
            {
                var srcRepo = git.OpenGitRepo(srcRepoPath);
                IGitRepo intermediateRepo;

                var intermediateRepoPath = config.GetIntermediateRepoPath(srcRepoPath);

                // 이미 리포지토리가 있으면 더 이상 진행하지 않는다
                if (intermediateRepoPath == null)
                {
                    intermediateRepoPath = MakeNewIntermidateRepoPath(srcRepoPath);

                    // 클론 시켜서 임시저장소를 하나 더 만든다                
                    git.Clone(srcRepoPath, intermediateRepoPath);

                    config.AddIntermediateRepo(srcRepoPath, intermediateRepoPath);
                    intermediateRepo = git.OpenGitRepo(intermediateRepoPath);
                }
                else
                {
                    intermediateRepo = git.OpenGitRepo(intermediateRepoPath);
                }

                // 
                var branchName = srcRepo.GetCurrentBranchName();

                // branchName에 auto자가 붙으면 저장하지 않는다
                if (branchName.StartsWith("autosave/"))
                    throw new ProcessException(ProcessException.Reason.WorkingOnAutosaveBranch);
                
                intermediateRepo.FetchBranch("origin", branchName);

                var autosaveBranchName = $"autosave/{branchName}";
                
                if (intermediateRepo.BranchExists(autosaveBranchName))
                {
                    // git checkout autosave/master
                    intermediateRepo.CheckoutBranch(autosaveBranchName);
                }
                else
                {
                    // git checkout -b autosave/master --track origin/master
                    intermediateRepo.CheckoutNewBranchTrackingOriginBranch(autosaveBranchName, branchName);
                }

                intermediateRepo.MergeOurs("origin", branchName);
                intermediateRepo.ReadTree("origin", branchName);
                
                MirrorWorkingTree(srcRepo, srcRepoPath, intermediateRepoPath, true);

                intermediateRepo.AddAll();

                // DateTime.
                intermediateRepo.Commit($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {branchName}");
                intermediateRepo.PushBranch("origin", autosaveBranchName);
            }
            catch (GitCloneException)
            {
                throw new ProcessException(ProcessException.Reason.CloneFailed);
            }
            catch
            {
                throw new UnreachableCodeException();
            }
        }

        void MirrorWorkingTreeFiles(IGitRepo srcRepo, string srcDirPath, string destDirPath)
        {
            if (!Path.IsPathFullyQualified(srcDirPath) ||
                !Path.IsPathFullyQualified(destDirPath))
                throw new InvalidOperationException();

            if (destDirPath == "" || destDirPath == "\\" || destDirPath == "/" || Regex.Match(destDirPath, @"^\w+:\\$").Success)
                throw new InvalidOperationException();

            // 나중에 priority queue 사용을 고려해보는 것으로
            var srcFilePaths = Directory.GetFiles(srcDirPath);
            var destFilePaths = Directory.EnumerateFiles(destDirPath)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

            // 1. srcFilePath가 ignored라면 스킵
            var ignoredSrcFilePaths = srcRepo.GetIgnores(srcFilePaths);

            foreach (var srcFilePath in srcFilePaths)
            {
                if (ignoredSrcFilePaths.Contains(srcFilePath)) continue;

                var srcFileRelPath = Path.GetRelativePath(srcDirPath, srcFilePath);
                var destFilePath = Path.Combine(destDirPath, srcFileRelPath);

                // 2. destFile이 있고, 파일 사이즈랑 마지막으로 쓴 시간이 같으면 스킵
                if (destFilePaths.Contains(destFilePath))
                {
                    var srcFileInfo = new FileInfo(srcFilePath);
                    var destFileInfo = new FileInfo(destFilePath);

                    // 크기와 날짜가 같으면 스킵
                    if (srcFileInfo.Length == destFileInfo.Length && srcFileInfo.LastWriteTime == destFileInfo.LastWriteTime)
                    {
                        destFilePaths.Remove(destFilePath);
                        continue;
                    }
                }

                // 아니라면 덮어씌우기
                File.Copy(srcFilePath, destFilePath, true);

                // destFilePaths에서 삭제
                destFilePaths.Remove(destFilePath);
            }

            // 남은 파일은 src에 없다는 것이므로 삭제, 주의
            foreach (var destFilePath in destFilePaths)
            {
                File.Delete(destFilePath);
                Console.WriteLine("{0}: deleted", destFilePath);
            }
        }

        void MirrorWorkingTreeDirectories(IGitRepo srcRepo, string srcDirPath, string destDirPath, bool bTopLevel)
        {
            if (!Path.IsPathFullyQualified(srcDirPath) ||
                !Path.IsPathFullyQualified(destDirPath))
                throw new InvalidOperationException();

            if (destDirPath == "" || destDirPath == "\\" || destDirPath == "/" || Regex.Match(destDirPath, @"^\w+:\\$").Success)
                throw new InvalidOperationException();

            var srcSubDirPaths = Directory.GetDirectories(srcDirPath);
            var destSubDirPaths = Directory.EnumerateDirectories(destDirPath).ToHashSet(StringComparer.CurrentCultureIgnoreCase);

            var ignoreSrcSubDirPaths = srcRepo.GetIgnores(srcSubDirPaths);

            // 각 디렉토리에서 
            foreach(var srcSubDirPath in srcSubDirPaths)
            {
                if (ignoreSrcSubDirPaths.Contains(srcSubDirPath))
                    continue;

                var srcSubDirRelPath = Path.GetRelativePath(srcDirPath, srcSubDirPath);
                var destSubDirPath = Path.Combine(destDirPath, srcSubDirRelPath);

                // 중요;
                if (bTopLevel && srcSubDirRelPath.Equals(".git", StringComparison.CurrentCultureIgnoreCase))
                {
                    destSubDirPaths.Remove(destSubDirPath);
                    continue;
                }
                
                // 디렉토리가 존재한다면
                if (destSubDirPaths.Contains(destSubDirPath))
                {
                    MirrorWorkingTree(srcRepo, srcSubDirPath, destSubDirPath, false);
                    destSubDirPaths.Remove(destSubDirPath);
                }
                else
                {                    
                    Directory.CreateDirectory(destSubDirPath);
                    MirrorWorkingTree(srcRepo, srcSubDirPath, destSubDirPath, false);
                    destSubDirPaths.Remove(destSubDirPath);
                }
            }

            // src에 존재하지 않거나 ignored된 디렉토리는 삭제
            foreach (var destSubDirPath in destSubDirPaths)
            {
                Directory.Delete(destSubDirPath, true);
                Console.WriteLine("{0}: deleted", destSubDirPath);
            }
        }

        void MirrorWorkingTree(IGitRepo srcRepo, string srcDirPath, string destDirPath, bool bTopLevel)
        {
            if (!Path.IsPathFullyQualified(srcDirPath) ||
                !Path.IsPathFullyQualified(destDirPath))
                throw new InvalidOperationException();

            if (destDirPath == "" || destDirPath == "\\" || destDirPath == "/" || Regex.Match(destDirPath, @"^\w+:\\$").Success)
                throw new InvalidOperationException();

            MirrorWorkingTreeFiles(srcRepo, srcDirPath, destDirPath);

            // 더 무서운 DirectoryMirror
            MirrorWorkingTreeDirectories(srcRepo, srcDirPath, destDirPath, bTopLevel);
        }

        string MakeNewIntermidateRepoPath(string srcDirPath)
        {
            // srcDir에서 이름만, 
            var srcName = Path.GetFileName(srcDirPath);
            var basePath = config.GetBaseIntermediateRepoPath();

            var baseRepoPath = Path.Combine(basePath, srcName);
            var repoPath = baseRepoPath;
            int count = 1;
            while (Directory.Exists(repoPath) || File.Exists(repoPath))
            {
                repoPath = $"{baseRepoPath}_{count}";
                count++;
            }

            return repoPath;
        }
    }
}
