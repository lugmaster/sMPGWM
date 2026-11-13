Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$restoreTag = 'Csproj_Restore_v1_0'
$repositoryPath = Split-Path $PSScriptRoot -Parent
$projectPath = 'sMPGWM.Game/sMPGWM.csproj'

$tagCommit = git -C $repositoryPath rev-parse --verify "refs/tags/${restoreTag}^{commit}"
if ($LASTEXITCODE -ne 0) { throw "Create tag '$restoreTag' at the clean project-file commit before running this script." }
$tagCommit = $tagCommit.Trim()

$fileType = git -C $repositoryPath cat-file -t "${tagCommit}:$projectPath"
if ($LASTEXITCODE -ne 0 -or $fileType -ne 'blob') { throw "Tag '$restoreTag' does not contain the project file." }

$null = git -C $repositoryPath symbolic-ref --quiet HEAD
if ($LASTEXITCODE -ne 0) { throw 'Check out a branch before restoring and committing the project file.' }

foreach ($operation in @('MERGE_HEAD', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'rebase-merge', 'rebase-apply', 'sequencer')) {
    $operationPath = git -C $repositoryPath rev-parse --path-format=absolute --git-path $operation
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect repository state.' }
    if (Test-Path -LiteralPath $operationPath) { throw "Finish the active Git operation before restoring: $operation" }
}

git -C $repositoryPath restore "--source=$tagCommit" --staged --worktree -- $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Could not restore the project file.' }

git -C $repositoryPath diff --cached --quiet HEAD -- $projectPath
$diffExitCode = $LASTEXITCODE
if ($diffExitCode -eq 0) {
    Write-Host "Restored $projectPath from $restoreTag. It already matches HEAD; no commit needed."
    return
}
if ($diffExitCode -ne 1) { throw 'Could not compare the restored project file with HEAD.' }

# Commit only this file, leaving unrelated staged changes out of the commit.
git -C $repositoryPath commit --only -m $restoreTag -- $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Commit failed. The restored project file remains available for inspection.' }

Write-Host "Restored and committed $projectPath with message '$restoreTag'."
