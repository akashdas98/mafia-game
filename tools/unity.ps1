param(
    [ValidateSet("version", "open", "refresh", "test-editmode", "test-playmode")]
    [string]$Command = "version",

    [string]$UnityPath = $env:UNITY_EDITOR,

    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectVersionFile = Join-Path $ProjectRoot "ProjectSettings\ProjectVersion.txt"

if (-not $UnityPath) {
    $EditorVersion = ""
    if (Test-Path $ProjectVersionFile) {
        $VersionLine = Get-Content $ProjectVersionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
        if ($VersionLine) {
            $EditorVersion = ($VersionLine -replace "m_EditorVersion:\s*", "").Trim()
        }
    }

    $Candidates = @()
    if ($EditorVersion) {
        $Candidates += "D:\Program Files\Unity\$EditorVersion\Editor\Unity.exe"
        $Candidates += "C:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
        $Candidates += "C:\Program Files\Unity\$EditorVersion\Editor\Unity.exe"
    }

    $UnityPath = $Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    throw "Unity Editor executable was not found. Set UNITY_EDITOR to Unity.exe or pass -UnityPath."
}

$DefaultLog = Join-Path $ProjectRoot "Logs\unity-cli-$Command.log"
if (-not $LogFile) {
    $LogFile = $DefaultLog
}

$CommonArgs = @(
    "-projectPath", $ProjectRoot,
    "-logFile", $LogFile
)

function ConvertTo-UnityArgumentString {
    param([string[]]$ArgumentList)

    return ($ArgumentList | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '"', '\"') + '"'
        }
        else {
            $_
        }
    }) -join " "
}

function Invoke-UnityAndWait {
    param([string[]]$ArgumentList)

    $ArgumentString = ConvertTo-UnityArgumentString -ArgumentList $ArgumentList
    $Process = Start-Process -FilePath $UnityPath -ArgumentList $ArgumentString -Wait -PassThru
    exit $Process.ExitCode
}

switch ($Command) {
    "version" {
        Invoke-UnityAndWait @("-version")
    }
    "open" {
        $ArgumentString = ConvertTo-UnityArgumentString -ArgumentList @("-projectPath", $ProjectRoot)
        Start-Process -FilePath $UnityPath -ArgumentList $ArgumentString
        exit 0
    }
    "refresh" {
        Invoke-UnityAndWait @($CommonArgs + @("-batchmode", "-nographics", "-quit"))
    }
    "test-editmode" {
        $ResultsFile = Join-Path $ProjectRoot "Logs\editmode-results.xml"
        Invoke-UnityAndWait @($CommonArgs + @("-batchmode", "-nographics", "-runTests", "-testPlatform", "EditMode", "-testResults", $ResultsFile))
    }
    "test-playmode" {
        $ResultsFile = Join-Path $ProjectRoot "Logs\playmode-results.xml"
        Invoke-UnityAndWait @($CommonArgs + @("-batchmode", "-nographics", "-runTests", "-testPlatform", "PlayMode", "-testResults", $ResultsFile))
    }
}
