"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { OperationalRecordRow } from "./components/operational-record-row";
import { EntitySemesterRecordHeader } from "./components/entity-semester-record";
import { StudentSemesterRecordHeader } from "./students/student-semester-record";
import { StructureSemesterRecordHeader } from "./components/structure-semester-record";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";
import { groupClassSessionRecords, sortClassSessionRecords, type ClassSessionRecordGroup } from "./sessions/class-session-record-ordering";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  sessions: { title: "Class sessions by date and time", description: "One visual card per held or not-held timetable occurrence, ordered by its actual class date and start time.", singular: "session" },
  students: { title: "Student semester records", description: "Student information, attendance, five course grades, and semester result remain together.", singular: "student" },
  teachers: { title: "Teacher semester records", description: "Teacher information, year levels, attendance, assigned courses, and completed classes remain together.", singular: "teacher" },
  courses: { title: "Course semester records", description: "Course information, assigned year, enrolled student total, state, and class details.", singular: "course" },
  classrooms: { title: "Classroom semester records", description: "Classroom information, assigned course total, year levels, state, and full course details.", singular: "classroom" },
  departments: { title: "Department semester records", description: "A visual semester view of students, teachers, courses, classrooms, and completed classes by department.", singular: "department" },
  timetable: { title: "Timetable semester records", description: "A visual semester view of enrolled schedule code, course, year, day, time, teacher, classroom, and students.", singular: "timetable" },
};

export function OperationalRecordWorkspace({ module: rawModule, history = false }: { module: string; history?: boolean }) {
  const currentModule = rawModule in modules ? rawModule : "students";
  const config = modules[currentModule];
  const isClassSessionModule = currentModule === "sessions";
  const isStudentModule = currentModule === "students";
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = isClassSessionModule ? "" : searchParams.get("year") ?? "";
  const selectedPeriod = isClassSessionModule ? "all" : searchParams.get("period") ?? "all";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [rows, setRows] = useState<OperationalRecord[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => recordApi.get(currentModule, workflowSourceSearch(query), departmentId, history).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [currentModule, departmentId, history, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const periods = useMemo(() => isClassSessionModule ? [] : [...new Map(rows.flatMap(row => row.activities
    .filter(activity => activity["Academic year"] && activity.Term)
    .map(activity => [`${activity["Academic year"]}|${activity.Term}`, { key: `${activity["Academic year"]}|${activity.Term}`, label: `${activity["Academic year"]} · ${activity.Term}` }] as const))).values()]
    .toSorted((left, right) => comparePeriod(right.key, left.key)), [isClassSessionModule, rows]);
  const visibleRows = useMemo(() => {
    if (isClassSessionModule) return sortClassSessionRecords(rows);
    return rows
      .filter(row => selectedPeriod === "all" || row.activities.some(activity => `${activity["Academic year"]}|${activity.Term}` === selectedPeriod))
      .filter(row => !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`))
      .toSorted((left, right) => comparePeriod(`${right.academicYear}|${right.term}`, `${left.academicYear}|${left.term}`) || recordYear(left) - recordYear(right) || (left.code || left.subject).localeCompare(right.code || right.subject, undefined, { numeric: true }));
  }, [isClassSessionModule, rows, selectedPeriod, year]);
  const routeModule = currentModule === "sessions" ? "class-sessions" : currentModule;
  const detailParams = new URLSearchParams(searchParams.toString());
  if (isClassSessionModule) { detailParams.delete("year"); detailParams.delete("period"); }
  const detailQuery = detailParams.toString();
  const detailHref = (id: string) => `${history ? "/records" : "/record"}/${routeModule}/${encodeURIComponent(id)}${detailQuery ? `?${detailQuery}` : ""}`;
  const activityCount = visibleRows.reduce((total, row) => total + row.activities.length, 0);
  const recordGroups = isClassSessionModule ? groupClassSessionRecords(visibleRows) : groupRowsByPeriod(visibleRows);
  const heading = isClassSessionModule
    ? history
      ? { eyebrow: "Chronological class session history", description: "Archived sessions are arranged from the earliest class date and start time to the latest." }
      : { eyebrow: "Chronological class session record", description: "Sessions are arranged from the earliest class date and start time to the latest; Year 1–4 does not control this view." }
    : history
    ? isStudentModule
      ? { eyebrow: "Permanent graduate history", description: `History is created only after Year 4 Semester 2 and remains read-only. ${config.description}` }
      : { eyebrow: "Completed semester history", description: `Each closed semester moves from Record to read-only History. ${config.description}` }
    : isStudentModule
      ? { eyebrow: "Accumulating student-cycle records", description: `Each new semester remains in Record until the student completes Year 4 Semester 2. ${config.description}` }
      : { eyebrow: "Current semester records", description: `Record contains the active semester; completed semesters move to History. ${config.description}` };
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page record-viewport-page">
    <PageHeading eyebrow={heading.eyebrow} title={history ? `${config.title} history` : config.title} description={`${heading.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    {history && !isClassSessionModule && <section className="record-semester-switcher panel"><div><span>{isStudentModule ? "Graduate archive" : "Semester archive"}</span><strong>{selectedPeriod === "all" ? isStudentModule ? "Complete Year 1–4 history" : "All completed semesters" : periods.find(period => period.key === selectedPeriod)?.label ?? "Selected semester"}</strong></div><label><span>Filter archived semester</span><select value={selectedPeriod} onChange={event => changePeriod(event.target.value)}><option value="all">{isStudentModule ? "Full four-year archive" : "All completed semesters"}</option>{periods.map(period => <option value={period.key} key={period.key}>{period.label}</option>)}</select></label></section>}
    <section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{visibleRows.length.toLocaleString()}</strong><small>{isClassSessionModule ? "Time-linked session records" : "Semester-linked records"}</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Enrollment and completed class evidence</small></article><article className="panel"><span>{isClassSessionModule ? "Class dates" : "Semester groups"}</span><strong>{recordGroups.length}</strong><small>{isClassSessionModule ? "Ascending by session date and start time" : "Academic year / semester sections"}</small></article></section>
    <section className="record-toolbar panel"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></label><span className="record-count">Showing {visibleRows.length} records</span></section>
    {visibleRows.length ? <section className="record-paginated-region"><div className="semester-history-scroll">{recordGroups.map(group => <RecordGroupSection module={currentModule} group={group} history={history} load={load} detailHref={detailHref} key={group.key}/>)}</div></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>{isClassSessionModule ? "No class sessions found" : history ? isStudentModule ? "No completed four-year archive yet" : "No completed semester history yet" : "No current semester records found"}</strong><span>{isClassSessionModule ? "Class sessions appear here by their scheduled date and start time." : history ? isStudentModule ? "A student moves here only after completing Year 4 Semester 2." : "This module receives a read-only entry after each semester closes." : isStudentModule ? "Student records accumulate here from enrollment through Year 4 Semester 2." : "Current-semester records appear automatically from Enrollment and timetable activity."}</span></section>}
  </div>;

  function changePeriod(period: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (period === "all") params.delete("period"); else params.set("period", period);
    router.replace(`/records/${routeModule}${params.size ? `?${params}` : ""}`, { scroll: false });
  }
}

type RecordGroup = ClassSessionRecordGroup;

function RecordGroupSection({ module, group, history, load, detailHref }: { module: string; group: RecordGroup; history: boolean; load: () => void; detailHref: (id: string) => string }) {
  const pagination = useDataPagination(group.rows, `${module}-${history}-${group.key}`);
  return <section className="semester-history-group"><header><div><span>{recordGroupEyebrow(module, history)}</span><h2>{group.title}</h2></div><strong>{group.label}</strong><small>{group.rows.length} record{group.rows.length === 1 ? "" : "s"}</small></header>{renderLedger(module, pagination.pageItems, history, load, detailHref)}<DataPagination page={pagination.page} pageCount={pagination.pageCount} total={group.rows.length} onPage={pagination.setPage}/></section>;
}

function renderLedger(module: string, rows: OperationalRecord[], history: boolean, load: () => void, detailHref: (id: string) => string) {
  const stage = history ? "history" : "record";
  if (module === "students") return <div className="student-semester-record-ledger panel"><StudentSemesterRecordHeader history={history}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.insights?.isFinal !== true && row.status !== "Closed"} showStatus={false} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  if (module === "teachers" || module === "courses" || module === "classrooms") return <div className="entity-semester-record-ledger panel"><EntitySemesterRecordHeader module={entityModuleName(module)} history={history}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  if (module === "departments" || module === "timetable") return <div className="structure-semester-record-ledger panel"><StructureSemesterRecordHeader module={module === "departments" ? "Department" : "Timetable"}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  return <div className="session-record-list">{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.status !== "Closed"} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div>;
}

function groupRowsByPeriod(rows: OperationalRecord[]): RecordGroup[] {
  const groups = new Map<string, OperationalRecord[]>();
  for (const row of rows) { const key = `${row.academicYear}|${row.term}`; groups.set(key, [...(groups.get(key) ?? []), row]); }
  return [...groups].map(([key, periodRows]) => ({ key, title: periodRows[0]?.academicYear ?? "Academic year unavailable", label: periodRows[0]?.term ?? "Semester unavailable", rows: periodRows }));
}
function recordGroupEyebrow(module: string, history: boolean) { return module === "sessions" ? history ? "Archived class date" : "Class session date" : history ? module === "students" ? "Graduate academic year" : "Completed academic semester" : module === "students" ? "Student academic cycle" : "Current academic semester"; }
function comparePeriod(left: string, right: string) { return left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }); }
function recordYear(record: OperationalRecord) { const match = JSON.stringify(record).match(/Year\s+([1-4])/i); return match ? Number(match[1]) : 99; }
function entityModuleName(module: string): "Teacher" | "Course" | "Classroom" { return module === "teachers" ? "Teacher" : module === "courses" ? "Course" : "Classroom"; }
