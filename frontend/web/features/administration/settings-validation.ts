import { editableSectionFields } from "./administration-config";
import type { SettingFieldDefinition, SettingSection } from "./administration-types";
import { parseCsv } from "./settings-codec";

export function validateSettings(section: SettingSection, values: Record<string, string>) {
  const errors: string[] = [];
  for (const definition of editableSectionFields(section)) validateField(section, definition, values, errors);

  if (section === "academic-year") validateDateWindow(values.startsOn, values.endsOn, "Academic year", errors);
  if (section === "semester") validateTerms(values, errors);
  if (section === "attendance-rules") validateAttendance(values, errors);
  if (section === "grade-rules") validateGrades(values, errors);
  if (section === "notifications") validateNotifications(values, errors);
  if (section === "security" && parseCsv(values.twoFactorMethods).length === 0) errors.push("Select at least one two-factor authentication method.");

  return [...new Set(errors)];
}

function validateField(section: SettingSection, definition: SettingFieldDefinition, values: Record<string, string>, errors: string[]) {
  const raw = values[definition.key] ?? "";
  const value = raw.trim();
  const optionalDeliveryField = section === "notifications" && ((definition.key.startsWith("smtp") || definition.key.startsWith("sender") || definition.key === "emailEncryption") && values.emailEnabled !== "true");
  if (definition.required && !optionalDeliveryField && !value) errors.push(`${definition.label} is required.`);
  if (!value) return;

  if (definition.type === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) errors.push(`${definition.label} must be a valid email address.`);
  if (definition.type === "url" && !isUrl(value)) errors.push(`${definition.label} must be a complete http:// or https:// URL.`);
  if (definition.type === "number") validateNumber(definition, value, errors);
  if (definition.type === "select" && definition.options && !definition.options.some(option => option.value === value)) errors.push(`${definition.label} must use an available option.`);
  if (definition.type === "multiselect" || definition.type === "checklist") {
    const selected = parseCsv(value);
    if (definition.required && selected.length === 0) errors.push(`Select at least one option for ${definition.label}.`);
    const available = new Set(definition.options?.map(option => option.value) ?? []);
    if (available.size && selected.some(item => !available.has(item))) errors.push(`${definition.label} contains an unavailable option.`);
  }
  if (definition.type === "asset" && value && !isAssetLocation(value)) errors.push(`${definition.label} must be an application path or an http:// or https:// URL.`);
  if ((["idPrefix", "codePrefix"].includes(definition.key) || definition.key.endsWith("CodePrefix")) && !/^[A-Za-z0-9_-]+$/.test(value)) errors.push(`${definition.label} may use only letters, numbers, underscores, and hyphens.`);
}

function validateNumber(definition: SettingFieldDefinition, raw: string, errors: string[]) {
  const value = Number(raw);
  if (!Number.isFinite(value)) { errors.push(`${definition.label} must be a number.`); return; }
  if (definition.step === undefined && !Number.isInteger(value)) errors.push(`${definition.label} must be a whole number.`);
  if (definition.min !== undefined && value < definition.min) errors.push(`${definition.label} must be at least ${definition.min}.`);
  if (definition.max !== undefined && value > definition.max) errors.push(`${definition.label} must not exceed ${definition.max}.`);
}

function validateTerms(values: Record<string, string>, errors: string[]) {
  const terms = [
    ["Semester 1", "semester1StartsOn", "semester1EndsOn"],
    ["Semester 2", "semester2StartsOn", "semester2EndsOn"],
    ["Summer Term", "summerStartsOn", "summerEndsOn"],
  ] as const;
  for (const [name, start, end] of terms) validateDateWindow(values[start], values[end], name, errors);
  const ordered = terms.flatMap(([, start, end]) => [Date.parse(values[start]), Date.parse(values[end])]);
  if (!ordered.some(Number.isNaN) && ordered.some((value, index) => index > 0 && value <= ordered[index - 1])) errors.push("Term dates must be ordered from Semester 1 through Summer Term without overlap.");
}

function validateAttendance(values: Record<string, string>, errors: string[]) {
  const thresholds = ["onTimeThresholdMinutes", "lateThresholdMinutes", "veryLateThresholdMinutes", "absentAfterMinutes"].map(key => Number(values[key]));
  if (thresholds.every(Number.isFinite) && !(thresholds[0] < thresholds[1] && thresholds[1] <= thresholds[2] && thresholds[2] <= thresholds[3])) {
    errors.push("Attendance thresholds must progress from On Time to Late, Very Late, and Absent.");
  }
}

function validateGrades(values: Record<string, string>, errors: string[]) {
  const thresholdKeys = ["aPlusMinimum", "aMinimum", "bPlusMinimum", "bMinimum", "cPlusMinimum", "cMinimum", "dMinimum"];
  const thresholds = thresholdKeys.map(key => Number(values[key]));
  if (thresholds.every(Number.isFinite) && thresholds.some((value, index) => index > 0 && value >= thresholds[index - 1])) errors.push("Grade boundaries must descend from A+ through D without equal values.");
  const minimum = Number(values.minimumScore);
  const maximum = Number(values.maximumScore);
  if (Number.isFinite(minimum) && Number.isFinite(maximum) && maximum <= minimum) errors.push("Maximum score must be greater than minimum score.");
  const scale = Number(values.gpaScale || values.maximumGpa);
  const points = ["aPlusGpa", "aGpa", "bPlusGpa", "bGpa", "cPlusGpa", "cGpa", "dGpa", "fGpa"].map(key => Number(values[key]));
  if (Number.isFinite(scale) && points.some(value => Number.isFinite(value) && value > scale)) errors.push("Grade GPA points cannot exceed the configured GPA scale.");
}

function validateNotifications(values: Record<string, string>, errors: string[]) {
  if (values.emailEnabled === "true") {
    for (const [key, label] of [["smtpHost", "SMTP host"], ["smtpPort", "SMTP port"], ["senderName", "Sender name"], ["senderEmail", "Sender email"]] as const) if (!values[key]?.trim()) errors.push(`${label} is required while email is enabled.`);
  }
  if (values.smsEnabled === "true" && (!values.smsProvider || values.smsProvider === "None")) errors.push("Choose an SMS provider while SMS is enabled.");
}

function validateDateWindow(start: string, end: string, name: string, errors: string[]) {
  const startTime = Date.parse(start);
  const endTime = Date.parse(end);
  if (Number.isNaN(startTime) || Number.isNaN(endTime)) errors.push(`${name} start and end dates must be valid.`);
  else if (endTime <= startTime) errors.push(`${name} end date must be after its start date.`);
}

function isUrl(value: string) {
  try { const url = new URL(value); return url.protocol === "http:" || url.protocol === "https:"; }
  catch { return false; }
}

function isAssetLocation(value: string) {
  return value.startsWith("/") || isUrl(value);
}
