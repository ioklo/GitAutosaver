using System.Collections.Generic;

namespace GitAutosaver
{
    interface IGitRepo
    {
        string GetCurrentBranchName();
        void FetchBranch(string remoteName, string branchName);
        bool BranchExists(string branchName);
        void CheckoutBranch(string branchName);
        void CheckoutNewBranchTrackingOriginBranch(string newBranchName, string trackingBranchName);
        void MergeOurs(string remoteName, string branchName);
        void ReadTree(string remoteName, string branchName);
        void AddAll();
        void Commit(string message);
        void PushBranch(string remoteName, string branchName);
        HashSet<string> GetIgnores(string[] paths);
    }
}
