"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { classroomApi } from "@/features/management/classrooms/classroom-api";
import { courseApi } from "@/features/management/courses/course-api";
import { departmentApi } from "@/features/management/departments/department-api";
import { studentApi } from "@/features/management/students/student-api";
import { teacherApi } from "@/features/management/teachers/teacher-api";
import { timetableApi } from "@/features/timetable/timetable-api";
import { classroomAssignmentApi } from "./classrooms/classroom-assignment-api";
import { courseAssignmentApi } from "./courses/course-assignment-api";
import { studentEnrollmentApi } from "./students/student-enrollment-api";
import { teacherAssignmentApi } from "./teachers/teacher-assignment-api";
import { timetableEnrollmentApi } from "./timetable/timetable-enrollment-api";
import { buildEnrollmentOverview, scopedEnrollmentHref, type EnrollmentOverviewData } from "./enrollment-overview-model";

const maintenanceRows = [
  { icon: "users", source: "Students", management: "students", assignment: "Student Enrollment", enrollment: "students", result: "Student Assign", resultPath: "student-assignments", rule: "Department + year + shift" },
  { icon: "teacher", source: "Teachers", management: "teachers", assignment: "Teacher Assign", enrollment: "teachers", result: "Timetable Enrollment", resultPath: "timetable", rule: "Department assignment" },
  { icon: "book", source: "Courses", management: "courses", assignment: "Course Assign", enrollment: "courses", result: "Timetable Enrollment", resultPath: "timetable", rule: "Department + year + teacher" },
  { icon: "room", source: "Classrooms", management: "classrooms", assignment: "Classroom Assign", enrollment: "classrooms", result: "Timetable Enrollment", resultPath: "timetable", rule: "Access + capacity" },
  { icon: "calendar", source: "Schedule", management: "timetable", assignment: "Timetable Enrollment", enrollment: "timetable", result: "Student Assign", resultPath: "student-assignments", rule: "Course + teacher + room + time" },
  { icon: "building", source: "Departments", management: "departments", assignment: "Department Assign", enrollment: "departments", result: "Student Assign", resultPath: "student-assignments", rule: "Department + selected year" },
] as const;

export function EnrollmentOverview() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [data, setData] = useState<EnrollmentOverviewData>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const [students, teachers, courses, classrooms, timetable, enrollmentStudents, enrollmentTeachers, enrollmentCourses, enrollmentClassrooms, enrollmentTimetable, departments] = await Promise.all([
        studentApi.get("", departmentId),
        teacherApi.get("", departmentId),
        courseApi.get("", departmentId),
        classroomApi.get("", departmentId),
        timetableApi.get("", departmentId),
        studentEnrollmentApi.get("", departmentId, year),
        teacherAssignmentApi.get("", departmentId, year),
        courseAssignmentApi.get("", departmentId, year),
        classroomAssignmentApi.get("", departmentId, year),
        timetableEnrollmentApi.get("", departmentId, year),
        departmentApi.get(),
      ]);
      setData({
        management: { students, teachers, courses, classrooms, timetable },
        enrollment: { students: enrollmentStudents, teachers: enrollmentTeachers, courses: enrollmentCourses, classrooms: enrollmentClassrooms, timetable: enrollmentTimetable },
        departments,
      });
      setError(false);
    } catch {
      setError(true);
    }
  }, [departmentId, year]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const view = useMemo(() => data ? buildEnrollmentOverview(data, year) : undefined, [data, year]);
  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>;
  if (!data || !view) return <LoadingPage/>;

  const selectedDepartment = data.departments.find(department => department.id === departmentId)?.values.name ?? "All departments";
  const scope = `${selectedDepartment} / ${year ? `Year ${year}` : "All years"}`;
  const attention = [
    { label: "Students waiting for enrollment", count: view.missingStudents, detail: "Add their department, year, and shift.", href: scopedEnrollmentHref("/enrollment/students", departmentId, year) },
    { label: "Courses waiting for assignment", count: view.missingCourses, detail: "Assign a department, year, and teacher.", href: scopedEnrollmentHref("/enrollment/courses", departmentId, year) },
    { label: "Management schedules not enrolled", count: view.missingTimetables, detail: "Enroll approved schedules for student use.", href: scopedEnrollmentHref("/enrollment/timetable", departmentId, year) },
    { label: "Cohorts missing course or time coverage", count: view.attentionCohorts, detail: "Complete the course and timetable chain.", href: scopedEnrollmentHref(view.firstAttentionPath, view.firstAttentionDepartmentId || departmentId, view.firstAttentionYear || year) },
  ];

  return <div className="viewport-data-page enrollment-overview-page">
    <PageHeading
      eyebrow="Academic enrollment control center"
      title="Enrollment Overview"
      description="Maintain Management source data, complete each enrollment assignment, then confirm what every student cohort receives."
    />
    <section className="enrollment-overview-scope panel">
      <div><span>Current scope</span><strong>{scope}</strong></div>
      <p>Student schedules connect automatically by <b>department + year + shift</b>. A Year 1 student only receives Year 1 courses assigned to that student&apos;s department, using timetable times that match the selected shift.</p>
    </section>
    <div className="enrollment-overview-scroll">
      <section className="enrollment-overview-metrics" aria-label="Enrollment completion">
        <OverviewMetric icon="users" label="Students enrolled" assigned={view.activeStudents.length} total={view.sourceStudents.length} href={scopedEnrollmentHref("/enrollment/students", departmentId, year)}/>
        <OverviewMetric icon="teacher" label="Teachers assigned" assigned={view.assignedTeachers.length} total={view.sourceTeachers.length} href={scopedEnrollmentHref("/enrollment/teachers", departmentId, year)}/>
        <OverviewMetric icon="book" label="Courses assigned" assigned={view.activeCourses.length} total={view.sourceCourses.length} href={scopedEnrollmentHref("/enrollment/courses", departmentId, year)}/>
        <OverviewMetric icon="room" label="Classrooms assigned" assigned={view.assignedClassrooms.length} total={view.sourceClassrooms.length} href={scopedEnrollmentHref("/enrollment/classrooms", departmentId, year)}/>
        <OverviewMetric icon="calendar" label="Schedules enrolled" assigned={view.enrolledTimetable.length} total={view.sourceTimetable.length} href={scopedEnrollmentHref("/enrollment/timetable", departmentId, year)}/>
      </section>

      <section className="enrollment-overview-main">
        <article className="panel enrollment-maintenance-map">
          <header><div><span>Maintain data in order</span><h2>Management → Assign → View result</h2></div><small>Open the step that needs work</small></header>
          <div className="enrollment-maintenance-list">
            {maintenanceRows.map(row => <div className="enrollment-maintenance-row" key={row.source}>
              <span className="enrollment-maintenance-icon"><Icon name={row.icon} size={16}/></span>
              <Link href={scopedEnrollmentHref(`/management/${row.management}`, departmentId, year)}><small>Management</small><strong>{row.source}</strong></Link>
              <Icon name="arrow" size={13}/>
              <Link href={scopedEnrollmentHref(`/enrollment/${row.enrollment}`, departmentId, year)}><small>Assign</small><strong>{row.assignment}</strong></Link>
              <Icon name="arrow" size={13}/>
              <Link href={scopedEnrollmentHref(`/enrollment/${row.resultPath}`, departmentId, year)}><small>View result</small><strong>{row.result}</strong></Link>
              <span className="enrollment-maintenance-rule">{row.rule}</span>
            </div>)}
          </div>
        </article>

        <article className="panel enrollment-attention-panel">
          <header><div><span>Automatic checks</span><h2>Needs attention</h2></div><strong>{attention.reduce((total, item) => total + item.count, 0)}</strong></header>
          <div className="enrollment-attention-list">
            {attention.map(item => <Link href={item.href} className={item.count ? "has-issue" : "is-ready"} key={item.label}>
              <span>{item.count ? item.count : <Icon name="check" size={15}/>}</span>
              <div><strong>{item.label}</strong><small>{item.count ? item.detail : "Complete for the current scope."}</small></div>
              <Icon name="arrow" size={14}/>
            </Link>)}
          </div>
        </article>
      </section>

      <section className="panel enrollment-cohort-panel">
        <header><div><span>Final relationship check</span><h2>Student cohort coverage</h2><p>Students in the same department, year, and shift automatically share the matching assigned courses and timetable.</p></div><Link className="button secondary" href={scopedEnrollmentHref("/enrollment/student-assignments", departmentId, year)}>View students <Icon name="arrow" size={14}/></Link></header>
        <div className="enrollment-cohort-table">
          <div className="enrollment-cohort-head"><span>Department</span><span>Year / shift</span><span>Students</span><span>Assigned courses</span><span>Matching classes</span><span>Coverage</span><span>Open</span></div>
          {view.cohorts.map(cohort => {
            const ready = cohort.courses > 0 && cohort.missingCourses === 0;
            const fixPath = cohort.courses ? "/enrollment/timetable" : "/enrollment/courses";
            return <article className="enrollment-cohort-row" key={cohort.key}>
              <strong>{cohort.department}</strong>
              <span>Year {cohort.year} / {cohort.shift}</span>
              <b>{cohort.students}</b>
              <span>{cohort.courses}</span>
              <span>{cohort.schedules}</span>
              <span className={`table-status ${ready ? "" : "watch"}`}>{ready ? "Ready" : cohort.courses ? `${cohort.missingCourses} course missing` : "Course needed"}</span>
              <Link href={scopedEnrollmentHref(ready ? "/enrollment/student-assignments" : fixPath, cohort.departmentId, cohort.year)}>{ready ? "View" : "Fix"}<Icon name="arrow" size={12}/></Link>
            </article>;
          })}
          {!view.cohorts.length && <div className="enrollment-overview-empty"><Icon name="users" size={22}/><strong>No enrolled student cohorts in this scope</strong><span>Start with Student Enrollment, then the cohort relationship will appear here.</span></div>}
        </div>
      </section>
    </div>
  </div>;
}

function OverviewMetric({ icon, label, assigned, total, href }: { icon: Parameters<typeof Icon>[0]["name"]; label: string; assigned: number; total: number; href: string }) {
  const complete = total > 0 && assigned >= total;
  return <Link className="panel enrollment-overview-metric" href={href}>
    <span className={complete ? "complete" : undefined}><Icon name={icon} size={17}/></span>
    <div><small>{label}</small><strong>{assigned}<em>/ {total}</em></strong><p>{complete ? "Complete" : `${Math.max(total - assigned, 0)} need attention`}</p></div>
    <Icon name="arrow" size={14}/>
  </Link>;
}
