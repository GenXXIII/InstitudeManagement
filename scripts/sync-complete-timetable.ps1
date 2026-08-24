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
SELECT c.[Id], c.[CourseCode], c.[TeacherId], c.[DepartmentId], d.[DepartmentCode], c.[Capacity], MIN(s.[YearLevel]) AS [YearLevel], MAX(s.[YearLevel]) AS [MaximumYearLevel]
FROM [Courses] c
INNER JOIN [Departments] d ON d.[Id] = c.[DepartmentId]
INNER JOIN [ScheduleEntries] s ON s.[CourseId] = c.[Id] AND s.[Status] <> 'Cancelled'
WHERE c.[IsActive] = 1
GROUP BY c.[Id], c.[CourseCode], c.[TeacherId], c.[DepartmentId], d.[DepartmentCode], c.[Capacity];
"@
    $courses = @($courseRows | ForEach-Object {
        [pscustomobject]@{
            Id = [Guid]$_.Id; Code = [string]$_.CourseCode; TeacherId = [Guid]$_.TeacherId
            DepartmentId = [Guid]$_.DepartmentId; DepartmentCode = [string]$_.DepartmentCode
            Capacity = [int]$_.Capacity; Year = [int]$_.YearLevel; MaximumYear = [int]$_.MaximumYearLevel
        }
    } | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
    if ($courses.Count -ne 40) { throw "Expected 40 current active courses, found $($courses.Count)." }
    $ambiguousCourseYears = @($courses | Where-Object { $_.Year -ne $_.MaximumYear })
    if ($ambiguousCourseYears.Count -gt 0) {
        $details = ($ambiguousCourseYears | ForEach-Object { "$($_.Code)=Years $($_.Year)-$($_.MaximumYear)" }) -join ", "
        throw "Each current course must belong to one Year 1-4 cohort before synchronization. Invalid courses: $details"
    }
    $invalidCourseCapacities = @($courses | Where-Object Capacity -ne 40)
    if ($invalidCourseCapacities.Count -gt 0) {
        $details = ($invalidCourseCapacities | ForEach-Object { "$($_.Code)=$($_.Capacity)" }) -join ", "
        throw "Expected all 40 current active courses to have Capacity=40. Invalid courses: $details"
    }

    $departments = @($courses | Group-Object DepartmentId | ForEach-Object { $_.Group[0] } | Sort-Object @{ Expression = { Get-CodeNumber $_.DepartmentCode } })
    if ($departments.Count -ne 5) { throw "Expected the 40 courses to cover 5 departments, found $($departments.Count)." }
    $coursesByYear = @{}
    foreach ($year in 1..4) {
        $yearCourses = @($courses | Where-Object Year -eq $year)
        if ($yearCourses.Count -ne 10) { throw "Expected 10 current courses for Year $year, found $($yearCourses.Count)." }
        $departmentCourses = @{}
        foreach ($department in $departments) {
            $items = @($yearCourses | Where-Object DepartmentId -eq $department.DepartmentId | Sort-Object @{ Expression = { Get-CodeNumber $_.Code } })
            if ($items.Count -ne 2) { throw "Expected 2 current courses for $($department.DepartmentCode) Year $year, found $($items.Count)." }
            $departmentCourses[$department.DepartmentId.ToString()] = $items
        }
        $interleaved = @()
        foreach ($coursePosition in 0..1) {
            foreach ($department in $departments) {
                $interleaved += $departmentCourses[$department.DepartmentId.ToString()][$coursePosition]
            }
        }
        foreach ($coursePosition in 0..1) {
            $departmentCount = @($interleaved[($coursePosition * 5)..(($coursePosition * 5) + 4)].DepartmentId | Sort-Object -Unique).Count
            if ($departmentCount -ne 5) { throw "Year $year course block $($coursePosition + 1) must cover all 5 departments." }
        }
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
    $capacitySafeRooms = @($rooms | Where-Object Capacity -ge 40)
    if ($capacitySafeRooms.Count -lt 4) { throw "Expected at least 4 active learning spaces with Capacity>=40, found $($capacitySafeRooms.Count)." }

    $teachingSlots = @()
    foreach ($shift in $shifts) {
        $courseIndex = 0
        foreach ($day in $shift.Days) {
            foreach ($period in $shift.Periods) {
                $teachingSlots += [pscustomobject]@{ Shift = $shift.Name; Day = $day; Period = $period; CourseIndex = $courseIndex }
                $courseIndex++
            }
        }
        if ($courseIndex -ne 10) { throw "$($shift.Name) must have 10 teaching slots, found $courseIndex." }
    }

    $desired = @()
    for ($slotIndex = 0; $slotIndex -lt $teachingSlots.Count; $slotIndex++) {
        $slot = $teachingSlots[$slotIndex]
        $usedRoomIds = [System.Collections.Generic.HashSet[Guid]]::new()
        foreach ($year in 1..4) {
            $yearCourses = $coursesByYear[$year]
            $course = $yearCourses[$slot.CourseIndex]
            $eligibleRooms = @($rooms | Where-Object { $_.Capacity -ge $course.Capacity -and ($year -eq 1 -or $_.Code -ne "501") })
            if ($eligibleRooms.Count -eq 0) { throw "No classroom has capacity $($course.Capacity) for $($course.Code), Year $year." }
            $preferredRoomIndex = (($slotIndex * 4) + ($year - 1)) % $eligibleRooms.Count
            $room = 0..($eligibleRooms.Count - 1) | ForEach-Object { $eligibleRooms[($preferredRoomIndex + $_) % $eligibleRooms.Count] } | Where-Object { -not $usedRoomIds.Contains($_.Id) } | Select-Object -First 1
            if ($null -eq $room) { throw "No room is available for Year $year in timetable slot $slotIndex." }
            $null = $usedRoomIds.Add($room.Id)
            $desired += [pscustomobject]@{
                CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $room.Id; Year = $year
                DepartmentId = $course.DepartmentId; Shift = $slot.Shift; PeriodLabel = $slot.Period.Session
                Day = $slot.Day; Start = $slot.Period.Start; End = $slot.Period.End
            }
        }
    }
    if ($desired.Count -ne 160) { throw "Expected 160 desired entries, found $($desired.Count)." }
    foreach ($shift in $shifts) {
        $shiftEntries = @($desired | Where-Object Shift -eq $shift.Name)
        if ($shiftEntries.Count -ne 40 -or @($shiftEntries.CourseId | Sort-Object -Unique).Count -ne 40) {
            throw "$($shift.Name) must schedule all 40 course IDs exactly once."
        }
        foreach ($year in 1..4) {
            if (@($shiftEntries | Where-Object Year -eq $year).Count -ne 10) { throw "$($shift.Name) must schedule 10 Year $year courses." }
        }
        foreach ($day in $shift.Days) {
            $shiftDayEntries = @($shiftEntries | Where-Object Day -eq $day)
            $expectedClasses = $shift.Periods.Count * 4
            if ($shiftDayEntries.Count -ne $expectedClasses -or @($shiftDayEntries | Group-Object Start, End).Count -ne $shift.Periods.Count -or @($shiftDayEntries.DepartmentId | Sort-Object -Unique).Count -ne $shift.Periods.Count) {
                throw "$($shift.Name), day $day must cover a distinct department in each of its $($shift.Periods.Count) periods."
            }
        }
    }
    $exactPeriodStudentCounts = @()
    foreach ($entryGroup in @($desired | Group-Object Day, Start, End)) {
        if ($entryGroup.Count -ne 4 -or @($entryGroup.Group.Year | Sort-Object -Unique).Count -ne 4 -or @($entryGroup.Group.ClassroomId | Sort-Object -Unique).Count -ne 4) {
            throw "Every exact weekday period must have one class for each of Years 1-4 in four distinct rooms."
        }
        $periodDepartments = @($entryGroup.Group.DepartmentId | Sort-Object -Unique)
        if ($periodDepartments.Count -ne 1) { throw "Every exact period must serve one department across Years 1-4 (40 students)." }
        $periodShift = $entryGroup.Group[0].Shift
        $periodStudentIds = @($studentShiftUpdates | Where-Object { $_.DepartmentId -eq $periodDepartments[0] -and $_.DesiredShift -eq $periodShift } | Select-Object -ExpandProperty Id -Unique)
        if ($periodStudentIds.Count -ne 40) { throw "Every exact period must serve 40 unique students, found $($periodStudentIds.Count)." }
        $exactPeriodStudentCounts += $periodStudentIds.Count
    }
    foreach ($course in $courses) {
        $copies = @($desired | Where-Object CourseId -eq $course.Id)
        if ($copies.Count -ne 4 -or @($copies.Shift | Sort-Object -Unique).Count -ne 4) { throw "$($course.Code) must appear exactly once in each shift." }
        $courseStudentIds = @($copies | ForEach-Object {
            $copy = $_
            $studentShiftUpdates | Where-Object { $_.DepartmentId -eq $course.DepartmentId -and $_.Year -eq $course.Year -and $_.DesiredShift -eq $copy.Shift } | Select-Object -ExpandProperty Id
        } | Sort-Object -Unique)
        if ($courseStudentIds.Count -ne 40) { throw "$($course.Code) must cover 40 unique students across its four shift copies, found $($courseStudentIds.Count)." }
    }
    $shiftDayStudentCounts = @($shifts | ForEach-Object {
        $shift = $_
        foreach ($day in $shift.Days) {
            $studentIds = @($desired | Where-Object { $_.Shift -eq $shift.Name -and $_.Day -eq $day } | ForEach-Object {
                $entry = $_
                $studentShiftUpdates | Where-Object { $_.DepartmentId -eq $entry.DepartmentId -and $_.Year -eq $entry.Year -and $_.DesiredShift -eq $shift.Name } | Select-Object -ExpandProperty Id
            } | Sort-Object -Unique)
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
SELECT s.[CourseId], s.[YearLevel], s.[DayOfWeek], s.[StartsAt], s.[EndsAt], s.[ClassroomId], c.[Capacity] AS [CourseCapacity], r.[Capacity] AS [RoomCapacity]
FROM [ScheduleEntries] s
INNER JOIN [Courses] c ON c.[Id] = s.[CourseId]
INNER JOIN [Classrooms] r ON r.[Id] = s.[ClassroomId]
WHERE s.[Status] <> 'Cancelled';
"@
    $activeInvariantEntries = @($activeInvariantRows | ForEach-Object {
        [pscustomobject]@{
            CourseId = [Guid]$_.CourseId; Year = [int]$_.YearLevel; Day = [int]$_.DayOfWeek
            Start = [TimeSpan]$_.StartsAt; End = [TimeSpan]$_.EndsAt; ClassroomId = [Guid]$_.ClassroomId
            CourseCapacity = [int]$_.CourseCapacity; RoomCapacity = [int]$_.RoomCapacity
        }
    })
    if ($activeInvariantEntries.Count -ne 160) { throw "Database verification expected 160 active timetable entries, found $($activeInvariantEntries.Count)." }
    $configuredPeriodKeys = @($desired | ForEach-Object { "$($_.Day)|$($_.Start)|$($_.End)" } | Sort-Object -Unique)
    if (@($activeInvariantEntries | Where-Object { "$($_.Day)|$($_.Start)|$($_.End)" -notin $configuredPeriodKeys }).Count -gt 0) { throw "Database verification found an active entry outside the configured weekday/weekend periods." }
    if (@($activeInvariantEntries | Where-Object { $_.CourseCapacity -ne 40 -or $_.RoomCapacity -lt $_.CourseCapacity }).Count -gt 0) { throw "Database verification found a course capacity or room capacity violation." }
    if (@($activeInvariantEntries | Group-Object CourseId | Where-Object Count -ne 4).Count -gt 0) { throw "Database verification requires exactly four active copies of every course." }
    foreach ($entryGroup in @($activeInvariantEntries | Group-Object Day, Start, End)) {
        if ($entryGroup.Count -ne 4 -or @($entryGroup.Group.Year | Sort-Object -Unique).Count -ne 4 -or @($entryGroup.Group.ClassroomId | Sort-Object -Unique).Count -ne 4) {
            throw "Database verification found an exact period without four Year 1-4 classes in distinct rooms."
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
        Shifts = $shifts.Count
        ShiftWindows = ($shifts | ForEach-Object { "$($_.Name) $($_.StartsAt.ToString('hh\:mm'))-$($_.EndsAt.ToString('hh\:mm'))" }) -join "; "
        DaysByShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Days -join ',')" }) -join "; "
        PeriodsPerShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Periods.Count)" }) -join "; "
        ExactPeriods = $teachingSlots.Count
        ClassesPerExactPeriod = 4
        YearsPerExactPeriod = "1,2,3,4"
        CoursesPerShift = 40
        CopiesPerCourse = 4
        CourseCapacity = 40
        Departments = $departments.Count
        Courses = $courses.Count
        ActiveRooms = $rooms.Count
        CapacitySafeRooms = $capacitySafeRooms.Count
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
