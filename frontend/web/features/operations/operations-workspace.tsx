"use client";

import { useParams, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, MetricCards, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { OperationPanel } from "./components/operation-panel";
import { operationsApi } from "./operations-api";
import type { Operation } from "./operations-types";

export default function OperationsWorkspace() {
  const { settings } = useInstituteSettings();
  const { module: routeModule } = useParams<{ module: string }>();
  const operationModule = routeModule === "overview" ? "dashboard" : routeModule;
  const searchParams = useSearchParams();
  const sidebarDepartmentId = searchParams.get("departmentId") ?? "";
  const timetable = operationModule === "timetable";
  const year = timetable ? 0 : Number(searchParams.get("year") ?? 0);
  const [data, setData] = useState<Operation>();
  const [error, setError] = useState(false);
  const departmentId = sidebarDepartmentId;
  const load = useCallback(async () => {
    try {
      let value = await operationsApi.get(operationModule, departmentId);
      if (operationModule === "dashboard") {
        const [students, teachers, classrooms, courses, timetableData] = await Promise.all(["students", "teachers", "classrooms", "courses", "timetable"].map(area => operationsApi.get(area, departmentId)));
        value = { ...value, students: students.students, teachers: teachers.teachers, classrooms: classrooms.classrooms, courses: courses.courses, weeklySchedule: timetableData.weeklySchedule };
      }
      const [studentsData, timetableData] = await Promise.all([
        operationModule === "students" ? Promise.resolve(value) : operationsApi.get("students", departmentId),
        operationModule === "timetable" ? Promise.resolve(value) : operationsApi.get("timetable", departmentId),
      ]);
      if (year) {
        value = filterOperationYear(value, year, studentsData, timetableData);
      }
      setData(sortOperationByYear(value, studentsData, timetableData));
      setError(false);
    } catch { setError(true); }
  }, [operationModule, departmentId, year]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  const refreshSeconds = Math.max(5, Number(settings.system.autoRefreshSeconds) || 30);
  useEffect(() => { const timer = window.setInterval(() => void load(), refreshSeconds * 1000); return () => window.clearInterval(timer); }, [load, refreshSeconds]);
  if (error) return <ErrorPage retry={load}/>;
  if (!data) return <LoadingPage/>;

  const dashboard = data.module === "dashboard";
  const visual = data.module === "classrooms" || data.module === "timetable";
  return <div className={`viewport-data-page operations-workspace ${visual ? "operations-visual-workspace" : ""}`}>
    <PageHeading eyebrow={dashboard ? "Institute operations" : "Live operation"} title={dashboard ? "Operation Overview" : data.title} description={`${data.description}${year ? ` Showing Year ${year}.` : ""}`} actions={<button className="button primary" onClick={load}><Icon name={dashboard ? "dashboard" : "pulse"} size={16}/>Refresh</button>}/>
    {!dashboard && !visual && <MetricCards metrics={data.metrics}/>} 
    <OperationPanel data={data} departmentId={departmentId} year={year} className={dashboard ? "operation-dashboard-page" : visual ? "operation-visual-page" : "operation-standard-page"} kicker={dashboard ? "Enrollment-powered institute operations" : timetable ? "Enrolled weekly schedule" : visual ? "Enrollment-derived whole view" : "Live enrollment-derived data"}/>
  </div>;
}

function filterOperationYear(data: Operation, year: number, studentsData: Operation, timetableData: Operation): Operation {
  const students = (studentsData.students ?? []).filter(student => student.year === year);
  const studentNames = new Set(students.map(student => student.student));
  const departments = new Set(students.map(student => student.department));
  const schedule = (timetableData.weeklySchedule ?? []).filter(entry => entry.yearLevel === year);
  const teachers = new Set(schedule.map(entry => entry.teacher));
  const courses = new Set(schedule.map(entry => entry.course));
  return {
    ...data,
    students: (data.students ?? []).filter(student => student.year === year),
    teachers: (data.teachers ?? []).filter(teacher => teachers.has(teacher.teacher)),
    courses: (data.courses ?? []).filter(course => courses.has(course.course)),
    weeklySchedule: (data.weeklySchedule ?? []).filter(entry => entry.yearLevel === year),
    attendance: (data.attendance ?? []).filter(entry => studentNames.has(entry.student)),
    grades: (data.grades ?? []).filter(entry => studentNames.has(entry.student)),
    departments: (data.departments ?? []).filter(entry => departments.has(entry.department)),
  };
}

function sortOperationByYear(data: Operation, studentsData: Operation, timetableData: Operation): Operation {
  const students = studentsData.students ?? [];
  const schedule = timetableData.weeklySchedule ?? [];
  const studentYears = new Map(students.map(student => [student.student, student.year]));
  const departmentYears = new Map<string, number>();
  for (const student of students) departmentYears.set(student.department, Math.min(departmentYears.get(student.department) ?? 99, student.year));
  const scheduledYear = (field: "teacher" | "course" | "room", value: string) => schedule.filter(entry => entry[field] === value).reduce((minimum, entry) => Math.min(minimum, entry.yearLevel), 99);
  return {
    ...data,
    students: data.students?.toSorted((left, right) => attendancePriority(left.attendanceStatus) - attendancePriority(right.attendanceStatus) || left.year - right.year || left.studentCode.localeCompare(right.studentCode, undefined, { numeric: true })),
    teachers: data.teachers?.toSorted((left, right) => runningPriority(left.status) - runningPriority(right.status) || scheduledYear("teacher", left.teacher) - scheduledYear("teacher", right.teacher) || left.teacherCode.localeCompare(right.teacherCode, undefined, { numeric: true })),
    courses: data.courses?.toSorted((left, right) => runningPriority(left.status) - runningPriority(right.status) || scheduledYear("course", left.course) - scheduledYear("course", right.course) || left.courseCode.localeCompare(right.courseCode, undefined, { numeric: true })),
    weeklySchedule: data.weeklySchedule?.toSorted((left, right) => left.yearLevel - right.yearLevel),
    attendance: data.attendance?.toSorted((left, right) => (studentYears.get(left.student) ?? 99) - (studentYears.get(right.student) ?? 99)),
    grades: data.grades?.toSorted((left, right) => (studentYears.get(left.student) ?? 99) - (studentYears.get(right.student) ?? 99)),
    departments: data.departments?.toSorted((left, right) => (departmentYears.get(left.department) ?? 99) - (departmentYears.get(right.department) ?? 99)),
  };
}
function runningPriority(status: string) { return status === "Running" ? 0 : status === "Available" ? 1 : 2; }
function attendancePriority(status: string) { return status === "Present" ? 0 : status === "Permission" ? 1 : status === "Absent" ? 2 : 3; }
