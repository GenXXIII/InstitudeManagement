const labels: Record<string, string> = {
  name: "Institute name", shortName: "Short name", email: "Email", phone: "Phone", address: "Address",
  currentYear: "Current year", currentTerm: "Semester", startsOn: "Start date", endsOn: "End date",
  semester1StartsOn: "Semester 1 start", semester1EndsOn: "Semester 1 end",
  semester2StartsOn: "Semester 2 start", semester2EndsOn: "Semester 2 end",
  defaultCapacity: "Default capacity", lateThresholdMinutes: "Late threshold",
  timeZone: "Time zone", language: "Language", dateFormat: "Date format", autoRefreshSeconds: "Auto refresh",
};

export function validateSettings(section: string, values: Record<string, string>) {
  const errors: string[] = [];
  const requiredBySection: Record<string, string[]> = {
    institute: ["name", "shortName", "email", "phone", "address"],
    "academic-year": ["currentYear", "startsOn", "endsOn"],
    semester: ["currentTerm", "startsOn", "endsOn", "semester1StartsOn", "semester1EndsOn", "semester2StartsOn", "semester2EndsOn"],
    departments: ["defaultStatus"],
    courses: ["defaultCapacity"],
    classrooms: ["defaultCapacity"],
    "attendance-rules": ["method", "lateThresholdMinutes"],
    "grade-rules": ["aMinimum", "bMinimum", "cMinimum", "dMinimum", "eMinimum"],
    system: ["timeZone", "language", "dateFormat", "autoRefreshSeconds"],
  };
  for (const key of requiredBySection[section] ?? []) if (!values[key]?.trim()) errors.push(`${label(key)} is required.`);

  if (values.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) errors.push("Email must be a valid email address.");
  if (values.shortName?.length > 20) errors.push("Short name must not exceed 20 characters.");

  if (section === "academic-year") validateDateWindow(values.startsOn, values.endsOn, "Academic year", errors);
  if (section === "semester") {
    validateDateWindow(values.startsOn, values.endsOn, "Active semester", errors);
    const dates = ["semester1StartsOn", "semester1EndsOn", "semester2StartsOn", "semester2EndsOn"].map(key => Date.parse(values[key]));
    if (dates.some(Number.isNaN)) errors.push("All Semester 1 and Semester 2 dates must be valid.");
    else if (!(dates[0] < dates[1] && dates[1] < dates[2] && dates[2] < dates[3])) errors.push("Semester dates must be ordered from Semester 1 start through Semester 2 end.");
  }
  if (section === "courses" || section === "classrooms") integerRange(values.defaultCapacity, 1, 10000, "Default capacity", errors);
  if (section === "attendance-rules") integerRange(values.lateThresholdMinutes, 0, 1440, "Late threshold", errors);
  if (section === "system") integerRange(values.autoRefreshSeconds, 5, 3600, "Auto refresh seconds", errors);
  if (section === "grade-rules") {
    const grades = ["aMinimum", "bMinimum", "cMinimum", "dMinimum", "eMinimum"].map(key => Number(values[key]));
    if (grades.some(value => !Number.isFinite(value) || value < 0 || value > 100)) errors.push("Every grade boundary must be a number from 0 to 100.");
    else if (!(grades[0] > grades[1] && grades[1] > grades[2] && grades[2] > grades[3] && grades[3] > grades[4])) errors.push("Grade boundaries must descend from A through E without equal values.");
  }
  return [...new Set(errors)];
}

function validateDateWindow(start: string, end: string, name: string, errors: string[]) {
  const startTime = Date.parse(start);
  const endTime = Date.parse(end);
  if (Number.isNaN(startTime) || Number.isNaN(endTime)) errors.push(`${name} start and end dates must be valid.`);
  else if (endTime <= startTime) errors.push(`${name} end date must be after its start date.`);
}

function integerRange(raw: string, minimum: number, maximum: number, name: string, errors: string[]) {
  const value = Number(raw);
  if (!Number.isInteger(value) || value < minimum || value > maximum) errors.push(`${name} must be a whole number from ${minimum} to ${maximum.toLocaleString()}.`);
}

function label(key: string) {
  return labels[key] ?? key.replace(/([A-Z])/g, " $1").replace(/^./, first => first.toUpperCase());
}
