import type { Field, ManagementModule, References } from "./management-types";

export const emptyReferences: References = { departments: [], teachers: [], students: [], classrooms: [], courses: [] };
export const managementCopy: Record<ManagementModule, { title: string; description: string; singular: string }> = {
  overview: { title: "Institute management", description: "Manage current institute data through connected department workspaces.", singular: "item" },
  students: { title: "Student management", description: "Enroll students, maintain 4×6 profile photos, and assign each learner to a department.", singular: "student" },
  teachers: { title: "Teacher management", description: "Maintain faculty profiles, availability, department membership, and leadership eligibility.", singular: "teacher" },
  classrooms: { title: "Classroom management", description: "Assign rooms to departments and manage capacity, availability, and device state.", singular: "classroom" },
  courses: { title: "Course management", description: "Connect courses to departments and eligible teachers with capacity and credit rules.", singular: "course" },
  timetable: { title: "Timetable planner", description: "Schedule a department's course, teacher, and classroom together without broken relationships.", singular: "class" },
  attendance: { title: "Attendance management", description: "Record and correct current attendance while preserving each change in immutable history.", singular: "attendance entry" },
  departments: { title: "Department management", description: "Organize academic units and appoint an existing teacher as each department head.", singular: "department" },
  grades: { title: "Grade management", description: "Maintain department gradebooks by connecting students to courses and terms.", singular: "grade" },
};

export const managementFields: Record<Exclude<ManagementModule, "overview">, Field[]> = {
  students: [{ key: "photoDataUrl", label: "4×6 student photo", type: "photo", required: true }, { key: "number", label: "Student ID", required: true }, { key: "name", label: "Full name", required: true }, { key: "email", label: "Email", type: "email", required: true }, { key: "departmentId", label: "Department", type: "select", source: "departments", required: true }, { key: "year", label: "Year level", type: "number", required: true }, { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true }],
  teachers: [{ key: "photoDataUrl", label: "4×6 teacher photo", type: "photo", required: true }, { key: "number", label: "Teacher ID", required: true }, { key: "name", label: "Full name", required: true }, { key: "email", label: "Email", type: "email", required: true }, { key: "departmentId", label: "Department", type: "select", source: "departments", required: true }, { key: "status", label: "Work status", type: "select", options: ["Available", "Teaching", "Meeting", "On leave", "Inactive"], required: true }],
  classrooms: [{ key: "code", label: "Room code", required: true }, { key: "building", label: "Building", required: true }, { key: "departmentId", label: "Department", type: "select", source: "departments", required: true }, { key: "capacity", label: "Capacity", type: "number", required: true }, { key: "status", label: "Room status", type: "select", options: ["Available", "Running", "Starting", "Offline", "Inactive"], required: true }, { key: "deviceOnline", label: "Attendance device online", type: "checkbox" }],
  courses: [{ key: "code", label: "Course code", required: true }, { key: "name", label: "Course name", required: true }, { key: "departmentId", label: "Department", type: "select", source: "departments", required: true }, { key: "teacherId", label: "Assigned teacher", type: "select", source: "teachers", required: true }, { key: "credits", label: "Credits", type: "number", required: true }, { key: "capacity", label: "Student capacity", type: "number", required: true }, { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true }],
  timetable: [{ key: "courseId", label: "Course", type: "select", source: "courses", required: true }, { key: "teacherId", label: "Teacher", type: "select", source: "teachers", required: true }, { key: "classroomId", label: "Classroom", type: "select", source: "classrooms", required: true }, { key: "dayOfWeek", label: "Day", type: "select", options: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], required: true }, { key: "startsAt", label: "Starts at", type: "time", required: true }, { key: "endsAt", label: "Ends at", type: "time", required: true }, { key: "status", label: "Status", type: "select", options: ["Upcoming", "Running", "Completed", "Cancelled"], required: true }],
  attendance: [{ key: "studentId", label: "Student", type: "select", source: "students", required: true }, { key: "date", label: "Attendance date", type: "date", required: true }, { key: "checkedInAt", label: "Check-in time", type: "time" }, { key: "status", label: "Status", type: "select", options: ["Present", "Late", "Absent", "Excused"], required: true }, { key: "method", label: "Method", type: "select", options: ["ID Card", "Manual", "QR Code", "Biometric"], required: true }],
  departments: [{ key: "code", label: "Department code", required: true }, { key: "name", label: "Department name", required: true }, { key: "headTeacherId", label: "Head of department", type: "select", source: "teachers", required: true }, { key: "status", label: "Status", type: "select", options: ["Active", "Inactive"], required: true }],
  grades: [{ key: "studentId", label: "Student", type: "select", source: "students", required: true }, { key: "courseId", label: "Course", type: "select", source: "courses", required: true }, { key: "score", label: "Score", type: "number", required: true }, { key: "term", label: "Semester / term", type: "select", options: ["Semester 1", "Semester 2", "Summer"], required: true }],
};

export function moduleDefaults(module: Exclude<ManagementModule, "overview">, departmentId: string): Record<string, string> {
  const base = { departmentId };
  if (module === "students") return { ...base, year: "1", status: "Active" };
  if (module === "teachers") return { ...base, status: "Available" };
  if (module === "classrooms") return { ...base, capacity: "40", status: "Available", deviceOnline: "true" };
  if (module === "courses") return { ...base, credits: "3", capacity: "40", status: "Active" };
  if (module === "timetable") return { ...base, dayOfWeek: "Monday", startsAt: "08:00", endsAt: "09:00", status: "Upcoming" };
  if (module === "attendance") return { ...base, date: new Date().toISOString().slice(0, 10), status: "Present", method: "ID Card" };
  if (module === "departments") return { status: "Active" };
  return { ...base, term: "Semester 1", score: "80" };
}
