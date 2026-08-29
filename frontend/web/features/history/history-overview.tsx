"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { recordApi } from "@/features/record/record-api";
import type { OperationalRecord } from "@/features/record/record-types";
import { historyApi } from "./history-api";
import type { RecordItem } from "./history-types";

const historyAreas = [
  { icon: "users", code: "StudentCode", title: "Student History", detail: "Attendance, course grades, and semester result together", path: "/records/students" },
  { icon: "teacher", code: "TeacherCode", title: "Teacher History", detail: "Teacher attendance and completed-class evidence together", path: "/records/teachers" },
  { icon: "calendar", code: "ClassSessionRecordCode", title: "Class Sessions", detail: "Frozen attendance for every completed enrolled timetable", path: "/records/class-sessions" },
  { icon: "book", code: "CourseCode", title: "Course History", detail: "Course lifecycle and assignment snapshots", path: "/records/courses" },
  { icon: "room", code: "ClassroomCode", title: "Classroom History", detail: "Learning-space lifecycle and capacity snapshots", path: "/records/classrooms" },
  { icon: "calendar", code: "TimetableCode", title: "Timetable History", detail: "Enrolled schedule lifecycle and time snapshots", path: "/records/timetable" },
  { icon: "building", code: "DepartmentCode", title: "Department History", detail: "Department leadership and organization snapshots", path: "/records/departments" },
  { icon: "grade", code: "StudentCode", title: "Result Semester", detail: "Final student semester outcome across five courses", path: "/records/result-semester" },
] as const;

export function HistoryOverview() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const selectedPeriod = searchParams.get("period") ?? "all";
  const [students, setStudents] = useState<OperationalRecord[]>();
  const [teachers, setTeachers] = useState<OperationalRecord[]>();
  const [audit, setAudit] = useState<RecordItem[]>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const [studentRows, teacherRows, auditRows] = await Promise.all([
        recordApi.get("students", "", departmentId, true),
        recordApi.get("teachers", "", departmentId, true),
        historyApi.get("", "all"),
      ]);
      setStudents(studentRows); setTeachers(teacherRows); setAudit(auditRows); setError(false);
    } catch { setError(true); }
  }, [departmentId]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 0); return () => window.clearTimeout(timer); }, [load]);

  const periods = useMemo(() => buildPeriods(students ?? [], teachers ?? []), [students, teachers]);
  if (error) return <ErrorPage retry={load}/>;
  if (!students || !teachers || !audit) return <LoadingPage/>;

  const inPeriod = (row: OperationalRecord) => selectedPeriod === "all" || `${row.academicYear}|${row.term}` === selectedPeriod;
  const inYear = (row: OperationalRecord) => !year || JSON.stringify(row.activities).toLowerCase().includes(`year ${year}`) || row.identifier.toLowerCase().includes(`year ${year}`);
  const visibleStudents = students.filter(row => inPeriod(row) && inYear(row));
  const visibleTeachers = teachers.filter(row => inPeriod(row) && inYear(row));
  const attendanceCount = visibleStudents.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Class attendance").length, 0);
  const gradeCount = visibleStudents.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Course grade").length, 0);

  return <div className="viewport-data-page history-control-overview-page">
    <PageHeading eyebrow="Semester history control center" title="History Overview" description="Choose a semester, then open visual Student or Teacher History where attendance and grades are kept with the person they belong to."/>
    <section className="record-semester-switcher panel"><div><span>Semester view</span><strong>{selectedPeriod === "all" ? "All semesters" : periods.find(period => period.key === selectedPeriod)?.label ?? "Selected semester"}</strong></div><nav><button type="button" className={selectedPeriod === "all" ? "active" : ""} onClick={() => changePeriod("all")}>All semesters</button>{periods.map(period => <button type="button" className={selectedPeriod === period.key ? "active" : ""} onClick={() => changePeriod(period.key)} key={period.key}>{period.label}</button>)}</nav></section>
    <div className="history-control-overview-scroll">
      <section className="enrollment-overview-metrics">
        <HistoryMetric icon="users" label="Student semester records" value={visibleStudents.length} detail={`${attendanceCount} attendance events`} href={scopedHref("/records/students", departmentId, year, selectedPeriod)}/>
        <HistoryMetric icon="teacher" label="Teacher semester records" value={visibleTeachers.length} detail="Attendance and completed classes" href={scopedHref("/records/teachers", departmentId, year, selectedPeriod)}/>
        <HistoryMetric icon="grade" label="Student course grades" value={gradeCount} detail="Stored inside Student History" href={scopedHref("/records/students", departmentId, year, selectedPeriod)}/>
        <HistoryMetric icon="archive" label="Permanent snapshots" value={audit.length} detail="Management and enrollment lifecycle" href={scopedHref("/records/class-sessions", departmentId, year, selectedPeriod)}/>
      </section>

      <section className="panel history-data-map">
        <header><div><span>Visual data ownership</span><h2>Open history by the record that owns the data</h2><p>Attendance is no longer a separate sidebar destination. Student attendance and grades live in Student History; teacher attendance lives in Teacher History.</p></div></header>
        <div>{historyAreas.map(area => <Link href={scopedHref(area.path, departmentId, year, selectedPeriod)} key={area.title}><span><Icon name={area.icon} size={17}/></span><div><small>{area.code}</small><strong>{area.title}</strong><p>{area.detail}</p></div><Icon name="arrow" size={14}/></Link>)}</div>
      </section>

      <section className="panel history-semester-coverage">
        <header><div><span>Semester comparison</span><h2>Recorded data by semester</h2></div></header>
        <div className="history-semester-table"><div className="history-semester-head"><span>Academic year</span><span>Semester</span><span>Students</span><span>Teachers</span><span>Attendance</span><span>Grades</span><span>Open</span></div>{periods.map(period => <article className="history-semester-row" key={period.key}><strong>{period.academicYear}</strong><span>{period.term}</span><b>{period.students}</b><b>{period.teachers}</b><span>{period.attendance}</span><span>{period.grades}</span><button type="button" onClick={() => changePeriod(period.key)}>View <Icon name="arrow" size={12}/></button></article>)}{!periods.length && <div className="empty-state"><strong>No semester history yet</strong><span>Completed enrolled classes and grades will create the first semester view.</span></div>}</div>
      </section>
    </div>
  </div>;

  function changePeriod(period: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (period === "all") params.delete("period"); else params.set("period", period);
    router.replace(`/records/overview${params.size ? `?${params}` : ""}`, { scroll: false });
  }
}

function HistoryMetric({ icon, label, value, detail, href }: { icon: Parameters<typeof Icon>[0]["name"]; label: string; value: number; detail: string; href: string }) {
  return <Link className="panel enrollment-overview-metric" href={href}><span className="complete"><Icon name={icon} size={17}/></span><div><small>{label}</small><strong>{value.toLocaleString()}</strong><p>{detail}</p></div><Icon name="arrow" size={14}/></Link>;
}

function buildPeriods(students: OperationalRecord[], teachers: OperationalRecord[]) {
  const keys = new Set([...students, ...teachers].filter(row => row.academicYear && row.term).map(row => `${row.academicYear}|${row.term}`));
  return [...keys].map(key => {
    const [academicYear, term] = key.split("|");
    const studentRows = students.filter(row => row.academicYear === academicYear && row.term === term);
    const teacherRows = teachers.filter(row => row.academicYear === academicYear && row.term === term);
    return {
      key, academicYear, term, label: `${academicYear} · ${term}`,
      students: studentRows.length,
      teachers: teacherRows.length,
      attendance: studentRows.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Class attendance").length, 0),
      grades: studentRows.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Course grade").length, 0),
    };
  }).toSorted((left, right) => right.key.localeCompare(left.key, undefined, { numeric: true }));
}

function scopedHref(pathname: string, departmentId: string, year: string, period: string) {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  if (period !== "all") params.set("period", period);
  return `${pathname}${params.size ? `?${params}` : ""}`;
}
