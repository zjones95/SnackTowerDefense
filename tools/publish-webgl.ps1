<#
.SYNOPSIS
    Build the WebGL player locally and publish it to GitHub Pages — with no Unity
    licence in CI.

.DESCRIPTION
    The GitHub Pages site is served straight from the `webgl` branch (Settings →
    Pages → Deploy from a branch: `webgl` / root). This script builds the player
    with the locally-installed Unity, copies build/WebGL onto a fresh single-commit
    `webgl` branch, and force-pushes it. GitHub Pages then republishes, so there is
    never a Unity activation step in Actions.

    Refresh the live site after gameplay/art changes:

        powershell -ExecutionPolicy Bypass -File tools\publish-webgl.ps1

    The Unity editor must be CLOSED (batch mode locks Library/).

.PARAMETER SkipBuild
    Publish the existing build/WebGL folder without rebuilding (fast; use when the
    build is already known-good).

.PARAMETER UnityPath
    Full path to Unity.exe. Defaults to the newest editor under
    C:\Program Files\Unity\Hub\Editor.

.PARAMETER Message
    Commit message for the `webgl` branch. Defaults to the current main commit.

.PARAMETER Branch
    Branch that GitHub Pages serves. Defaults to `webgl`.

.PARAMETER Remote
    Git remote to push to. Defaults to `origin`.
#>
[CmdletBinding()]
param(
    [switch] $SkipBuild,
    [string] $UnityPath = '',
    [string] $Message = '',
    [string] $Branch = 'webgl',
    [string] $Remote = 'origin'
)

$ErrorActionPreference = 'Stop'

# Never let git/credential-manager block on an interactive prompt.
$env:GIT_TERMINAL_PROMPT = '0'
$env:GCM_INTERACTIVE = 'never'

$projectRoot = Split-Path -Parent $PSScriptRoot
$webglDir = Join-Path $projectRoot 'build\WebGL'

# Run git in a directory, surfacing its output but not letting stderr rows
# (progress/advice) become PowerShell errors. Git arguments are always passed as
# an array so tokens like -D are not reinterpreted as PowerShell parameters.
function Invoke-Git {
    param([string] $RepoDir, [string[]] $GitArgs)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & git -C $RepoDir @GitArgs
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed (exit $LASTEXITCODE)"
    }
}

# --- 1. Build (unless -SkipBuild) -------------------------------------------

if (-not $SkipBuild) {
    if (-not $UnityPath) {
        $UnityPath = Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe' `
            -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
        throw "Could not find Unity.exe. Pass -UnityPath <path>."
    }

    $running = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -like "*$projectRoot*" }
    if ($running) {
        throw "The Unity editor is open for this project. Close it first (batch mode locks Library/)."
    }

    $log = Join-Path $env:TEMP 'opencode\webgl-build.log'
    New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
    Write-Host "Building WebGL with $UnityPath" -ForegroundColor Cyan
    Write-Host "  log: $log" -ForegroundColor DarkGray

    & $UnityPath -batchmode -projectPath $projectRoot -executeMethod WebGLBuild.Build -quit -logFile $log
    $ok = (Test-Path $log) -and (Select-String -Path $log -Pattern 'WebGLBuild: BUILD OK' -Quiet)
    if ($LASTEXITCODE -ne 0 -or -not $ok) {
        throw "WebGL build failed (exit $LASTEXITCODE). See $log"
    }
}

if (-not (Test-Path (Join-Path $webglDir 'index.html'))) {
    throw "No WebGL build found at $webglDir. Run without -SkipBuild, or build it first."
}

# --- 2. Publish build/WebGL as a single-commit `webgl` branch ---------------

$source = (Invoke-Git $projectRoot @('log', '-1', '--format=%h %s') | Out-String).Trim()
if (-not $Message) { $Message = "WebGL build from main@$source" }

$worktree = Join-Path $env:TEMP 'opencode\webgl-publish'
if (Test-Path $worktree) { Remove-Item $worktree -Recurse -Force }

# Clear any leftovers from a previously interrupted run.
Invoke-Git $projectRoot @('worktree', 'prune')

Invoke-Git $projectRoot @('worktree', 'add', '--orphan', '-B', $Branch, $worktree)

Copy-Item (Join-Path $webglDir '*') $worktree -Recurse -Force
# Tell GitHub Pages' Jekyll step to serve every file verbatim.
Set-Content -Path (Join-Path $worktree '.nojekyll') -Value '' -NoNewline

Invoke-Git $worktree @('add', '-A')
Invoke-Git $worktree @('commit', '-m', $Message)
Invoke-Git $worktree @('push', '--force', $Remote, "HEAD:refs/heads/$Branch")

Invoke-Git $projectRoot @('worktree', 'remove', '--force', $worktree)
try { Invoke-Git $projectRoot @('branch', '-D', $Branch) } catch { }

Write-Host ""
Write-Host "Published to the '$Branch' branch." -ForegroundColor Green
Write-Host "Live in a minute or two at https://zjones95.github.io/SnackTowerDefense/" -ForegroundColor Green
Write-Host "(GitHub Pages source: Settings -> Pages -> Deploy from a branch -> $Branch / root)"
