import type { ConfigurationGroup, SettingFieldDefinition, SettingSection } from "../administration-types";
import { field, options } from "./schema-helpers";

export const policyGroups = {
  "attendance-rules": [
    {
      title: "Attendance workflow",
      description: "Core capture requirements and visibility rules.",
      fields: [
        field("method", "Default attendance method", "Preselected method for new attendance records.", "select", { required: true, options: options("Manual", "ID Card", "QR Code", "Biometric") }),
        field("attendanceRequired", "Attendance required", "Require attendance for scheduled classes.", "toggle"),
        field("checkInRequired", "Check-in required", "Require an arrival check-in.", "toggle"),
        field("checkOutRequired", "Check-out required", "Require a departure check-out.", "toggle"),
        field("teacherCanRecord", "Teacher can record attendance", "Allow teachers to capture class attendance.", "toggle"),
        field("studentCanView", "Student can view attendance", "Allow students to view their attendance record.", "toggle"),
      ],
    },
    {
      title: "Late and absent thresholds",
      description: "Minute thresholds are evaluated from the scheduled class start.",
      fields: [
        field("onTimeThresholdMinutes", "On-time maximum", "Latest minute still considered on time.", "number", { required: true, min: 0, max: 1440, unit: "minutes" }),
        field("lateThresholdMinutes", "Late starts at", "First minute classified as Late.", "number", { required: true, min: 0, max: 1440, unit: "minutes" }),
        field("veryLateThresholdMinutes", "Very Late starts at", "First minute classified as Very Late.", "number", { required: true, min: 0, max: 1440, unit: "minutes" }),
        field("absentAfterMinutes", "Absent after", "Minute boundary used to classify a missing student as Absent.", "number", { required: true, min: 0, max: 1440, unit: "minutes" }),
        field("autoAbsent", "Automatically mark absent", "Mark missing students Absent when a completed class is recorded.", "toggle"),
        field("autoPercentage", "Calculate attendance rates", "Show derived attendance percentages in dashboards and records.", "toggle"),
      ],
    },
    {
      title: "Excused absence",
      description: "Rules for approved absence requests.",
      fields: [
        field("excusedAbsenceEnabled", "Allow excused absence", "Enable the Excused or Permission state.", "toggle"),
        field("requireExcuseApproval", "Require excuse approval", "Require administrator approval for an excuse.", "toggle"),
        field("maximumExcusedAbsences", "Maximum per semester", "Maximum approved excused absences per student and semester.", "number", { required: true, min: 0, max: 365, unit: "absences" }),
      ],
    },
    {
      title: "Editing and audit",
      description: "Correction controls for active-period attendance records.",
      fields: [
        field("teacherCanEdit", "Teacher can edit", "Allow teachers to correct their attendance records.", "toggle"),
        field("allowCorrection", "Allow corrections", "Enable attendance correction workflows.", "toggle"),
        field("correctionPeriodDays", "Correction period", "Number of days after a class that corrections remain available.", "number", { required: true, min: 0, max: 365, unit: "days" }),
        field("requireCorrectionReason", "Require correction reason", "Require an explanation for each correction.", "toggle"),
        field("requireAdminApproval", "Require administrator approval", "Require approval before a correction becomes active.", "toggle"),
        field("keepChangeHistory", "Keep change history", "Preserve previous attendance values in Record History.", "toggle"),
      ],
    },
    {
      title: "Attendance alerts",
      description: "Existing operational notification effects retained by the application.",
      fields: [
        field("notifyTeacher", "Notify teacher", "Create teacher-facing attendance exception notifications.", "toggle"),
        field("notifyAdministrator", "Notify administrators", "Create administrator attendance exception notifications.", "toggle"),
      ],
    },
  ],
  "grade-rules": [
    {
      title: "Grading system",
      description: "Score range, pass mark, and GPA availability.",
      fields: [
        field("gradingSystem", "System", "Primary score and grade representation.", "select", { required: true, options: options("Percentage + Letter Grade") }),
        field("minimumScore", "Minimum score", "Lowest valid recorded score.", "number", { required: true, min: 0, max: 100, unit: "points" }),
        field("maximumScore", "Maximum score", "Highest valid recorded score.", "number", { required: true, min: 1, max: 1000, unit: "points" }),
        field("passMark", "Pass mark", "Overall score at which the institute considers a result passing.", "number", { required: true, min: 0, max: 100, unit: "percent" }),
        field("gpaEnabled", "Enable GPA", "Calculate GPA values alongside letter grades.", "toggle"),
        field("maximumGpa", "Maximum GPA", "Highest value on the GPA scale.", "number", { required: true, min: 0, max: 10, step: 0.01 }),
      ],
    },
    {
      title: "Grade scale",
      description: "Descending lower bounds support decimal scores without gaps. F is everything below the D minimum.",
      fields: [
        ...gradeBand("aPlus", "A+", "95", "4.00"),
        ...gradeBand("a", "A", "90", "4.00"),
        ...gradeBand("bPlus", "B+", "85", "3.50"),
        ...gradeBand("b", "B", "80", "3.00"),
        ...gradeBand("cPlus", "C+", "75", "2.50"),
        ...gradeBand("c", "C", "70", "2.00"),
        ...gradeBand("d", "D", "60", "1.00"),
        field("fRange", "F range", "Calculated from the minimum score and D lower bound.", "derived", { derive: values => `${values.minimumScore || 0} to below ${values.dMinimum || 60}` }),
        field("fGpa", "F GPA points", "GPA value assigned to F.", "number", { required: true, min: 0, max: 10, step: 0.01 }),
      ],
    },
    {
      title: "Pass rules",
      description: "Additional thresholds used by result publication workflows.",
      fields: [
        field("overallPassMark", "Overall pass mark", "Minimum overall percentage.", "number", { required: true, min: 0, max: 100, unit: "percent" }),
        field("coursePassMark", "Course pass mark", "Minimum percentage for an individual course.", "number", { required: true, min: 0, max: 100, unit: "percent" }),
        field("finalExamMinimum", "Final exam minimum", "Minimum final-exam percentage.", "number", { required: true, min: 0, max: 100, unit: "percent" }),
      ],
    },
    {
      title: "GPA rules",
      description: "Controls for GPA inclusion and rounding.",
      fields: [
        field("gpaScale", "GPA scale", "Maximum GPA used in calculations.", "number", { required: true, min: 0, max: 10, step: 0.01 }),
        field("includeFailedCourses", "Include failed courses", "Include failed course attempts in GPA calculations.", "toggle"),
        field("includeWithdrawnCourses", "Include withdrawn courses", "Include withdrawn courses in GPA calculations.", "toggle"),
        field("gpaDecimalPlaces", "Round GPA to", "Number of decimal places used for displayed GPA.", "number", { required: true, min: 0, max: 4, unit: "decimals" }),
      ],
    },
  ],
} satisfies Partial<Record<SettingSection, readonly ConfigurationGroup[]>>;

function gradeBand(key: string, label: string, threshold: string, gpa: string): SettingFieldDefinition[] {
  return [
    field(`${key}Minimum`, `${label} starts at`, `Lower score boundary for ${label}; sample value ${threshold}.`, "number", { required: true, min: 0, max: 100, step: 0.01, unit: "points" }),
    field(`${key}Gpa`, `${label} GPA points`, `GPA value assigned to ${label}; sample value ${gpa}.`, "number", { required: true, min: 0, max: 10, step: 0.01 }),
  ];
}
