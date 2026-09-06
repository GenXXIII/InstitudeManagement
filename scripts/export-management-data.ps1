param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5080",
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\exports\ManagementData.json")
)

$ErrorActionPreference = "Stop"
$resources = @("departments", "teachers", "students", "courses", "classrooms", "timetable", "attendance", "grades")
$data = [ordered]@{}
$counts = [ordered]@{}

foreach ($resource in $resources) {
    $rows = Invoke-RestMethod -Uri "$ApiBaseUrl/api/catalog/$resource"
    $data[$resource] = $rows
    $counts[$resource] = @($rows).Length
}

$expected = [ordered]@{
    departments = 5
    teachers = 40
    students = 800
    courses = 40
    classrooms = 13
    timetable = 295
    attendance = 800
    grades = 800
}
foreach ($resource in $expected.Keys) {
    if ($counts[$resource] -ne $expected[$resource]) {
        throw "Expected $($expected[$resource]) $resource records but the API returned $($counts[$resource])."
    }
}

$target = [IO.Path]::GetFullPath($OutputPath)
$directory = Split-Path -Parent $target
$null = New-Item -ItemType Directory -Path $directory -Force

[ordered]@{
    exportedAtUtc = [DateTime]::UtcNow.ToString("O")
    source = $ApiBaseUrl
    database = "INK_Manangement"
    note = "Database IDs are included for reference only. Use scripts/import-institute-data.ps1 on another device so relationships receive valid IDs for that database."
    counts = $counts
    data = $data
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $target -Encoding utf8

Get-Item -LiteralPath $target | Select-Object FullName, Length, LastWriteTime
