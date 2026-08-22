import type { Field } from "./management-types";

export type FieldErrors = Record<string, string>;

const codeFields = new Set(["studentCode", "teacherCode", "departmentCode", "courseCode", "classroomCode", "timetableCode", "attendanceCode", "gradeCode"]);

export function validateManagementFields(fields: Field[], values: Record<string, string>, validOptions: Record<string, Set<string>> = {}): FieldErrors {
  const errors: FieldErrors = {};

  for (const field of fields) {
    const value = values[field.key]?.trim() ?? "";
    if (field.required && !value) {
      errors[field.key] = `${field.label} is required.`;
      continue;
    }
    if (!value) continue;

    if (field.type === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))
      errors[field.key] = `${field.label} must be a valid email address.`;
    else if (field.type === "number" && !Number.isFinite(Number(value)))
      errors[field.key] = `${field.label} must be a number.`;
    else if (field.type === "date" && !/^\d{4}-\d{2}-\d{2}$/.test(value))
      errors[field.key] = `${field.label} must be a valid date.`;
    else if (field.type === "time" && !/^([01]\d|2[0-3]):[0-5]\d$/.test(value))
      errors[field.key] = `${field.label} must be a valid 24-hour time.`;
    else if (field.type === "photo" && !/^data:image\/(png|jpeg|webp);base64,/i.test(value))
      errors[field.key] = `${field.label} must be a JPG, PNG, or WebP image.`;
    else if (field.type === "select" && validOptions[field.key]?.size && !validOptions[field.key].has(value))
      errors[field.key] = `Select a valid ${field.label.toLowerCase()} from the list.`;

    if (field.type !== "select" && codeFields.has(field.key) && (value.length > 64 || !/^[A-Za-z0-9][A-Za-z0-9._/-]*$/.test(value)))
      errors[field.key] = `${field.label} must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen.`;
    if (["name", "building"].includes(field.key) && value.length > 200)
      errors[field.key] = `${field.label} must not exceed 200 characters.`;
    if (field.key === "email" && value.length > 320)
      errors[field.key] = `${field.label} must not exceed 320 characters.`;
    if (field.key === "year" && (!Number.isInteger(Number(value)) || Number(value) < 1 || Number(value) > 4))
      errors[field.key] = "Year level must be a whole number from 1 to 4.";
    if (field.key === "capacity" && (!Number.isInteger(Number(value)) || Number(value) < 1 || Number(value) > 10000))
      errors[field.key] = "Capacity must be a whole number from 1 to 10,000.";
    if (field.key === "score" && (Number(value) < 0 || Number(value) > 100))
      errors[field.key] = "Score must be between 0 and 100.";
    if (field.key === "correctionReason" && value.length > 500)
      errors[field.key] = "Correction reason must not exceed 500 characters.";
  }

  return errors;
}

export function validationMessages(errors: FieldErrors, serverError = "") {
  return [...Object.values(errors), ...(serverError ? [serverError] : [])];
}
