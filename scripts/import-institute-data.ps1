param(
    [string]$DataPath = (Join-Path $PSScriptRoot "..\docs\Data.txt"),
    [string]$Server = "INK-SQL-SERVER,1433",
    [string]$Database = "INK_Manangement",
    [string]$User = "sa",
    [string]$Password = "1234",
    [ValidateSet(1, 2)]
    [int]$Semester = 1
)

$ErrorActionPreference = "Stop"
$lines = Get-Content -LiteralPath $DataPath -Encoding utf8
$departmentNames = @("Business Administration", "Accounting & Finance", "English", "Computer Science & IT", "Law")

function New-Email([string]$name, [string]$code) {
    $slug = (($name.ToLowerInvariant() -replace "[^a-z0-9]+", ".").Trim("."))
    return "$slug.$($code.ToLowerInvariant() -replace '-', '')@gmail.com"
}

function New-Photo([string]$name) {
    $initials = (($name -split "\s+" | Select-Object -First 2 | ForEach-Object { $_.Substring(0, 1).ToUpperInvariant() }) -join "")
    $svg = "<svg xmlns='http://www.w3.org/2000/svg' width='240' height='360' viewBox='0 0 240 360'><rect width='240' height='360' fill='#e8f2ff'/><rect x='12' y='12' width='216' height='336' rx='8' fill='#ffffff'/><text x='120' y='198' text-anchor='middle' font-family='Arial' font-size='64' font-weight='700' fill='#2f70cf'>$initials</text></svg>"
    return "data:image/svg+xml;base64,$([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($svg)))"
}

function Invoke-Statement([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [string]$sql, [hashtable]$parameters = @{}) {
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

function Invoke-Scalar([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [string]$sql, [hashtable]$parameters = @{}) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = $sql
    foreach ($entry in $parameters.GetEnumerator()) {
        $value = if ($null -eq $entry.Value) { [DBNull]::Value } else { $entry.Value }
        $null = $command.Parameters.AddWithValue("@$($entry.Key)", $value)
    }
    $result = $command.ExecuteScalar()
    $command.Dispose()
    return $result
}

function Get-Setting([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [string]$section, [string]$key, [string]$fallback) {
    $result = Invoke-Scalar $connection $transaction "SELECT [Value] FROM [SystemSettings] WHERE [Section]=@Section AND [Key]=@Key;" @{ Section=$section; Key=$key }
    if ($null -eq $result -or $result -is [DBNull] -or [string]::IsNullOrWhiteSpace([string]$result)) { return $fallback }
    return [string]$result
}

function Add-Audit([System.Data.SqlClient.SqlConnection]$connection, [System.Data.SqlClient.SqlTransaction]$transaction, [int]$number, [Guid]$resourceId, [string]$type, [string]$subject, [hashtable]$details, [DateTime]$createdAt) {
    Invoke-Statement $connection $transaction @"
INSERT INTO [AuditLogs] ([Id],[AuditLogCode],[ResourceId],[Type],[Subject],[Action],[Details],[CreatedAtUtc],[UpdatedAtUtc])
VALUES (@Id,@Code,@ResourceId,@Type,@Subject,'Imported',@Details,@CreatedAt,@CreatedAt);
"@ @{
        Id = [Guid]::NewGuid(); Code = "AUD-IMPORT-$number"; ResourceId = $resourceId; Type = $type; Subject = $subject
        Details = ($details | ConvertTo-Json -Compress); CreatedAt = $createdAt
    }
}

$courseMeta = @{}
$currentDepartment = ""
foreach ($line in $lines[0..50]) {
    if ($departmentNames -contains $line.Trim()) { $currentDepartment = $line.Trim(); continue }
    $parts = $line -split "`t"
    if ($parts.Count -eq 3 -and $parts[0] -match "^Year (\d)$") {
        $courseMeta[$parts[2].Trim()] = @{ Department = $currentDepartment; Year = [int]$Matches[1]; Semester = [int]($parts[1] -replace "\D", "") }
    }
}

$courseRows = @()
foreach ($line in $lines[52..91]) {
    $parts = $line -split "`t"
    if ($parts.Count -ne 4) { throw "Invalid course row: $line" }
    $teachers = $parts[2] -split ",\s*"
    $meta = $courseMeta[$parts[1].Trim()]
    if ($null -eq $meta) { throw "Missing year and semester for course $($parts[1])." }
    $courseRows += [pscustomobject]@{
        Department = $parts[0].Trim(); Name = $parts[1].Trim(); PrimaryTeacher = $teachers[0].Trim()
        SecondaryTeacher = $teachers[1].Trim(); Capacity = [int]$parts[3]; Year = $meta.Year; Semester = $meta.Semester
    }
}
if ($courseRows.Count -ne 40) { throw "Expected 40 courses, found $($courseRows.Count)." }
$invalidCourseCapacities = @($courseRows | Where-Object Capacity -ne 40)
if ($invalidCourseCapacities.Count -gt 0) {
    $details = ($invalidCourseCapacities | ForEach-Object { "$($_.Name)=$($_.Capacity)" }) -join ", "
    throw "Expected all 40 source courses to have Capacity=40. Invalid courses: $details"
}

# Select one distinct teacher from each course's supplied pair.
$teacherToCourse = @{}
function Set-CourseTeacher([int]$courseIndex, [hashtable]$seenTeachers) {
    $row = $courseRows[$courseIndex]
    foreach ($teacherName in @($row.PrimaryTeacher, $row.SecondaryTeacher)) {
        if ($seenTeachers.ContainsKey($teacherName)) { continue }
        $seenTeachers[$teacherName] = $true
        if (-not $teacherToCourse.ContainsKey($teacherName) -or (Set-CourseTeacher $teacherToCourse[$teacherName] $seenTeachers)) {
            $teacherToCourse[$teacherName] = $courseIndex
            return $true
        }
    }
    return $false
}

for ($courseIndex = 0; $courseIndex -lt $courseRows.Count; $courseIndex++) {
    if (-not (Set-CourseTeacher $courseIndex @{})) {
        throw "Could not assign one distinct supplied teacher to course $($courseRows[$courseIndex].Name)."
    }
}
$assignedTeachers = [string[]]::new($courseRows.Count)
foreach ($entry in $teacherToCourse.GetEnumerator()) {
    $assignedTeachers[[int]$entry.Value] = [string]$entry.Key
}

$groups = @{}
$groups["Business Administration|1"] = @($lines[93..132] | ForEach-Object { ($_ -split "`t", 2)[1].Trim() })
$currentKey = $null
foreach ($line in $lines[133..($lines.Count - 1)]) {
    if ($line -match "^(Accounting & Finance|English|Computer Science & IT|Law).+Year ([1-4])") {
        $currentKey = "$($Matches[1])|$($Matches[2])"
        $groups[$currentKey] = @()
        continue
    }
    if ($currentKey -and $line.Trim() -match "^[A-Za-z]+\s+[A-Za-z]+$") {
        $groups[$currentKey] += $line.Trim()
    }
}

# Data.txt omits four cohorts. Reuse supplied cohort names so no invented person names are introduced.
$groups["Business Administration|2"] = @($groups["Accounting & Finance|2"])
$groups["Business Administration|3"] = @($groups["Accounting & Finance|3"])
$groups["Business Administration|4"] = @($groups["Accounting & Finance|4"])
$groups["Law|4"] = @($groups["Computer Science & IT|4"])

foreach ($department in $departmentNames) {
    foreach ($year in 1..4) {
        $key = "$department|$year"
        if (@($groups[$key]).Count -ne 40) { throw "Expected 40 students for $key, found $(@($groups[$key]).Count)." }
    }
}

$now = [DateTime]::UtcNow
$departments = @()
for ($index = 0; $index -lt $departmentNames.Count; $index++) {
    $departments += [pscustomobject]@{ Id = [Guid]::NewGuid(); Code = "DEP-$($index + 1)"; Name = $departmentNames[$index] }
}

$teachers = @()
for ($index = 0; $index -lt $courseRows.Count; $index++) {
    $row = $courseRows[$index]
    $department = $departments | Where-Object Name -eq $row.Department | Select-Object -First 1
    $code = "TEA-$($index + 1)"
    $teachers += [pscustomobject]@{
        Id = [Guid]::NewGuid(); Code = $code; Name = $assignedTeachers[$index]; Email = New-Email $assignedTeachers[$index] $code
        Photo = New-Photo $assignedTeachers[$index]; DepartmentId = $department.Id; Status = "Available"
    }
}

$courses = @()
for ($index = 0; $index -lt $courseRows.Count; $index++) {
    $row = $courseRows[$index]
    $department = $departments | Where-Object Name -eq $row.Department | Select-Object -First 1
    $courses += [pscustomobject]@{
        Id = [Guid]::NewGuid(); Code = "COU-$($index + 1)"; Name = $row.Name; DepartmentId = $department.Id
        DepartmentCode = $department.Code
        TeacherId = $teachers[$index].Id; Capacity = $row.Capacity; Year = $row.Year; Semester = $row.Semester
        AssignedTeacher = $assignedTeachers[$index]; SuppliedTeacherCandidates = @($row.PrimaryTeacher, $row.SecondaryTeacher)
    }
}
if (@($courses.TeacherId | Sort-Object -Unique).Count -ne $courses.Count) {
    throw "Each teacher must be assigned to exactly one course."
}

$students = @()
$studentNumber = 0
$studentShifts = @("Morning", "Afternoon", "Evening", "Weekend")
foreach ($department in $departments) {
    foreach ($year in 1..4) {
        $cohortNames = @($groups["$($department.Name)|$year"])
        for ($cohortIndex = 0; $cohortIndex -lt $cohortNames.Count; $cohortIndex++) {
            $name = $cohortNames[$cohortIndex]
            $studentNumber++
            $code = "STU-$studentNumber"
            $students += [pscustomobject]@{
                Id = [Guid]::NewGuid(); Code = $code; Name = $name; Email = New-Email $name $code
                Photo = New-Photo $name; DepartmentId = $department.Id; Year = $year
                Shift = $studentShifts[[Math]::Floor($cohortIndex / 10)]; Status = "Active"
            }
        }
    }
}
if ($students.Count -ne 800) { throw "Expected 800 students, found $($students.Count)." }
foreach ($department in $departments) {
    foreach ($year in 1..4) {
        $cohort = @($students | Where-Object { $_.DepartmentId -eq $department.Id -and $_.Year -eq $year })
        if ($cohort.Count -ne 40) { throw "Expected 40 students for $($department.Code) Year $year, found $($cohort.Count)." }
        foreach ($shift in $studentShifts) {
            $shiftCount = @($cohort | Where-Object Shift -eq $shift).Count
            if ($shiftCount -ne 10) { throw "Expected 10 students for $($department.Code) Year $year $shift, found $shiftCount." }
        }
    }
}

$roomCodes = @("101", "102", "103", "201", "202", "203", "301", "302", "303", "401", "402", "403", "501")
$classrooms = @()
for ($index = 0; $index -lt $roomCodes.Count; $index++) {
    $classrooms += [pscustomobject]@{
        Id = [Guid]::NewGuid(); Code = $roomCodes[$index]; Building = "INK Academic Building"
        RoomType = if ($roomCodes[$index] -eq "501") { "Meeting Room" } else { "Classroom" }
        Capacity = 40
        DepartmentId = $null; Status = "Available"; DeviceOnline = $true
    }
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

$timetable = @()
$activeCourses = @($courses | Where-Object Semester -eq $Semester)
if ($activeCourses.Count -ne 20) { throw "Expected 20 Semester $Semester courses, found $($activeCourses.Count)." }
$coursesByYear = @{}
foreach ($year in 1..4) {
    $yearCourses = @($activeCourses | Where-Object Year -eq $year)
    if ($yearCourses.Count -ne 5) { throw "Expected 5 Semester $Semester courses for Year $year, found $($yearCourses.Count)." }
    $departmentCourses = @{}
    foreach ($department in $departments) {
        $items = @($yearCourses | Where-Object DepartmentId -eq $department.Id | Sort-Object @{ Expression = { [int]($_.Code -replace "\D", "") } })
        if ($items.Count -ne 1) { throw "Expected 1 Semester $Semester course for $($department.Code) Year $year, found $($items.Count)." }
        $departmentCourses[$department.Id.ToString()] = $items[0]
    }
    $interleaved = @()
    foreach ($department in $departments) { $interleaved += $departmentCourses[$department.Id.ToString()] }
    if (@($interleaved.DepartmentId | Sort-Object -Unique).Count -ne 5) { throw "Year $year must cover all 5 departments in Semester $Semester." }
    $coursesByYear[$year] = $interleaved
}

$courseOrder = @()
foreach ($coursePosition in 0..4) {
    foreach ($year in 1..4) { $courseOrder += $coursesByYear[$year][$coursePosition] }
}
if ($courseOrder.Count -ne 20 -or @($courseOrder.Id | Sort-Object -Unique).Count -ne 20) { throw "Semester $Semester must provide 20 distinct courses across Years 1-4." }
$yearOneCourses = @($coursesByYear[1])
$olderCourseOrder = @()
foreach ($coursePosition in 0..4) {
    foreach ($year in 2..4) { $olderCourseOrder += $coursesByYear[$year][$coursePosition] }
}

$desiredTimetable = @()
foreach ($shift in $shifts) {
    for ($dayIndex = 0; $dayIndex -lt $shift.Days.Count; $dayIndex++) {
        $day = $shift.Days[$dayIndex]
        if ($shift.Name -eq "Weekend") {
            foreach ($coursePosition in 0..4) {
                $period = $shift.Periods[$coursePosition]
                foreach ($year in 1..4) {
                    $course = $coursesByYear[$year][$coursePosition]
                    $desiredTimetable += [pscustomobject]@{
                        CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = $course.Year
                        DepartmentId = $course.DepartmentId; Day = $day; Session = $shift.Name; PeriodLabel = $period.Session
                        Start = $period.Start; End = $period.End; Status = "Upcoming"
                    }
                }
            }
            continue
        }

        for ($courseIndex = 0; $courseIndex -lt $olderCourseOrder.Count; $courseIndex++) {
            $course = $olderCourseOrder[$courseIndex]
            $periodIndex = if ($courseIndex -lt 8) { 0 } else { 1 }
            $period = $shift.Periods[$periodIndex]
            $desiredTimetable += [pscustomobject]@{
                CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = $course.Year
                DepartmentId = $course.DepartmentId; Day = $day; Session = $shift.Name; PeriodLabel = $period.Session
                Start = $period.Start; End = $period.End; Status = "Upcoming"
            }
        }
        foreach ($periodIndex in 0..1) {
            $course = $yearOneCourses[(($dayIndex * 2) + $periodIndex) % $yearOneCourses.Count]
            $period = $shift.Periods[$periodIndex]
            $desiredTimetable += [pscustomobject]@{
                CourseId = $course.Id; TeacherId = $course.TeacherId; ClassroomId = $null; Year = $course.Year
                DepartmentId = $course.DepartmentId; Day = $day; Session = $shift.Name; PeriodLabel = $period.Session
                Start = $period.Start; End = $period.End; Status = "Upcoming"
            }
        }
    }
}

$periodGroupIndex = 0
foreach ($entryGroup in @($desiredTimetable | Group-Object Session, Day, Start, End)) {
    $usedRoomIds = [System.Collections.Generic.HashSet[Guid]]::new()
    foreach ($item in $entryGroup.Group) {
        $course = $activeCourses | Where-Object Id -eq $item.CourseId | Select-Object -First 1
        $eligibleRooms = @($classrooms | Where-Object {
            $_.Capacity -ge $course.Capacity -and (($item.Year -eq 1 -and $_.Code -eq "501") -or ($item.Year -ne 1 -and $_.Code -ne "501"))
        } | Sort-Object Code)
        if ($eligibleRooms.Count -eq 0) { throw "No classroom has capacity $($course.Capacity) for $($course.Code), Year $($item.Year)." }
        $preferredRoomIndex = ($periodGroupIndex + $item.Year - 1) % $eligibleRooms.Count
        $room = 0..($eligibleRooms.Count - 1) | ForEach-Object { $eligibleRooms[($preferredRoomIndex + $_) % $eligibleRooms.Count] } | Where-Object {
            -not $usedRoomIds.Contains($_.Id)
        } | Select-Object -First 1
        if ($null -eq $room) { throw "No available classroom for Year $($item.Year) on day $($item.Day) at $($item.Start)." }
        $null = $usedRoomIds.Add($room.Id)
        $item.ClassroomId = $room.Id
    }
    $periodGroupIndex++
}
for ($index = 0; $index -lt $desiredTimetable.Count; $index++) {
    $item = $desiredTimetable[$index]
    $timetable += [pscustomobject]@{
        Id = [Guid]::NewGuid(); Code = "TIM-$($index + 1)"; CourseId = $item.CourseId; TeacherId = $item.TeacherId
        ClassroomId = $item.ClassroomId; Year = $item.Year; DepartmentId = $item.DepartmentId
        Day = $item.Day; Session = $item.Session; PeriodLabel = $item.PeriodLabel
        Start = $item.Start; End = $item.End; Status = $item.Status
    }
}
if ($timetable.Count -ne 295) { throw "Expected 295 timetable entries, found $($timetable.Count)." }
foreach ($shift in $shifts) {
    $shiftEntries = @($timetable | Where-Object Session -eq $shift.Name)
    $expectedPerDay = if ($shift.Name -eq "Weekend") { 20 } else { 17 }
    $expectedShiftEntries = $shift.Days.Count * $expectedPerDay
    if ($shiftEntries.Count -ne $expectedShiftEntries -or @($shiftEntries.CourseId | Sort-Object -Unique).Count -ne 20) { throw "$($shift.Name) must cover all 20 Semester $Semester courses across its scheduled days." }
    foreach ($day in $shift.Days) {
        $shiftDayEntries = @($shiftEntries | Where-Object Day -eq $day)
        if ($shiftDayEntries.Count -ne $expectedPerDay -or @($shiftDayEntries | Group-Object Start, End).Count -ne $shift.Periods.Count) { throw "$($shift.Name), day $day must schedule $expectedPerDay current-semester classes." }
        foreach ($year in 1..4) {
            $expectedYearCount = if ($year -eq 1 -and $shift.Name -ne "Weekend") { 2 } else { 5 }
            if (@($shiftDayEntries | Where-Object Year -eq $year).Count -ne $expectedYearCount) { throw "$($shift.Name), day $day must schedule $expectedYearCount Year $year courses." }
        }
        $periodCounts = @($shiftDayEntries | Group-Object Start, End | Select-Object -ExpandProperty Count | Sort-Object)
        $expectedPeriodCounts = if ($shift.Name -eq "Weekend") { @(4, 4, 4, 4, 4) } else { @(8, 9) }
        if (($periodCounts -join ",") -ne ($expectedPeriodCounts -join ",")) { throw "$($shift.Name), day $day has an invalid period distribution: $($periodCounts -join ',')." }
    }
}
$exactPeriodStudentCounts = @()
foreach ($entryGroup in @($timetable | Group-Object Day, Start, End)) {
    $weekendPeriod = $entryGroup.Group[0].Session -eq "Weekend"
    $periodStart = [TimeSpan]$entryGroup.Group[0].Start
    $firstWeekdayPeriod = $periodStart -in @([TimeSpan]::Parse("07:30:00"), [TimeSpan]::Parse("14:00:00"), [TimeSpan]::Parse("17:30:00"))
    $expectedClasses = if ($weekendPeriod) { 4 } elseif ($firstWeekdayPeriod) { 9 } else { 8 }
    if ($entryGroup.Count -ne $expectedClasses -or @($entryGroup.Group.ClassroomId | Sort-Object -Unique).Count -ne $expectedClasses -or @($entryGroup.Group.TeacherId | Sort-Object -Unique).Count -ne $expectedClasses) { throw "Every exact period must use distinct teachers and rooms for all $expectedClasses classes." }
    $periodShift = $entryGroup.Group[0].Session
    $periodStudentIds = @($entryGroup.Group | ForEach-Object {
        $entry = $_
        $students | Where-Object { $_.DepartmentId -eq $entry.DepartmentId -and $_.Year -eq $entry.Year -and $_.Shift -eq $periodShift } | Select-Object -ExpandProperty Id
    } | Sort-Object -Unique)
    $expectedStudents = $expectedClasses * 10
    if ($periodStudentIds.Count -ne $expectedStudents) { throw "Every exact period must serve $expectedStudents unique students, found $($periodStudentIds.Count)." }
    $exactPeriodStudentCounts += $periodStudentIds.Count
}
foreach ($course in $activeCourses) {
    $copies = @($timetable | Where-Object CourseId -eq $course.Id)
    $expectedCopies = if ($course.Year -eq 1) { 8 } else { 17 }
    if ($copies.Count -ne $expectedCopies -or @($copies.Session | Sort-Object -Unique).Count -ne 4) { throw "$($course.Code) must appear in all four shifts ($expectedCopies copies)." }
    $courseStudentIds = @($copies | ForEach-Object {
        $copy = $_
        $students | Where-Object { $_.DepartmentId -eq $course.DepartmentId -and $_.Year -eq $course.Year -and $_.Shift -eq $copy.Session } | Select-Object -ExpandProperty Id
    } | Sort-Object -Unique)
    if ($courseStudentIds.Count -ne 40) { throw "$($course.Code) must cover 40 unique students across its four shifts, found $($courseStudentIds.Count)." }
}
if (@($courses | Where-Object Semester -ne $Semester | Where-Object { $_.Id -in $timetable.CourseId }).Count -gt 0) { throw "The timetable contains a course outside Semester $Semester." }
$shiftDayStudentCounts = @($shifts | ForEach-Object {
    $shift = $_
    foreach ($day in $shift.Days) {
        $studentIds = @($timetable | Where-Object { $_.Session -eq $shift.Name -and $_.Day -eq $day } | ForEach-Object {
            $entry = $_
            $course = $courses | Where-Object Id -eq $entry.CourseId | Select-Object -First 1
            $students | Where-Object { $_.DepartmentId -eq $course.DepartmentId -and $_.Year -eq $entry.Year -and $_.Shift -eq $shift.Name } | Select-Object -ExpandProperty Id
        } | Sort-Object -Unique)
        $expectedStudents = if ($shift.Name -eq "Weekend") { 200 } else { 170 }
        if ($studentIds.Count -ne $expectedStudents) { throw "$($shift.Name), day $day must serve $expectedStudents students, found $($studentIds.Count)." }
        [pscustomobject]@{ Shift = $shift.Name; Day = $day; Students = $studentIds.Count }
    }
})
$attendance = @()
$grades = @()

$connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Encrypt=True"
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
$transaction = $connection.BeginTransaction()
try {
    Invoke-Statement $connection $transaction @"
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
DELETE FROM [ClassSessionRecords]; DELETE FROM [AttendanceRecords]; DELETE FROM [GradeRecords];
IF OBJECT_ID(N'[Enrollment].[TimetableEnrollments]', N'U') IS NOT NULL DELETE FROM [Enrollment].[TimetableEnrollments];
DELETE FROM [ScheduleEntries];
IF OBJECT_ID(N'[Enrollment].[StudentEnrollments]', N'U') IS NOT NULL DELETE FROM [Enrollment].[StudentEnrollments];
IF OBJECT_ID(N'[Enrollment].[CourseAssignments]', N'U') IS NOT NULL DELETE FROM [Enrollment].[CourseAssignments];
IF OBJECT_ID(N'[Enrollment].[ClassroomAssignments]', N'U') IS NOT NULL DELETE FROM [Enrollment].[ClassroomAssignments];
IF OBJECT_ID(N'[Enrollment].[TeacherAssignments]', N'U') IS NOT NULL DELETE FROM [Enrollment].[TeacherAssignments];
DELETE FROM [Courses]; DELETE FROM [Students]; DELETE FROM [Classrooms];
UPDATE [Departments] SET [HeadTeacherId] = NULL; DELETE FROM [Teachers]; DELETE FROM [Departments]; DELETE FROM [AuditLogs];
"@

    $academicYear = Get-Setting $connection $transaction "academic-year" "currentYear" "2026$([char]0x2013)2027"
    $term = Get-Setting $connection $transaction "semester" "currentTerm" "Semester 1"
    $attendanceMethod = Get-Setting $connection $transaction "attendance-rules" "method" "ID Card"
    $aMinimum = [decimal](Get-Setting $connection $transaction "grade-rules" "aMinimum" "90")
    $bMinimum = [decimal](Get-Setting $connection $transaction "grade-rules" "bMinimum" "80")
    $cMinimum = [decimal](Get-Setting $connection $transaction "grade-rules" "cMinimum" "70")
    $dMinimum = [decimal](Get-Setting $connection $transaction "grade-rules" "dMinimum" "60")
    $eMinimum = [decimal](Get-Setting $connection $transaction "grade-rules" "eMinimum" "50")
    $termSemester = if ($term -match "2") { 2 } else { 1 }
    if ($termSemester -ne $Semester) { throw "The importer was prepared for Semester $Semester, but Administration currentTerm is $term. Run with -Semester $termSemester." }
    $attendanceDate = $now.Date.AddDays(-((([int]$now.DayOfWeek + 6) % 7)))
    $shiftStarts = @{
        Morning = [TimeSpan]::Parse("07:30:00")
        Afternoon = [TimeSpan]::Parse("14:00:00")
        Evening = [TimeSpan]::Parse("17:30:00")
        Weekend = [TimeSpan]::Parse("07:00:00")
    }
    for ($index = 0; $index -lt $students.Count; $index++) {
        $student = $students[$index]
        $course = $courses | Where-Object { $_.DepartmentId -eq $student.DepartmentId -and $_.Year -eq $student.Year -and $_.Semester -eq $termSemester } | Select-Object -First 1
        if ($null -eq $course) { throw "No current-semester course found for $($student.Code)." }
        $score = [decimal](60 + (($index * 7) % 36))
        $letter = if ($score -ge $aMinimum) { "A" } elseif ($score -ge $bMinimum) { "B" } elseif ($score -ge $cMinimum) { "C" } elseif ($score -ge $dMinimum) { "D" } elseif ($score -ge $eMinimum) { "E" } else { "F" }
        $attendance += [pscustomobject]@{
            Id=[Guid]::NewGuid(); Code="ATT-$($index + 1)"; StudentId=$student.Id; Date=$attendanceDate
            CheckedInAt=$shiftStarts[$student.Shift].Add([TimeSpan]::FromMinutes($index % 10)); Status="Present"
            Method=$attendanceMethod; AcademicYear=$academicYear; Term=$term
        }
        $grades += [pscustomobject]@{
            Id=[Guid]::NewGuid(); Code="GRD-$($index + 1)"; StudentId=$student.Id; CourseId=$course.Id
            Score=$score; Letter=$letter; AcademicYear=$academicYear; Term=$term
        }
    }

    foreach ($department in $departments) {
        Invoke-Statement $connection $transaction "INSERT INTO [Departments] ([Id],[DepartmentCode],[Name],[Head],[HeadTeacherId],[IsActive],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@Name,'Not appointed',NULL,1,@CreatedAt,@CreatedAt);" @{ Id=$department.Id; Code=$department.Code; Name=$department.Name; CreatedAt=$now }
    }
    foreach ($teacher in $teachers) {
        Invoke-Statement $connection $transaction "INSERT INTO [Teachers] ([Id],[TeacherCode],[FullName],[Email],[PhotoDataUrl],[DepartmentId],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@Name,@Email,@Photo,@DepartmentId,@Status,@CreatedAt,@CreatedAt);" @{ Id=$teacher.Id; Code=$teacher.Code; Name=$teacher.Name; Email=$teacher.Email; Photo=$teacher.Photo; DepartmentId=$teacher.DepartmentId; Status=$teacher.Status; CreatedAt=$now }
    }
    foreach ($department in $departments) {
        $head = $teachers | Where-Object DepartmentId -eq $department.Id | Select-Object -First 1
        Invoke-Statement $connection $transaction "UPDATE [Departments] SET [Head]=@Head,[HeadTeacherId]=@HeadTeacherId,[UpdatedAtUtc]=@UpdatedAt WHERE [Id]=@Id;" @{ Head=$head.Name; HeadTeacherId=$head.Id; UpdatedAt=$now; Id=$department.Id }
    }
    foreach ($student in $students) {
        Invoke-Statement $connection $transaction "INSERT INTO [Students] ([Id],[StudentCode],[FullName],[Email],[PhotoDataUrl],[DepartmentId],[YearLevel],[Shift],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@Name,@Email,@Photo,@DepartmentId,@Year,@Shift,@Status,@CreatedAt,@CreatedAt);" @{ Id=$student.Id; Code=$student.Code; Name=$student.Name; Email=$student.Email; Photo=$student.Photo; DepartmentId=$student.DepartmentId; Year=$student.Year; Shift=$student.Shift; Status=$student.Status; CreatedAt=$now }
    }
    foreach ($course in $courses) {
        Invoke-Statement $connection $transaction "INSERT INTO [Courses] ([Id],[CourseCode],[Name],[DepartmentId],[TeacherId],[Capacity],[IsActive],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@Name,@DepartmentId,@TeacherId,@Capacity,1,@CreatedAt,@CreatedAt);" @{ Id=$course.Id; Code=$course.Code; Name=$course.Name; DepartmentId=$course.DepartmentId; TeacherId=$course.TeacherId; Capacity=$course.Capacity; CreatedAt=$now }
    }
    foreach ($room in $classrooms) {
        Invoke-Statement $connection $transaction "INSERT INTO [Classrooms] ([Id],[ClassroomCode],[Building],[RoomType],[Capacity],[DepartmentId],[Status],[DeviceOnline],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@Building,@RoomType,@Capacity,NULL,@Status,@DeviceOnline,@CreatedAt,@CreatedAt);" @{ Id=$room.Id; Code=$room.Code; Building=$room.Building; RoomType=$room.RoomType; Capacity=$room.Capacity; Status=$room.Status; DeviceOnline=$room.DeviceOnline; CreatedAt=$now }
    }
    foreach ($teacher in $teachers) {
        Invoke-Statement $connection $transaction "INSERT INTO [Enrollment].[TeacherAssignments] ([Id],[TeacherId],[DepartmentId],[AcademicYear],[Semester],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (NEWID(),@TeacherId,@DepartmentId,@AcademicYear,@Semester,'Assigned',@CreatedAt,@CreatedAt);" @{ TeacherId=$teacher.Id; DepartmentId=$teacher.DepartmentId; AcademicYear=$academicYear; Semester=$term; CreatedAt=$now }
    }
    foreach ($student in $students) {
        Invoke-Statement $connection $transaction "INSERT INTO [Enrollment].[StudentEnrollments] ([Id],[StudentId],[DepartmentId],[YearLevel],[Shift],[AcademicYear],[Semester],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (NEWID(),@StudentId,@DepartmentId,@Year,@Shift,@AcademicYear,@Semester,'Active',@CreatedAt,@CreatedAt);" @{ StudentId=$student.Id; DepartmentId=$student.DepartmentId; Year=$student.Year; Shift=$student.Shift; AcademicYear=$academicYear; Semester=$term; CreatedAt=$now }
    }
    foreach ($course in $courses | Where-Object Semester -eq $termSemester) {
        Invoke-Statement $connection $transaction "INSERT INTO [Enrollment].[CourseAssignments] ([Id],[CourseId],[DepartmentId],[TeacherId],[YearLevel],[Capacity],[AcademicYear],[Semester],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (NEWID(),@CourseId,@DepartmentId,@TeacherId,@Year,@Capacity,@AcademicYear,@Semester,'Active',@CreatedAt,@CreatedAt);" @{ CourseId=$course.Id; DepartmentId=$course.DepartmentId; TeacherId=$course.TeacherId; Year=$course.Year; Capacity=$course.Capacity; AcademicYear=$academicYear; Semester=$term; CreatedAt=$now }
    }
    foreach ($room in $classrooms) {
        Invoke-Statement $connection $transaction "INSERT INTO [Enrollment].[ClassroomAssignments] ([Id],[ClassroomId],[DepartmentId],[Capacity],[Access],[AcademicYear],[Semester],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (NEWID(),@ClassroomId,NULL,@Capacity,'Shared institute',@AcademicYear,@Semester,'Available',@CreatedAt,@CreatedAt);" @{ ClassroomId=$room.Id; Capacity=$room.Capacity; AcademicYear=$academicYear; Semester=$term; CreatedAt=$now }
    }
    foreach ($entry in $timetable) {
        Invoke-Statement $connection $transaction "INSERT INTO [ScheduleEntries] ([Id],[TimetableCode],[CourseId],[ClassroomId],[TeacherId],[YearLevel],[DayOfWeek],[StartsAt],[EndsAt],[Status],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@CourseId,@ClassroomId,@TeacherId,@Year,@Day,@Start,@End,@Status,@CreatedAt,@CreatedAt);" @{ Id=$entry.Id; Code=$entry.Code; CourseId=$entry.CourseId; ClassroomId=$entry.ClassroomId; TeacherId=$entry.TeacherId; Year=$entry.Year; Day=$entry.Day; Start=$entry.Start; End=$entry.End; Status=$entry.Status; CreatedAt=$now }
    }
    Invoke-Statement $connection $transaction "IF OBJECT_ID(N'[Enrollment].[TimetableEnrollments]', N'U') IS NOT NULL INSERT INTO [Enrollment].[TimetableEnrollments] ([Id],[ScheduleEntryId],[AcademicYear],[Semester],[Status],[CreatedAtUtc],[UpdatedAtUtc]) SELECT NEWID(),[Id],@AcademicYear,@Semester,'Active',@CreatedAt,@CreatedAt FROM [ScheduleEntries] WHERE [Status] <> 'Cancelled';" @{ AcademicYear=$academicYear; Semester=$term; CreatedAt=$now }
    foreach ($record in $attendance) {
        Invoke-Statement $connection $transaction "INSERT INTO [AttendanceRecords] ([Id],[AttendanceCode],[StudentId],[Date],[CheckedInAt],[Status],[Method],[AcademicYear],[Term],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@StudentId,@Date,@CheckedInAt,@Status,@Method,@AcademicYear,@Term,@CreatedAt,@CreatedAt);" @{ Id=$record.Id; Code=$record.Code; StudentId=$record.StudentId; Date=$record.Date; CheckedInAt=$record.CheckedInAt; Status=$record.Status; Method=$record.Method; AcademicYear=$record.AcademicYear; Term=$record.Term; CreatedAt=$now }
    }
    foreach ($record in $grades) {
        Invoke-Statement $connection $transaction "INSERT INTO [GradeRecords] ([Id],[GradeCode],[StudentId],[CourseId],[Score],[LetterGrade],[AcademicYear],[Term],[CreatedAtUtc],[UpdatedAtUtc]) VALUES (@Id,@Code,@StudentId,@CourseId,@Score,@LetterGrade,@AcademicYear,@Term,@CreatedAt,@CreatedAt);" @{ Id=$record.Id; Code=$record.Code; StudentId=$record.StudentId; CourseId=$record.CourseId; Score=$record.Score; LetterGrade=$record.Letter; AcademicYear=$record.AcademicYear; Term=$record.Term; CreatedAt=$now }
    }

    $auditNumber = 0
    foreach ($department in $departments) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $department.Id "Department" $department.Name @{ departmentCode=$department.Code; name=$department.Name; status="Active"; importSource="docs/Data.txt" } $now }
    foreach ($teacher in $teachers) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $teacher.Id "Teacher" $teacher.Name @{ teacherCode=$teacher.Code; name=$teacher.Name; email=$teacher.Email; departmentId=$teacher.DepartmentId; status=$teacher.Status; importSource="docs/Data.txt" } $now }
    foreach ($student in $students) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $student.Id "Student" $student.Name @{ studentCode=$student.Code; name=$student.Name; email=$student.Email; departmentId=$student.DepartmentId; year=$student.Year; shift=$student.Shift; status=$student.Status; importSource="docs/Data.txt" } $now }
    foreach ($course in $courses) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $course.Id "Course" $course.Name @{ courseCode=$course.Code; name=$course.Name; departmentId=$course.DepartmentId; teacherId=$course.TeacherId; assignedTeacher=$course.AssignedTeacher; suppliedTeacherCandidates=$course.SuppliedTeacherCandidates; capacity=$course.Capacity; year=$course.Year; semester=$course.Semester; status="Active"; importSource="docs/Data.txt" } $now }
    foreach ($room in $classrooms) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $room.Id "Classroom" $room.Code @{ classroomCode=$room.Code; building=$room.Building; roomType=$room.RoomType; capacity=$room.Capacity; access="Shared institute"; status=$room.Status; deviceOnline=$room.DeviceOnline; importSource="docs/Data.txt" } $now }
    foreach ($entry in $timetable) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $entry.Id "Timetable" $entry.Code @{ timetableCode=$entry.Code; courseId=$entry.CourseId; teacherId=$entry.TeacherId; classroomId=$entry.ClassroomId; year=$entry.Year; dayOfWeek=$entry.Day; shift=$entry.Session; session=$entry.PeriodLabel; startsAt=$entry.Start.ToString(); endsAt=$entry.End.ToString(); status=$entry.Status; importSource="docs/Data.txt" } $now }
    foreach ($record in $attendance) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $record.Id "Attendance" $record.Code @{ attendanceCode=$record.Code; studentId=$record.StudentId; date=$record.Date; checkedInAt=$record.CheckedInAt.ToString(); status=$record.Status; method=$record.Method; academicYear=$record.AcademicYear; term=$record.Term; importSource="docs/Data.txt" } $now }
    foreach ($record in $grades) { $auditNumber++; Add-Audit $connection $transaction $auditNumber $record.Id "Grade" $record.Code @{ gradeCode=$record.Code; studentId=$record.StudentId; courseId=$record.CourseId; score=$record.Score; letterGrade=$record.Letter; academicYear=$record.AcademicYear; term=$record.Term; importSource="docs/Data.txt" } $now }

    $transaction.Commit()
}
catch {
    $transaction.Rollback()
    throw
}
finally {
    $transaction.Dispose()
    $connection.Dispose()
}

[pscustomobject]@{
    Departments = $departments.Count
    Teachers = $teachers.Count
    Students = $students.Count
    Courses = $courses.Count
    Classrooms = $classrooms.Count
    CapacitySafeRooms = @($classrooms | Where-Object Capacity -ge 40).Count
    TimetableEntries = $timetable.Count
    Shifts = $shifts.Count
    ShiftWindows = ($shifts | ForEach-Object { "$($_.Name) $($_.StartsAt.ToString('hh\:mm'))-$($_.EndsAt.ToString('hh\:mm'))" }) -join "; "
    DaysByShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Days -join ',')" }) -join "; "
    PeriodsPerShift = ($shifts | ForEach-Object { "$($_.Name)=$($_.Periods.Count)" }) -join "; "
    Semester = $Semester
    ExactPeriods = @($timetable | Group-Object Day, Start, End).Count
    ClassesPerExactPeriod = "Weekday=8-9; Weekend=4"
    CoursesPerWeekdayShiftDay = 17
    CoursesPerWeekendDay = 20
    CurrentSemesterCourses = $activeCourses.Count
    Year1CopiesPerCourse = 8
    Year2To4CopiesPerCourse = 17
    CourseCapacity = 40
    StudentsPerDepartmentYear = 40
    StudentsPerShiftDepartmentYear = 10
    StudentsPerExactPeriod = (@($exactPeriodStudentCounts | Sort-Object -Unique) -join ",")
    StudentsByShiftDay = ($shiftDayStudentCounts | ForEach-Object { "$($_.Shift)/$($_.Day)=$($_.Students)" }) -join "; "
    Attendance = $attendance.Count
    Grades = $grades.Count
    AuditLogs = 5 + 40 + 800 + 40 + 13 + $timetable.Count + $attendance.Count + $grades.Count
}
