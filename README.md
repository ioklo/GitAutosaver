# GitAutosaver
 - commit working tree contents into `autosave/current_branch_name` branch.
 - you can run this program periodically using TaskScheduler(Windows), crontab, etc.

## Usage
```
GitAutosaver [git repository path]
ex) GitAutosaver MyRepo
    GitAutosaver Z:\Proj\MyRepo
```

## Caution
- GitAutosaver clones additional repository into `%LOCALAPPDATA%\GitAutosaver\IntermediateRepos` (`~/.local/GitAutosaver/IntermediateRepos`, on linux)
- Mirroring from repository to intermediateRepo is not tested aggressively yet, so you should change `%LOCALAPPDATA%\GitAutosaver\appsettings.json` very carefully.
- It involves deleting folder operations recurively.
