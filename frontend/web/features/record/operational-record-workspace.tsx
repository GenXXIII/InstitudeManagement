"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { OperationalRecordRow } from "./components/operational-record-row";
import { EntitySemesterRecordHeader } from "./components/entity-semester-record";
import { StudentSemesterRecordHeader } from "./components/student-semester-record";
import { StructureSemesterRecordHeader } from "./components/structure-semester-record";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  sessions: { title: "Recorded timetable sessions", description: "One visual card per held or not-held timetable occurrence with course, teacher, room, and frozen evidence.", singular: "session" },
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
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const selectedPeriod = searchParams.get("period") ?? "all";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [rows, setRows] = useState<OperationalRecord[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => recordApi.get(currentModule, workflowSourceSearch(query), departmentId, history).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [currentModule, departmentId, history, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const periods = useMemo(() => [...new Map(rows.flatMap(row => row.activities
    .filter(activity => activity["Academic year"] && activity.Term)
    .map(activity => [`${activity["Academic year"]}|${activity.Term}`, { key: `${activity["Academic year"]}|${activity.Term}`, label: `${activity["Academic year"]} · ${activity.Term}` }] as const))).values()]
    .toSorted((left, right) => comparePeriod(right.key, left.key)), [rows]);
  const visibleRows = useMemo(() => rows
    .filter(row => selectedPeriod === "all" || row.activities.some(activity => `${activity["Academic year"]}|${activity.Term}` === selectedPeriod))
    .filter(row => !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`))
    .toSorted((left, right) => comparePeriod(`${right.academicYear}|${right.term}`, `${left.academicYear}|${left.term}`) || recordYear(left) - recordYear(right) || (left.code || left.subject).localeCompare(right.code || right.subject, undefined, { numeric: true })), [rows, selectedPeriod, year]);
  const routeModule = currentModule === "sessions" ? "class-sessions" : currentModule;
  const detailQuery = searchParams.toString();
  const detailHref = (id: string) => `${history ? "/records" : "/record"}/${routeModule}/${encodeURIComponent(id)}${detailQuery ? `?${detailQuery}` : ""}`;
  const activityCount = visibleRows.reduce((total, row) => total + row.activities.length, 0);
  const periodGroups = groupRowsByPeriod(visibleRows);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page record-viewport-page">
    <PageHeading eyebrow={history ? "Permanent graduate history" : "Accumulating student-cycle records"} title={history ? `${config.title} history` : config.title} description={`${history ? "History is created only after Year 4 Semester 2 and remains read-only. " : "Each new semester is added above the earlier semesters; nothing leaves Record until the student completes Year 4 Semester 2. "}${config.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    {history && <section className="record-semester-switcher panel"><div><span>Graduate archive</span><strong>{selectedPeriod === "all" ? "Complete Year 1–4 history" : periods.find(period => period.key === selectedPeriod)?.label ?? "Selected semester"}</strong></div><label><span>Filter archived semester</span><select value={selectedPeriod} onChange={event => changePeriod(event.target.value)}><option value="all">Full four-year archive</option>{periods.map(period => <option value={period.key} key={period.key}>{period.label}</option>)}</select></label></section>}
    <section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{visibleRows.length.toLocaleString()}</strong><small>Semester-linked records</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Enrollment and completed class evidence</small></article><article className="panel"><span>Semester groups</span><strong>{new Set(visibleRows.map(row => `${row.academicYear}|${row.term}`)).size}</strong><small>Academic year / semester sections</small></article></section>
    <section className="record-toolbar panel"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></label><span className="record-count">Showing {visibleRows.length} records</span></section>
    {visibleRows.length ? <section className="record-paginated-region"><div className="semester-history-scroll">{periodGroups.map(group => <SemesterHistoryGroup module={currentModule} group={group} history={history} load={load} detailHref={detailHref} key={group.key}/>)}</div></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>{history ? "No completed four-year archive yet" : "No accumulated records found"}</strong><span>{history ? "A student moves here only after completing Year 4 Semester 2." : "Records appear automatically from Enrollment and recorded timetable periods, then remain through the student cycle."}</span></section>}
  </div>;

  function changePeriod(period: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (period === "all") params.delete("period"); else params.set("period", period);
    router.replace(`/records/${routeModule}${params.size ? `?${params}` : ""}`, { scroll: false });
  }
}

function SemesterHistoryGroup({ module, group, history, load, detailHref }: { module: string; group: ReturnType<typeof groupRowsByPeriod>[number]; history: boolean; load: () => void; detailHref: (id: string) => string }) {
  const pagination = useDataPagination(group.rows, `${module}-${history}-${group.key}`);
  return <section className="semester-history-group"><header><div><span>Academic history</span><h2>{group.academicYear}</h2></div><strong>{group.term}</strong><small>{group.rows.length} record{group.rows.length === 1 ? "" : "s"}</small></header>{renderLedger(module, pagination.pageItems, history, load, detailHref)}<DataPagination page={pagination.page} pageCount={pagination.pageCount} total={group.rows.length} onPage={pagination.setPage}/></section>;
}

function renderLedger(module: string, rows: OperationalRecord[], history: boolean, load: () => void, detailHref: (id: string) => string) {
  const stage = history ? "history" : "record";
  if (module === "students") return <div className="student-semester-record-ledger panel"><StudentSemesterRecordHeader history={history}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.insights?.isFinal !== true && row.status !== "Closed"} showStatus={false} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  if (module === "teachers" || module === "courses" || module === "classrooms") return <div className="entity-semester-record-ledger panel"><EntitySemesterRecordHeader module={entityModuleName(module)}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  if (module === "departments" || module === "timetable") return <div className="structure-semester-record-ledger panel"><StructureSemesterRecordHeader module={module === "departments" ? "Department" : "Timetable"}/><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>;
  return <div className="session-record-list">{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.status !== "Closed"} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div>;
}

function groupRowsByPeriod(rows: OperationalRecord[]) {
  const groups = new Map<string, OperationalRecord[]>();
  for (const row of rows) { const key = `${row.academicYear}|${row.term}`; groups.set(key, [...(groups.get(key) ?? []), row]); }
  return [...groups].map(([key, periodRows]) => ({ key, academicYear: periodRows[0]?.academicYear ?? "Academic year unavailable", term: periodRows[0]?.term ?? "Semester unavailable", rows: periodRows }));
}
function comparePeriod(left: string, right: string) { return left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }); }
function recordYear(record: OperationalRecord) { const match = JSON.stringify(record).match(/Year\s+([1-4])/i); return match ? Number(match[1]) : 99; }
function entityModuleName(module: string): "Teacher" | "Course" | "Classroom" { return module === "teachers" ? "Teacher" : module === "courses" ? "Course" : "Classroom"; }
