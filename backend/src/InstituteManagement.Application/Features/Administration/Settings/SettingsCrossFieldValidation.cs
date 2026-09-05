using System.Globalization;

namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static void ValidateCrossFields(
        string section,
        IReadOnlyDictionary<string, string> values,
        ICollection<string> errors)
    {
        switch (section)
        {
            case "academic-year":
                ValidateWindow(values, "startsOn", "endsOn", "Academic year", errors);
                break;
            case "semester":
                ValidateSemester(values, errors);
                break;
            case "attendance-rules":
                ValidateAttendanceThresholds(values, errors);
                break;
            case "grade-rules":
                ValidateGradeRules(values, errors);
                break;
            case "notifications":
                ValidateNotificationChannels(values, errors);
                break;
        }
    }

    private static void ValidateSemester(IReadOnlyDictionary<string, string> values, ICollection<string> errors)
    {
        if (!TryWindow(values, "semester1StartsOn", "semester1EndsOn", "Semester 1", errors, out var semester1Start, out var semester1End)
            || !TryWindow(values, "semester2StartsOn", "semester2EndsOn", "Semester 2", errors, out var semester2Start, out var semester2End)
            || !TryWindow(values, "summerStartsOn", "summerEndsOn", "Summer Term", errors, out var summerStart, out var summerEnd)) return;
        if (!(semester1End < semester2Start && semester2End < summerStart))
            errors.Add("Term date windows must be ordered Semester 1, Semester 2, then Summer Term without overlap.");

        var selected = values["currentTerm"] switch
        {
            "Semester 1" => (semester1Start, semester1End),
            "Semester 2" => (semester2Start, semester2End),
            _ => (summerStart, summerEnd)
        };
        if (Date(values["startsOn"]) != selected.Item1 || Date(values["endsOn"]) != selected.Item2)
            errors.Add("startsOn and endsOn must match the selected currentTerm window.");
    }

    private static void ValidateAttendanceThresholds(IReadOnlyDictionary<string, string> values, ICollection<string> errors)
    {
        var onTime = Integer(values["onTimeThresholdMinutes"]);
        var late = Integer(values["lateThresholdMinutes"]);
        var veryLate = Integer(values["veryLateThresholdMinutes"]);
        if (!(onTime < late && late < veryLate))
            errors.Add("Attendance thresholds must progress from On Time to Late to Very Late.");
    }

    private static void ValidateGradeRules(IReadOnlyDictionary<string, string> values, ICollection<string> errors)
    {
        var minimum = Decimal(values["minimumScore"]);
        var maximum = Decimal(values["maximumScore"]);
        var thresholds = new[] { "aPlusMinimum", "aMinimum", "bPlusMinimum", "bMinimum", "cPlusMinimum", "cMinimum", "dMinimum" }
            .Select(key => Decimal(values[key]))
            .ToArray();
        if (minimum >= maximum) errors.Add("minimumScore must be lower than maximumScore.");
        if (!(thresholds[0] <= maximum
            && thresholds.Zip(thresholds.Skip(1)).All(pair => pair.First > pair.Second)
            && thresholds[^1] >= minimum))
            errors.Add("Grade thresholds must descend A+, A, B+, B, C+, C, D; lower scores are F.");

        foreach (var key in new[] { "passMark", "overallPassMark", "coursePassMark", "finalExamMinimum" })
            if (Decimal(values[key]) < minimum || Decimal(values[key]) > maximum)
                errors.Add($"{key} must be within the score range.");
        var maximumGpa = Decimal(values["maximumGpa"]);
        foreach (var key in new[] { "aPlusGpa", "aGpa", "bPlusGpa", "bGpa", "cPlusGpa", "cGpa", "dGpa", "fGpa", "gpaScale" })
            if (Decimal(values[key]) > maximumGpa)
                errors.Add($"{key} cannot exceed maximumGpa.");
    }

    private static void ValidateNotificationChannels(IReadOnlyDictionary<string, string> values, ICollection<string> errors)
    {
        if (bool.TryParse(values["emailEnabled"], out var emailEnabled) && emailEnabled)
            foreach (var key in new[] { "smtpHost", "smtpPort", "emailEncryption", "senderName", "senderEmail" })
                if (string.IsNullOrWhiteSpace(values[key])) errors.Add($"{key} is required while email is enabled.");
        if (bool.TryParse(values["smsEnabled"], out var smsEnabled) && smsEnabled && values["smsProvider"] == "None")
            errors.Add("smsProvider must be configured while SMS is enabled.");
    }

    private static void ValidateWindow(IReadOnlyDictionary<string, string> values, string startKey, string endKey, string label, ICollection<string> errors) =>
        TryWindow(values, startKey, endKey, label, errors, out _, out _);

    private static bool TryWindow(
        IReadOnlyDictionary<string, string> values,
        string startKey,
        string endKey,
        string label,
        ICollection<string> errors,
        out DateOnly start,
        out DateOnly end)
    {
        var validStart = DateOnly.TryParseExact(values.GetValueOrDefault(startKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
        var validEnd = DateOnly.TryParseExact(values.GetValueOrDefault(endKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end);
        if (!validStart || !validEnd) return false;
        if (end > start) return true;
        errors.Add($"{label} end date must be after its start date.");
        return false;
    }

    private static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static int Integer(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    private static decimal Decimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}
