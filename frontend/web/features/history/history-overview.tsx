"use client";

import Link from "next/link";
import { DataTable } from "@/components/data-table";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "@/features/finance/finance-api";
import { notificationHistoryApi } from "@/features/notifications/history/notification-history-api";
import { recordApi } from "@/features/record/record-api";
import type { OperationalRecord } from "@/features/record/record-types";
import { resultApi } from "@/features/results/result-api";
import { historyApi } from "./history-api";

const historyAreas = [
  { key: "students", icon: "users", title: "Student", detail: "Complete graduate stories from Year 1 through Year 4", path: "/records/students" },
  { key: "teachers", icon: "teacher", title: "Teacher", detail: "Closed-semester assignments, attendance, and classes", path: "/records/teachers" },
  { key: "courses", icon: "book", title: "Course", detail: "Course identity, enrollment, operations, and classes", path: "/records/courses" },
  { key: "classrooms", icon: "room", title: "Classroom", detail: "Learning-space assignments and completed operations", path: "/records/classrooms" },
  { key: "timetable", icon: "calendar", title: "Timetable", detail: "Enrolled schedule lifecycle by closed semester", path: "/records/timetable" },
  { key: "departments", icon: "building", title: "Department", detail: "Department identity and semester relationships", path: "/records/departments" },
  { key: "finance", icon: "finance", title: "Finance", detail: "Fully paid accounts grouped by semester", path: "/records/finance" },
  { key: "notifications", icon: "bell", title: "Notification & Alert", detail: "Every recorded notification and alert lifecycle event", path: "/records/notifications" },
  { key: "results", icon: "grade", title: "Academic Result Archive", detail: "Released student outcomes across approved courses", path: "/records/result-semester" },
] as const;

type HistoryCounts = Record<(typeof historyAreas)[number]["key"], number> & { snapshots: number };

export function HistoryOverview() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const selectedPeriod = searchParams.get("period") ?? "all";
  const [students, setStudents] = useState<OperationalRecord[]>();
  const [teachers, setTeachers] = useState<OperationalRecord[]>();
  const [counts, setCounts] = useState<HistoryCounts>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const [studentRows, teacherRows, courseRows, classroomRows, timetableRows, departmentRows, paidRows, notificationRows, resultRows, auditRows] = await Promise.all([
        recordApi.get("students", "", departmentId, true),
        recordApi.get("teachers", "", departmentId, true),
        recordApi.get("courses", "", departmentId, true),
        recordApi.get("classrooms", "", departmentId, true),
        recordApi.get("timetable", "", departmentId, true),
        recordApi.get("departments", "", departmentId, true),
        financeApi.get("", "Paid"),
        notificationHistoryApi.get(),
        resultApi.get(departmentId, year, true),
        historyApi.get("", "all"),
      ]);
      setStudents(studentRows);
      setTeachers(teacherRows);
      setCounts({ students: studentRows.length, teachers: teacherRows.length, courses: courseRows.length, classrooms: classroomRows.length, timetable: timetableRows.length, departments: departmentRows.length, finance: paidRows.length, notifications: notificationRows.length, results: resultRows.length, snapshots: auditRows.length });
      setError(false);
    } catch { setError(true); }
  }, [departmentId, year]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 0); return () => window.clearTimeout(timer); }, [load]);

  const periods = useMemo(() => buildPeriods(students ?? [], teachers ?? []), [students, teachers]);
  if (error) return <ErrorPage retry={load}/>;
  if (!students || !teachers || !counts) return <LoadingPage/>;
  const totalRecords = historyAreas.reduce((total, area) => total + counts[area.key], 0);

  return <div className="viewport-data-page history-control-overview-page">
    <PageHeading eyebrow="Permanent lifecycle archive" title="History Overview" description="Open each read-only archive from one ordered dashboard. Every destination shows its current total record count."/>
    <section className="panel history-overview-hero">
      <div><span>Archive at a glance</span><h2>{totalRecords.toLocaleString()} total history records</h2><p>Paid Finance semesters, published results, institutional records, and communication history remain separate and read-only.</p></div>
      <dl><div><dt>Lifecycle snapshots</dt><dd>{counts.snapshots.toLocaleString()}</dd></div><div><dt>Paid semesters</dt><dd>{counts.finance.toLocaleString()}</dd></div><div><dt>Notification & Alert</dt><dd>{counts.notifications.toLocaleString()}</dd></div><div><dt>Published results</dt><dd>{counts.results.toLocaleString()}</dd></div></dl>
    </section>
    <section className="record-semester-switcher panel"><div><span>Archived academic period</span><strong>{selectedPeriod === "all" ? "All completed semesters" : periods.find(period => period.key === selectedPeriod)?.label ?? "Selected semester"}</strong></div><label><span>Inspect an archived semester</span><select value={selectedPeriod} onChange={event => changePeriod(event.target.value)}><option value="all">All archived periods</option>{periods.map(period => <option value={period.key} key={period.key}>{period.label}</option>)}</select></label></section>
    <div className="history-control-overview-scroll">
      <section className="history-archive-grid" aria-label="History destinations">{historyAreas.map((area, index) => <Link className="panel history-archive-card" href={scopedHref(area.path, departmentId, year, selectedPeriod)} key={area.key}><span className="history-archive-index">{String(index + 1).padStart(2, "0")}</span><span className="history-archive-icon"><Icon name={area.icon} size={20}/></span><div><strong>{area.title}</strong><p>{area.detail}</p></div><aside><b>{counts[area.key].toLocaleString()}</b><small>Total records</small></aside><Icon name="arrow" size={14}/></Link>)}</section>
      <section className="panel history-semester-coverage"><header><div><span>Academic archive coverage</span><h2>Completed semesters</h2></div></header><DataTable className="history-semester-table" headerClassName="history-semester-head" rowSelector=":scope > .history-semester-row" columns={["Academic year", "Semester", "Students", "Teachers", "Attendance", "Grades", "Open"]}>{periods.map(period => <article className="history-semester-row" key={period.key}><strong>{period.academicYear}</strong><span>{period.term}</span><b>{period.students}</b><b>{period.teachers}</b><span>{period.attendance}</span><span>{period.grades}</span><button type="button" onClick={() => changePeriod(period.key)}>View <Icon name="arrow" size={12}/></button></article>)}{!periods.length && <div className="empty-state"><strong>No semester history yet</strong><span>Completed enrolled classes and grades will create the first semester view.</span></div>}</DataTable></section>
    </div>
  </div>;

  function changePeriod(period: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (period === "all") params.delete("period"); else params.set("period", period);
    router.replace(`/records/overview${params.size ? `?${params}` : ""}`, { scroll: false });
  }
}

function buildPeriods(students: OperationalRecord[], teachers: OperationalRecord[]) {
  const keys = new Set([...students, ...teachers].flatMap(row => row.activities.filter(activity => activity["Academic year"] && activity.Term).map(activity => `${activity["Academic year"]}|${activity.Term}`)));
  return [...keys].map(key => {
    const [academicYear, term] = key.split("|");
    const studentRows = students.filter(row => row.activities.some(activity => activity["Academic year"] === academicYear && activity.Term === term));
    const teacherRows = teachers.filter(row => row.activities.some(activity => activity["Academic year"] === academicYear && activity.Term === term));
    return { key, academicYear, term, label: `${academicYear} · ${term}`, students: studentRows.length, teachers: teacherRows.length, attendance: studentRows.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Class attendance" && activity["Academic year"] === academicYear && activity.Term === term).length, 0), grades: studentRows.reduce((total, row) => total + row.activities.filter(activity => activity.Activity === "Course grade" && activity["Academic year"] === academicYear && activity.Term === term).length, 0) };
  }).toSorted((left, right) => left.key.localeCompare(right.key, undefined, { numeric: true }));
}

function scopedHref(pathname: string, departmentId: string, year: string, period: string) {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  if (period !== "all") params.set("period", period);
  return `${pathname}${params.size ? `?${params}` : ""}`;
}
