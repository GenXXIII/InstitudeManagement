param(
    [string]$Server = "127.0.0.1,1433",
    [string]$Database = "INK_Manangement",
    [string]$User = "sa",
    [string]$Password = "1234"
)

$ErrorActionPreference = "Stop"

function Invoke-Table([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = $sql
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $table = [System.Data.DataTable]::new()
    $null = $adapter.Fill($table)
    $adapter.Dispose()
    $command.Dispose()
    return $table
}

function Invoke-Statement([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [string]$sql, [hashtable]$parameters) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = $sql
    foreach ($entry in $parameters.GetEnumerator()) {
        $value = if ($null -eq $entry.Value) { [DBNull]::Value } else { $entry.Value }
        $null = $command.Parameters.AddWithValue("@$($entry.Key)", $value)
    }
    $null = $command.ExecuteNonQuery()
    $command.Dispose()
}

function Get-CodeNumber([string]$code) {
    if ($code -match "(\d+)$") { return [int]$Matches[1] }
    return [int]::MaxValue
}

$weekdayPeriods = @(
    @{ Start = [TimeSpan]::Parse("07:30:00"); End = [TimeSpan]::Parse("09:00:00") },
    @{ Start = [TimeSpan]::Parse("09:15:00"); End = [TimeSpan]::Parse("10:45:00") },
    @{ Start = [TimeSpan]::Parse("11:00:00"); End = [TimeSpan]::Parse("12:30:00") },
    @{ Start = [TimeSpan]::Parse("14:00:00"); End = [TimeSpan]::Parse("15:30:00") },
    @{ Start = [TimeSpan]::Parse("15:30:00"); End = [TimeSpan]::Parse("17:00:00") },
    @{ Start = [TimeSpan]::Parse("17:30:00"); End = [TimeSpan]::Parse("19:00:00") },
    @{ Start = [TimeSpan]::Parse("19:00:00"); End = [TimeSpan]::Parse("20:30:00") }
)
$weekendPeriods = @(
    @{ Start = [TimeSpan]::Parse("07:00:00"); End = [TimeSpan]::Parse("08:30:00") },
    @{ Start = [TimeSpan]::Parse("08:40:00"); End = [TimeSpan]::Parse("10:10:00") },
    @{ Start = [TimeSpan]::Parse("11:40:00"); End = [TimeSpan]::Parse("13:10:00") },
    @{ Start = [TimeSpan]::Parse("14:00:00"); End = [TimeSpan]::Parse("15:30:00") },
    @{ Start = [TimeSpan]::Parse("15:40:00"); End = [TimeSpan]::Parse("17:10:00") }
)

$connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Encrypt=True"
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
$transaction = $connection.BeginTransaction()

try {
    $courseRows = Invoke-Table $connection $transaction @"
SELECT c.[Id], c.[CourseCode], c.[TeacherId], MIN(s.[YearLevel]) AS [YearLevel]
FROM [Courses] c
INNER JOIN [ScheduleEntries] s ON s.[CourseId] = c.[Id]
WHERE c.[IsActive] = 1
GROUP BY c.[Id], c.[CourseCode], c.[TeacherId];
"@
    $courses = @($courseRows | ForEach-Object {
        [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.CourseCode; TeacherId = [Guid]$_.TeacherId; Year = [int]$_.YearLevel }
    } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
    $coursesByYear = @{}
    foreach ($year in 1..4) {
        $coursesByYear[$year] = @($courses | Where-Object Year -eq $year)
        if ($coursesByYear[$year].Count -ne 10) { throw "Expected 10 current courses for Year $year, found $($coursesByYear[$year].Count)." }
    }

    $roomRows = Invoke-Table $connection $transaction "SELECT [Id], [ClassroomCode] FROM [Classrooms] WHERE [Status] <> 'Inactive';"
    $rooms = @($roomRows | ForEach-Object { [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.ClassroomCode } } | Sort-Object Code)
    if ($rooms.Count -ne 13) { throw "Expected 13 active learning spaces, found $($rooms.Count)." }

    $periods = @()
    foreach ($day in 1..5) { foreach ($period in $weekdayPeriods) { $periods += [pscustomobject]@{ Day = $day; Period = $period } } }
    foreach ($day in @(6, 0)) { foreach ($period in $weekendPeriods) { $periods += [pscustomobject]@{ Day = $day; Period = $period } } }

    $desired = @()
    for ($slotIndex = 0; $slotIndex -lt $periods.Count; $slotIndex++) {
        $slot = $periods[$slotIndex]
        $usedRoomIds = [System.Collections.Generic.HashSet[Guid]]::new()
        foreach ($year in 1..4) {
            $yearCourses = $coursesByYear[$year]
            $course = $yearCourses[$slotIndex % $yearCourses.Count]
            $eligibleRooms = if ($year -eq 1) { @($rooms) } else { @($rooms | Where-Object Code -ne "501") }
            $preferredRoomIndex = (($slotIndex * 4) + ($year - 1)) % $eligibleRooms.Count
            $room = 0..($eligibleRooms.Count - 1) | ForEach-Object { $eligibleRooms[($preferredRoomIndex + $_) % $eligibleRooms.Count] } | Where-Object { -not $usedRoomIds.Contains($_.Id) } | Select-Object -First 1
            if ($null -eq $room) { throw "No room is available for Year $year in timetable slot $slotIndex." }
            $null = $usedRoomIds.Add($room.Id)
            $desired += [pscustomobject]@{
                CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $room.Id; Year = $year
                Day = $slot.Day; Start = $slot.Period.Start; End = $slot.Period.End
            }
        }
    }
    if ($desired.Count -ne 180) { throw "Expected 180 desired entries, found $($desired.Count)." }

    $entryRows = Invoke-Table $connection $transaction "SELECT [Id], [TimetableCode] FROM [ScheduleEntries] WHERE [Status] <> 'Cancelled';"
    $entries = @($entryRows | ForEach-Object { [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.TimetableCode } } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
    $allCodeRows = Invoke-Table $connection $transaction "SELECT [TimetableCode] FROM [ScheduleEntries];"
    $usedCodes = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $allCodeRows) { $null = $usedCodes.Add([string]$row.TimetableCode) }

    $now = [DateTime]::UtcNow
    for ($index = 0; $index -lt $desired.Count; $index++) {
        $item = $desired[$index]
        if ($index -lt $entries.Count) {
            $entry = $entries[$index]
            Invoke-Statement $connection $transaction @"
UPDATE [ScheduleEntries]
SET [CourseId]=@CourseId, [TeacherId]=@TeacherId, [ClassroomId]=@ClassroomId, [YearLevel]=@Year,
    [DayOfWeek]=@Day, [StartsAt]=@Start, [EndsAt]=@End, [Status]='Upcoming', [UpdatedAtUtc]=@Now
WHERE [Id]=@Id;
"@ @{ Id=$entry.Id; CourseId=$item.CourseId; TeacherId=$item.TeacherId; ClassroomId=$item.ClassroomId; Year=$item.Year; Day=$item.Day; Start=$item.Start; End=$item.End; Now=$now }
        }
        else {
            $number = $index + 1
            $code = "TIM-$number"
            while ($usedCodes.Contains($code)) { $number++; $code = "TIM-$number" }
            $null = $usedCodes.Add($code)
            Invoke-Statement $connection $transaction @"
INSERT INTO [ScheduleEntries] ([Id],[TimetableCode],[CourseId],[ClassroomId],[TeacherId],[YearLevel],[DayOfWeek],[StartsAt],[EndsAt],[Status],[CreatedAtUtc],[UpdatedAtUtc])
VALUES (@Id,@Code,@CourseId,@ClassroomId,@TeacherId,@Year,@Day,@Start,@End,'Upcoming',@Now,@Now);
"@ @{ Id=[Guid]::NewGuid(); Code=$code; CourseId=$item.CourseId; TeacherId=$item.TeacherId; ClassroomId=$item.ClassroomId; Year=$item.Year; Day=$item.Day; Start=$item.Start; End=$item.End; Now=$now }
        }
    }

    for ($index = $desired.Count; $index -lt $entries.Count; $index++) {
        Invoke-Statement $connection $transaction "UPDATE [ScheduleEntries] SET [Status]='Cancelled', [UpdatedAtUtc]=@Now WHERE [Id]=@Id;" @{ Id=$entries[$index].Id; Now=$now }
    }

    $transaction.Commit()
    [pscustomobject]@{
        ActiveEntries = $desired.Count
        Periods = $periods.Count
        ClassesPerPeriod = 4
        YearsPerPeriod = "1,2,3,4"
        PreservedExistingIds = [Math]::Min($entries.Count, $desired.Count)
        AddedEntries = [Math]::Max(0, $desired.Count - $entries.Count)
        CancelledExtraEntries = [Math]::Max(0, $entries.Count - $desired.Count)
    }
}
catch {
    try { $transaction.Rollback() } catch {}
    throw
}
finally {
    $transaction.Dispose()
    $connection.Dispose()
}
