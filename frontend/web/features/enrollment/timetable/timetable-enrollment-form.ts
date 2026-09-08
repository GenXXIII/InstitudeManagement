import type { EnrollmentField } from "../common/enrollment-field";
import type { EnrollmentItem } from "../common/enrollment-types";

export function timetableEnrollmentFields(courses: EnrollmentItem[], teachers: EnrollmentItem[], classrooms: EnrollmentItem[], semester: string): EnrollmentField[] {
  const availableCourses = courses.filter(item => item.values.status === "Active" && item.values.semester === semester);
  const availableTeachers = teachers.filter(item => item.values.status !== "Inactive");
  const availableClassrooms = classrooms.filter(item => item.values.status === "Available");
  return [
    { key: "courseId", label: "Course (Management)", type: "select", options: availableCourses.map(item => ({ id: item.id, label: `${item.values.courseCode} - ${item.values.name} · Year ${item.values.yearLevel} · ${item.values.semester}` })), required: true },
    { key: "teacherId", label: "Teacher (Management)", type: "select", options: availableTeachers.map(item => ({ id: item.id, label: `${item.values.teacherCode} - ${item.values.name}` })), required: true },
    { key: "classroomId", label: "Classroom (Management)", type: "select", options: availableClassrooms.map(item => ({ id: item.id, label: `${item.values.classroomCode} - ${item.values.building} · ${item.values.roomType}` })), required: true },
    { key: "yearLevel", label: "Student year (from course)", type: "text", required: true, readOnly: true },
    { key: "semester", label: "Semester (from course)", type: "text", required: true, readOnly: true },
  ];
}

export function timetableEnrollmentDefaults(year: string, semester = "Semester 1"): Record<string, string> {
  return { courseId: "", teacherId: "", classroomId: "", yearLevel: year || "1", semester, status: "Active" };
}
