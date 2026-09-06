param(
    [string]$Server = "127.0.0.1,1433",
    [string]$Database = "INK_Manangement",
    [string]$User = "sa",
    [string]$Password = "1234",
    [ValidateSet(1, 2)]
    [int]$Semester = 1
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

$shifts = @(
    [pscustomobject]@{ Name = "Morning"; Days = @(1, 2, 3, 4, 5); StartsAt = [TimeSpan]::Parse("07:30:00"); EndsAt = [TimeSpan]::Parse("10:30:00"); Periods = @(
        [pscustomobject]@{ Session = "Morning"; Start = [TimeSpan]::Parse("07:30:00"); End = [TimeSpan]::Parse("09:00:00") },
        [pscustomobject]@{ Session = "Morning"; Start = [TimeSpan]::Parse("09:00:00"); End = [TimeSpan]::Parse("10:30:00") }
    ) },
    [pscustomobject]@{ Name = "Afternoon"; Days = @(1, 2, 3, 4, 5); StartsAt = [TimeSpan]::Parse("14:00:00"); EndsAt = [TimeSpan]::Parse("17:00:00"); Periods = @(
        [pscustomobject]@{ Session = "Afternoon"; Start = [TimeSpan]::Parse("14:00:00"); End = [TimeSpan]::Parse("15:30:00") },
        [pscustomobject]@{ Session = "Afternoon"; Start = [TimeSpan]::Parse("15:30:00"); End = [TimeSpan]::Parse("17:00:00") }
    ) },
    [pscustomobject]@{ Name = "Evening"; Days = @(1, 2, 3, 4, 5); StartsAt = [TimeSpan]::Parse("17:30:00"); EndsAt = [TimeSpan]::Parse("20:30:00"); Periods = @(
        [pscustomobject]@{ Session = "Evening"; Start = [TimeSpan]::Parse("17:30:00"); End = [TimeSpan]::Parse("19:00:00") },
        [pscustomobject]@{ Session = "Evening"; Start = [TimeSpan]::Parse("19:00:00"); End = [TimeSpan]::Parse("20:30:00") }
    ) },
    [pscustomobject]@{ Name = "Weekend"; Days = @(6, 0); StartsAt = [TimeSpan]::Parse("07:00:00"); EndsAt = [TimeSpan]::Parse("17:10:00"); Periods = @(
        [pscustomobject]@{ Session = "Morning"; Start = [TimeSpan]::Parse("07:00:00"); End = [TimeSpan]::Parse("08:30:00") },
        [pscustomobject]@{ Session = "Morning"; Start = [TimeSpan]::Parse("08:40:00"); End = [TimeSpan]::Parse("10:10:00") },
        [pscustomobject]@{ Session = "Morning"; Start = [TimeSpan]::Parse("11:40:00"); End = [TimeSpan]::Parse("13:10:00") },
        [pscustomobject]@{ Session = "Afternoon"; Start = [TimeSpan]::Parse("14:00:00"); End = [TimeSpan]::Parse("15:30:00") },
        [pscustomobject]@{ Session = "Afternoon"; Start = [TimeSpan]::Parse("15:40:00"); End = [TimeSpan]::Parse("17:10:00") }
    ) }
)
foreach ($shift in $shifts) {
    if ($shift.Periods[0].Start -ne $shift.StartsAt -or $shift.Periods[-1].End -ne $shift.EndsAt) {
        throw "$($shift.Name) periods must cover the configured shift window."
    }
    if (@($shift.Periods | Where-Object { ($_.End - $_.Start).TotalMinutes -ne 90 }).Count -gt 0) {
        throw "$($shift.Name) periods must each last 90 minutes."
    }
    if ($shift.Name -eq "Weekend" -and ($shift.Days.Count -ne 2 -or $shift.Periods.Count -ne 5)) { throw "Weekend must contain five periods on Saturday and Sunday." }
    if ($shift.Name -ne "Weekend" -and ($shift.Days.Count -ne 5 -or $shift.Periods.Count -ne 2 -or $shift.Periods[0].End -ne $shift.Periods[1].Start)) { throw "$($shift.Name) must contain two contiguous periods Monday-Friday." }
}

$connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Encrypt=True"
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
$transaction = $connection.BeginTransaction()

try {
    $courseRows = Invoke-Table $connection $transaction @"
SELECT c.[Id], c.[CourseCode], c.[TeacherId], c.[DepartmentId], d.[DepartmentCode], c.[Capacity],
       TRY_CONVERT(int, JSON_VALUE(metadata.[Details], '$.year')) AS [YearLevel],
       TRY_CONVERT(int, JSON_VALUE(metadata.[Details], '$.semester')) AS [Semester]
FROM [Courses] c
INNER JOIN [Departments] d ON d.[Id] = c.[DepartmentId]
OUTER APPLY
(
    SELECT TOP (1) audit.[Details]
    FROM [AuditLogs] audit
    WHERE audit.[ResourceId] = c.[Id]
      AND audit.[Type] = 'Course'
      AND ISJSON(audit.[Details]) = 1
      AND JSON_VALUE(audit.[Details], '$.year') IS NOT NULL
      AND JSON_VALUE(audit.[Details], '$.semester') IS NOT NULL
    ORDER BY audit.[CreatedAtUtc]
) metadata
WHERE c.[IsActive] = 1
"@
    $courses = @($courseRows | ForEach-Object {
        [pscustomobject]@{
            Id = [Guid]$_.Id; Code = [string]$_.CourseCode; TeacherId = [Guid]$_.TeacherId
            DepartmentId = [Guid]$_.DepartmentId; DepartmentCode = [string]$_.DepartmentCode
            Capacity = [int]$_.Capacity; Year = [int]$_.YearLevel; Semester = [int]$_.Semester
        }
    } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
    if ($courses.Count -ne 40) { throw "Expected 40 current active courses, found $($courses.Count)." }
    $invalidCourseMetadata = @($courses | Where-Object { $_.Year -notin 1..4 -or $_.Semester -notin 1..2 })
    if ($invalidCourseMetadata.Count -gt 0) {
        $details = ($invalidCourseMetadata | ForEach-Object { "$($_.Code)=Year $($_.Year), Semester $($_.Semester)" }) -join ", "
        throw "Every current course needs imported Year 1-4 and Semester 1-2 metadata. Invalid courses: $details"
    }
    $invalidCourseCapacities = @($courses | Where-Object { $_.Capacity -lt 1 -or $_.Capacity -gt 10000 })
    if ($invalidCourseCapacities.Count -gt 0) {
        $details = ($invalidCourseCapacities | ForEach-Object { "$($_.Code)=$($_.Capacity)" }) -join ", "
        throw "Every current course must have a capacity from 1 to 10000. Invalid courses: $details"
    }

    $departments = @($courses | Group-Object DepartmentId | ForEach-Object { $_.Group[0] } | Sort-Object @{ Expression = { Get-CodeNumber $_.DepartmentCode } })
    if ($departments.Count -ne 5) { throw "Expected the 40 courses to cover 5 departments, found $($departments.Count)." }
    $activeCourses = @($courses | Where-Object Semester -eq $Semester)
    if ($activeCourses.Count -ne 20) { throw "Expected 20 current Semester $Semester courses, found $($activeCourses.Count)." }
    $coursesByYear = @{}
    foreach ($year in 1..4) {
        $yearCourses = @($activeCourses | Where-Object Year -eq $year)
        if ($yearCourses.Count -ne 5) { throw "Expected 5 Semester $Semester courses for Year $year, found $($yearCourses.Count)." }
        $departmentCourses = @{}
        foreach ($department in $departments) {
            $items = @($yearCourses | Where-Object DepartmentId -eq $department.DepartmentId | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
            if ($items.Count -ne 1) { throw "Expected 1 Semester $Semester course for $($department.DepartmentCode) Year $year, found $($items.Count)." }
            $departmentCourses[$department.DepartmentId.ToString()] = $items[0]
        }
        $interleaved = @()
        foreach ($department in $departments) { $interleaved += $departmentCourses[$department.DepartmentId.ToString()] }
        if (@($interleaved.DepartmentId | Sort-Object -Unique).Count -ne 5) { throw "Year $year must cover all 5 departments in Semester $Semester." }
        $coursesByYear[$year] = $interleaved
    }

    $studentRows = Invoke-Table $connection $transaction "SELECT [Id], [StudentCode], [DepartmentId], [YearLevel], [Shift] FROM [Students] WHERE [Status] <> 'Inactive';"
    $students = @($studentRows | ForEach-Object {
        [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.StudentCode; DepartmentId = [Guid]$_.DepartmentId; Year = [int]$_.YearLevel; CurrentShift = [string]$_.Shift }
    })
    if ($students.Count -ne 800) { throw "Expected 800 current active students, found $($students.Count)." }
    $studentShifts = @("Morning", "Afternoon", "Evening", "Weekend")
    $studentShiftUpdates = @()
    foreach ($department in $departments) {
        foreach ($year in 1..4) {
            $cohort = @($students | Where-Object { $_.DepartmentId -eq $department.DepartmentId -and $_.Year -eq $year } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
            if ($cohort.Count -ne 40) { throw "Expected 40 current students for $($department.DepartmentCode) Year $year, found $($cohort.Count)." }
            for ($studentIndex = 0; $studentIndex -lt $cohort.Count; $studentIndex++) {
                $studentShiftUpdates += [pscustomobject]@{
                    Id = $cohort[$studentIndex].Id
                    DepartmentId = $cohort[$studentIndex].DepartmentId
                    Year = $cohort[$studentIndex].Year
                    CurrentShift = $cohort[$studentIndex].CurrentShift
                    DesiredShift = $studentShifts[[Math]::Floor($studentIndex / 10)]
                }
            }
        }
    }
    if ($studentShiftUpdates.Count -ne 800) { throw "Expected 800 deterministic student shift assignments, found $($studentShiftUpdates.Count)." }
    foreach ($department in $departments) {
        foreach ($year in 1..4) {
            $cohortIds = @($students | Where-Object { $_.DepartmentId -eq $department.DepartmentId -and $_.Year -eq $year }).Id
            foreach ($shift in $studentShifts) {
                $shiftCount = @($studentShiftUpdates | Where-Object { $_.Id -in $cohortIds -and $_.DesiredShift -eq $shift }).Count
                if ($shiftCount -ne 10) { throw "Expected 10 current students for $($department.DepartmentCode) Year $year $shift, found $shiftCount." }
            }
        }
    }

    $roomRows = Invoke-Table $connection $transaction "SELECT [Id], [ClassroomCode], [Capacity] FROM [Classrooms] WHERE [Status] <> 'Inactive';"
    $rooms = @($roomRows | ForEach-Object { [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.ClassroomCode; Capacity = [int]$_.Capacity } } | Sort-Object Code)
    $maximumCourseCapacity = [int](($activeCourses | Measure-Object Capacity -Maximum).Maximum)
    $minimumCourseCapacity = [int](($activeCourses | Measure-Object Capacity -Minimum).Minimum)
    $capacitySafeRooms = @($rooms | Where-Object Capacity -ge $maximumCourseCapacity)

    $courseOrder = @()
    foreach ($coursePosition in 0..4) {
        foreach ($year in 1..4) { $courseOrder += $coursesByYear[$year][$coursePosition] }
    }
    if ($courseOrder.Count -ne 20 -or @($courseOrder.Id | Sort-Object -Unique).Count -ne 20) {
        throw "Semester $Semester must provide 20 distinct courses across Years 1-4."
    }
    $yearOneCourses = @($coursesByYear[1])
    $olderCourseOrder = @()
    foreach ($coursePosition in 0..4) {
        foreach ($year in 2..4) { $olderCourseOrder += $coursesByYear[$year][$coursePosition] }
    }

    $desired = @()
    foreach ($shift in $shifts) {
        for ($dayIndex = 0; $dayIndex -lt $shift.Days.Count; $dayIndex++) {
            $day = $shift.Days[$dayIndex]
            if ($shift.Name -eq "Weekend") {
                for ($coursePosition = 0; $coursePosition -lt 5; $coursePosition++) {
                    $period = $shift.Periods[$coursePosition]
                    foreach ($year in 1..4) {
                        $course = $coursesByYear[$year][$coursePosition]
                        $desired += [pscustomobject]@{
                            CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = $course.Year
                            DepartmentId = $course.DepartmentId; Shift = $shift.Name; PeriodLabel = $period.Session
                            Day = $day; Start = $period.Start; End = $period.End
                        }
                    }
                }
                continue
            }
            for ($courseIndex = 0; $courseIndex -lt $olderCourseOrder.Count; $courseIndex++) {
                $course = $olderCourseOrder[$courseIndex]
                $periodIndex = if ($courseIndex -lt 8) { 0 } else { 1 }
                $period = $shift.Periods[$periodIndex]
                $desired += [pscustomobject]@{
                    CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = $course.Year
                    DepartmentId = $course.DepartmentId; Shift = $shift.Name; PeriodLabel = $period.Session
                    Day = $day; Start = $period.Start; End = $period.End
                }
            }
            foreach ($periodIndex in 0..1) {
                $course = $yearOneCourses[(($dayIndex * 2) + $periodIndex) % $yearOneCourses.Count]
                $period = $shift.Periods[$periodIndex]
                $desired += [pscustomobject]@{
                    CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = 1
                    DepartmentId = $course.DepartmentId; Shift = $shift.Name; PeriodLabel = $period.Session
                    Day = $day; Start = $period.Start; End = $period.End
                }
            }
        }
    }

    $periodGroupIndex = 0
    foreach ($entryGroup in @($desired | Group-Object Shift, Day, Start, End)) {
        $usedRoomIds = [System.Collections.Generic.HashSet[Guid]]::new()
        foreach ($item in $entryGroup.Group) {
            $course = $activeCourses | Where-Object Id -eq $item.CourseId | Select-Object -First 1
            $eligibleRooms = @($rooms | Where-Object { $_.Capacity -ge $course.Capacity -and (($item.Year -eq 1 -and $_.Code -eq "501") -or ($item.Year -ne 1 -and $_.Code -ne "501")) } | Sort-Object Code)
            if ($eligibleRooms.Count -eq 0) { throw "No classroom has capacity $($course.Capacity) for $($course.Code), Year $($item.Year)." }
            $preferredRoomIndex = ($periodGroupIndex + $item.Year - 1) % $eligibleRooms.Count
            $room = 0..($eligibleRooms.Count - 1) | ForEach-Object { $eligibleRooms[($preferredRoomIndex + $_) % $eligibleRooms.Count] } | Where-Object { -not $usedRoomIds.Contains($_.Id) } | Select-Object -First 1
            if ($null -eq $room) { throw "No room is available for Year $($item.Year) in $($item.Shift), day $($item.Day), $($item.Start)-$($item.End)." }
            $null = $usedRoomIds.Add($room.Id)
            $item.ClassroomId = $room.Id
        }
        $periodGroupIndex++
    }
    if ($desired.Count -ne 295) { throw "Expected 295 desired entries, found $($desired.Count)." }
    foreach ($shift in $shifts) {
        $shiftEntries = @($desired | Where-Object Shift -eq $shift.Name)
        $expectedPerDay = if ($shift.Name -eq "Weekend") { 20 } else { 17 }
        $expectedShiftEntries = $shift.Days.Count * $expectedPerDay
        if ($shiftEntries.Count -ne $expectedShiftEntries -or @($shiftEntries.CourseId | Sort-Object -Unique).Count -ne 20) { throw "$($shift.Name) must cover all 20 Semester $Semester courses across its weekly rotation." }
        foreach ($day in $shift.Days) {
            $shiftDayEntries = @($shiftEntries | Where-Object Day -eq $day)
            if ($shiftDayEntries.Count -ne $expectedPerDay -or @($shiftDayEntries.CourseId | Sort-Object -Unique).Count -ne $expectedPerDay -or @($shiftDayEntries | Group-Object Start, End).Count -ne $shift.Periods.Count) {
                throw "$($shift.Name), day $day must schedule $expectedPerDay distinct current-semester courses."
            }
            $expectedYearOne = if ($shift.Name -eq "Weekend") { 5 } else { 2 }
            if (@($shiftDayEntries | Where-Object Year -eq 1).Count -ne $expectedYearOne) { throw "$($shift.Name), day $day must rotate $expectedYearOne Year 1 courses through Classroom 501." }
            foreach ($year in 2..4) {
                if (@($shiftDayEntries | Where-Object Year -eq $year).Count -ne 5) { throw "$($shift.Name), day $day must schedule 5 Year $year courses." }
            }
            $periodCounts = @($shiftDayEntries | Group-Object Start, End | Select-Object -ExpandProperty Count | Sort-Object)
            $expectedPeriodCounts = if ($shift.Name -eq "Weekend") { @(4, 4, 4, 4, 4) } else { @(8, 9) }
            if (($periodCounts -join ",") -ne ($expectedPeriodCounts -join ",")) {
                throw "$($shift.Name), day $day has an invalid per-period class distribution."
            }
        }
    }
    $exactPeriodStudentCounts = @()
    foreach ($entryGroup in @($desired | Group-Object Day, Start, End)) {
        $weekendPeriod = $entryGroup.Group[0].Shift -eq "Weekend"
        $expectedClasses = if ($weekendPeriod) { 4 } elseif ($entryGroup.Group[0].Start -in @([TimeSpan]::Parse("07:30:00"), [TimeSpan]::Parse("14:00:00"), [TimeSpan]::Parse("17:30:00"))) { 9 } else { 8 }
        if ($entryGroup.Count -ne $expectedClasses -or @($entryGroup.Group.ClassroomId | Sort-Object -Unique).Count -ne $expectedClasses -or @($entryGroup.Group.TeacherId | Sort-Object -Unique).Count -ne $expectedClasses) {
            throw "Every exact period must use distinct teachers and rooms for all $expectedClasses classes."
        }
        $periodShift = $entryGroup.Group[0].Shift
        $periodStudentIds = @($entryGroup.Group | ForEach-Object {
            $entry = $_
            $studentShiftUpdates | Where-Object { $_.DepartmentId -eq $entry.DepartmentId -and $_.Year -eq $entry.Year -and $_.DesiredShift -eq $periodShift } | Select-Object -ExpandProperty Id
        } | Sort-Object -Unique)
        $expectedStudents = $expectedClasses * 10
        if ($periodStudentIds.Count -ne $expectedStudents) { throw "Every exact period must serve $expectedStudents unique students, found $($periodStudentIds.Count)." }
        $exactPeriodStudentCounts += $periodStudentIds.Count
    }
    foreach ($course in $activeCourses) {
        $copies = @($desired | Where-Object CourseId -eq $course.Id)
        $expectedCopies = if ($course.Year -eq 1) { 8 } else { 17 }
        if ($copies.Count -ne $expectedCopies -or @($copies.Shift | Sort-Object -Unique).Count -ne 4) { throw "$($course.Code) must appear $expectedCopies times across all four shifts." }
        $courseStudentIds = @($copies | ForEach-Object {
            $copy = $_
            $studentShiftUpdates | Where-Object { $_.DepartmentId -eq $course.DepartmentId -and $_.Year -eq $course.Year -and $_.DesiredShift -eq $copy.Shift } | Select-Object -ExpandProperty Id
        } | Sort-Object -Unique)
        if ($courseStudentIds.Count -ne 40) { throw "$($course.Code) must cover 40 unique students across its four shifts, found $($courseStudentIds.Count)." }
    }
    if (@($courses | Where-Object Semester -ne $Semester | Where-Object { $_.Id -in $desired.CourseId }).Count -gt 0) { throw "The desired schedule contains a course outside Semester $Semester." }
    $shiftDayStudentCounts = @($shifts | ForEach-Object {
        $shift = $_
        foreach ($day in $shift.Days) {
            $studentIds = @($desired | Where-Object { $_.Shift -eq $shift.Name -and $_.Day -eq $day } | ForEach-Object {
                $entry = $_
                $studentShiftUpdates | Where-Object { $_.DepartmentId -eq $entry.DepartmentId -and $_.Year -eq $entry.Year -and $_.DesiredShift -eq $shift.Name } | Select-Object -ExpandProperty Id
            } | Sort-Object -Unique)
            $expectedStudents = if ($shift.Name -eq "Weekend") { 200 } else { 170 }
            if ($studentIds.Count -ne $expectedStudents) { throw "$($shift.Name), day $day must serve $expectedStudents students, found $($studentIds.Count)." }
            [pscustomobject]@{ Shift = $shift.Name; Day = $day; Students = $studentIds.Count }
        }
    })

    $entryRows = Invoke-Table $connection $transaction "SELECT [Id], [TimetableCode] FROM [ScheduleEntries] WHERE [Status] <> 'Cancelled';"
    $entries = @($entryRows | ForEach-Object { [pscustomobject]@{ Id = [Guid]$_.Id; Code = [string]$_.TimetableCode } } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
    $allCodeRows = Invoke-Table $connection $transaction "SELECT [TimetableCode] FROM [ScheduleEntries];"
    $usedCodes = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $allCodeRows) { $null = $usedCodes.Add([string]$row.TimetableCode) }

    $now = [DateTime]::UtcNow
    $studentShiftChanges = 0
    foreach ($assignment in $studentShiftUpdates) {
        if ($assignment.CurrentShift -eq $assignment.DesiredShift) { continue }
        Invoke-Statement $connection $transaction "UPDATE [Students] SET [Shift]=@Shift, [UpdatedAtUtc]=@Now WHERE [Id]=@Id;" @{ Id=$assignment.Id; Shift=$assignment.DesiredShift; Now=$now }
        $studentShiftChanges++
    }

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

    $activeInvariantRows = Invoke-Table $connection $transaction @"
SELECT s.[CourseId], s.[TeacherId], s.[YearLevel], s.[DayOfWeek], s.[StartsAt], s.[EndsAt], s.[ClassroomId],
       c.[Capacity] AS [CourseCapacity], r.[Capacity] AS [RoomCapacity], r.[ClassroomCode]
FROM [ScheduleEntries] s
INNER JOIN [Courses] c ON c.[Id] = s.[CourseId]
INNER JOIN [Classrooms] r ON r.[Id] = s.[ClassroomId]
WHERE s.[Status] <> 'Cancelled';
"@
    $activeInvariantEntries = @($activeInvariantRows | ForEach-Object {
        [pscustomobject]@{
            CourseId = [Guid]$_.CourseId; TeacherId = [Guid]$_.TeacherId; Year = [int]$_.YearLevel; Day = [int]$_.DayOfWeek
            Start = [TimeSpan]$_.StartsAt; End = [TimeSpan]$_.EndsAt; ClassroomId = [Guid]$_.ClassroomId
            ClassroomCode = [string]$_.ClassroomCode; CourseCapacity = [int]$_.CourseCapacity; RoomCapacity = [int]$_.RoomCapacity
        }
    })
    if ($activeInvariantEntries.Count -ne 295) { throw "Database verification expected 295 active timetable entries, found $($activeInvariantEntries.Count)." }
    $configuredPeriodKeys = @($desired | ForEach-Object { "$($_.Day)|$($_.Start)|$($_.End)" } | Sort-Object -Unique)
    if (@($activeInvariantEntries | Where-Object { "$($_.Day)|$($_.Start)|$($_.End)" -notin $configuredPeriodKeys }).Count -gt 0) { throw "Database verification found an active entry outside the configured weekday/weekend periods." }
    if (@($activeInvariantEntries | Where-Object { $_.CourseCapacity -lt 1 -or $_.CourseCapacity -gt 10000 -or $_.RoomCapacity -lt $_.CourseCapacity }).Count -gt 0) { throw "Database verification found a course capacity or room capacity violation." }
    if (@($activeInvariantEntries.CourseId | Sort-Object -Unique).Count -ne 20 -or @($activeInvariantEntries | Where-Object { $_.CourseId -notin $activeCourses.Id }).Count -gt 0) { throw "Database verification must contain only the 20 Semester $Semester courses." }
    foreach ($course in $activeCourses) {
        $expectedCopies = if ($course.Year -eq 1) { 8 } else { 17 }
        if (@($activeInvariantEntries | Where-Object CourseId -eq $course.Id).Count -ne $expectedCopies) { throw "Database verification requires $expectedCopies copies of $($course.Code)." }
    }
    if (@($activeInvariantEntries | Where-Object { ($_.Year -eq 1 -and $_.ClassroomCode -ne "501") -or ($_.Year -ge 2 -and $_.ClassroomCode -eq "501") }).Count -gt 0) { throw "Database verification found a Year/classroom policy violation for Classroom 501." }
    foreach ($entryGroup in @($activeInvariantEntries | Group-Object Day, Start, End)) {
        $expectedClasses = if ($entryGroup.Group[0].Day -in @(0, 6)) { 4 } elseif ($entryGroup.Group[0].Start -in @([TimeSpan]::Parse("07:30:00"), [TimeSpan]::Parse("14:00:00"), [TimeSpan]::Parse("17:30:00"))) { 9 } else { 8 }
        if ($entryGroup.Count -ne $expectedClasses -or @($entryGroup.Group.ClassroomId | Sort-Object -Unique).Count -ne $expectedClasses -or @($entryGroup.Group.TeacherId | Sort-Object -Unique).Count -ne $expectedClasses) {
            throw "Database verification found an exact period without $expectedClasses classes in distinct rooms with distinct teachers."
        }
    }

    $studentInvariantRows = Invoke-Table $connection $transaction @"
SELECT [DepartmentId], [YearLevel], [Shift], COUNT(*) AS [StudentCount]
FROM [Students]
WHERE [Status] <> 'Inactive'
GROUP BY [DepartmentId], [YearLevel], [Shift];
"@
    $studentInvariantGroups = @($studentInvariantRows | ForEach-Object {
        [pscustomobject]@{ DepartmentId = [Guid]$_.DepartmentId; Year = [int]$_.YearLevel; Shift = [string]$_.Shift; Count = [int]$_.StudentCount }
    })
    if ($studentInvariantGroups.Count -ne 80 -or @($studentInvariantGroups | Where-Object { $_.Count -ne 10 -or $_.Shift -notin $studentShifts }).Count -gt 0) {
        throw "Database verification requires 10 active students in every department/year/shift group."
    }

    $transaction.Commit()
    [pscustomobject]@{
        ActiveEntries = $desired.Count
        Semester = $Semester
        Shifts = $shifts.Count
        ShiftWindows = ($shifts | ForEach-Object { "$($_.Name) $($_.StartsAt.ToString('hh\:mm'))-$($_.EndsAt.ToString('hh\:mm'))" }) -join "; "
        DaysByShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Days -join ',')" }) -join "; "
        PeriodsPerShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Periods.Count)" }) -join "; "
        ExactPeriods = @($desired | Group-Object Day, Start, End).Count
        ClassesPerExactPeriod = "Weekday=8-9; Weekend=4"
        CoursesPerWeekdayShiftDay = 17
        CoursesPerWeekendDay = 20
        CurrentSemesterCourses = $activeCourses.Count
        Year1CopiesPerCourse = 8
        Year2To4CopiesPerCourse = 17
        CourseCapacityRange = "$minimumCourseCapacity-$maximumCourseCapacity"
        Departments = $departments.Count
        Courses = $courses.Count
        ActiveRooms = $rooms.Count
        RoomsAtMaximumCourseCapacity = $capacitySafeRooms.Count
        Students = $students.Count
        StudentsPerDepartmentYear = 40
        StudentsPerShiftDepartmentYear = 10
        StudentsPerExactPeriod = (@($exactPeriodStudentCounts | Sort-Object -Unique) -join ",")
        StudentsByShiftDay = ($shiftDayStudentCounts | ForEach-Object { "$($_.Shift)/$($_.Day)=$($_.Students)" }) -join "; "
        StudentShiftChanges = $studentShiftChanges
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
