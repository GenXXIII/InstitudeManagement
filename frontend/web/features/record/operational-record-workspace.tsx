"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableToolbar, PaginatedDataRegion, type DataTableColumn } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { OperationalRecordRow } from "./components/operational-record-row";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";
import { groupClassSessionRecords, sortClassSessionRecords, type ClassSessionRecordGroup } from "./sessions/class-session-record-ordering";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  sessions: { title: "Class sessions by semester", description: "One semester group contains every daily class occurrence, with the newest class date and time shown first.", singular: "session" },
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
  const year = searchParams.get("year") ?? "";
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
    if (isClassSessionModule) return sortClassSessionRecords(rows.filter(row => !year || recordYear(row) === Number(year)));
    return rows
      .filter(row => selectedPeriod === "all" || row.activities.some(activity => `${activity["Academic year"]}|${activity.Term}` === selectedPeriod))
      .filter(row => !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`))
      .toSorted((left, right) => comparePeriod(`${right.academicYear}|${right.term}`, `${left.academicYear}|${left.term}`) || recordYear(left) - recordYear(right) || (left.code || left.subject).localeCompare(right.code || right.subject, undefined, { numeric: true }));
  }, [isClassSessionModule, rows, selectedPeriod, year]);
  const routeModule = currentModule === "sessions" ? "class-sessions" : currentModule;
  const detailParams = new URLSearchParams(searchParams.toString());
  if (isClassSessionModule) detailParams.delete("period");
  const detailQuery = detailParams.toString();
  const detailHref = (id: string) => `${history ? "/records" : "/record"}/${routeModule}/${encodeURIComponent(id)}${detailQuery ? `?${detailQuery}` : ""}`;
  const activityCount = visibleRows.reduce((total, row) => total + row.activities.length, 0);
  const recordGroups = isClassSessionModule ? groupClassSessionRecords(visibleRows) : groupRowsByPeriod(visibleRows);
  const heading = isClassSessionModule
    ? history
      ? { eyebrow: "Completed semester session history", description: "Each ended semester is removed from active Record and kept here as one read-only group, newest semester first." }
      : { eyebrow: "Active semester class sessions", description: "All daily sessions stay in one semester group, with each new date placed at the top." }
    : history
    ? isStudentModule
      ? { eyebrow: "Permanent graduate history", description: `After Year 4 Semester 2, the Management profile and every Enrollment, Operation, and Record semester move here as one read-only story. ${config.description}` }
      : { eyebrow: "Completed semester history", description: `Each closed semester moves its Management identity, Enrollment assignment, operational evidence, and final Record into read-only History. ${config.description}` }
    : isStudentModule
      ? { eyebrow: "Accumulating student-cycle records", description: `Each new semester remains in Record until the student completes Year 4 Semester 2. ${config.description}` }
      : { eyebrow: "Current semester records", description: `Record contains the active semester; completed semesters move to History. ${config.description}` };
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page record-viewport-page">
    <PageHeading eyebrow={heading.eyebrow} title={history ? `${config.title} history` : config.title} description={`${heading.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    {history && !isClassSessionModule && <section className="record-semester-switcher panel"><div><span>{isStudentModule ? "Graduate archive" : "Semester archive"}</span><strong>{selectedPeriod === "all" ? isStudentModule ? "Complete Year 1–4 history" : "All completed semesters" : periods.find(period => period.key === selectedPeriod)?.label ?? "Selected semester"}</strong></div><label><span>Filter archived semester</span><select value={selectedPeriod} onChange={event => changePeriod(event.target.value)}><option value="all">{isStudentModule ? "Full four-year archive" : "All completed semesters"}</option>{periods.map(period => <option value={period.key} key={period.key}>{period.label}</option>)}</select></label></section>}
    <section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{visibleRows.length.toLocaleString()}</strong><small>{isClassSessionModule ? "Time-linked session records" : "Semester-linked records"}</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Enrollment and completed class evidence</small></article><article className="panel"><span>Semester groups</span><strong>{recordGroups.length}</strong><small>{isClassSessionModule ? "Newest class date appears first inside each semester" : "Academic year / semester sections"}</small></article></section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder={`Search ${config.title.toLowerCase()}…`} searchAriaLabel={`Search ${config.title}`} resultLabel={`Showing ${visibleRows.length} records`} className="record-toolbar panel" searchClassName="record-search management-search module-search-field"/>
    {visibleRows.length ? <section className="record-paginated-region"><div className="semester-history-scroll">{recordGroups.map(group => <RecordGroupSection module={currentModule} group={group} history={history} load={load} detailHref={detailHref} key={group.key}/>)}</div></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>{isClassSessionModule ? "No class sessions found" : history ? isStudentModule ? "No completed four-year archive yet" : "No completed semester history yet" : "No current semester records found"}</strong><span>{isClassSessionModule ? history ? "Completed-semester sessions appear here as read-only semester groups." : "Daily sessions appear inside the active semester group as their timetable periods finish." : history ? isStudentModule ? "A student moves here only after completing Year 4 Semester 2." : "This module receives a read-only entry after each semester closes." : isStudentModule ? "Student records accumulate here from enrollment through Year 4 Semester 2." : "Current-semester records appear automatically from Enrollment and timetable activity."}</span></section>}
  </div>;

  function changePeriod(period: string) {
    const params = new URLSearchParams(searchParams.toString());
    if (period === "all") params.delete("period"); else params.set("period", period);
    router.replace(`/records/${routeModule}${params.size ? `?${params}` : ""}`, { scroll: false });
  }
}

type RecordGroup = ClassSessionRecordGroup;

function RecordGroupSection({ module, group, history, load, detailHref }: { module: string; group: RecordGroup; history: boolean; load: () => void; detailHref: (id: string) => string }) {
  return <section className="semester-history-group"><header><div><span>{recordGroupEyebrow(module, history)}</span><h2>{group.title}</h2></div><strong>{group.label}</strong><small>{group.rows.length} record{group.rows.length === 1 ? "" : "s"}</small></header><PaginatedDataRegion items={group.rows} resetKey={`${module}-${history}-${group.key}`} as="fragment">{pageItems => renderLedger(module, pageItems, history, load, detailHref)}</PaginatedDataRegion></section>;
}

function renderLedger(module: string, rows: OperationalRecord[], history: boolean, load: () => void, detailHref: (id: string) => string) {
  const stage = history ? "history" : "record";
  if (module === "sessions") return <DataTable className="session-semester-record-ledger panel" headerClassName="session-semester-record-head" rowSelector=".session-semester-record-row" columns={sessionRecordColumns(history)}><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.status !== "Closed"} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div></DataTable>;
  if (module === "students") return <DataTable className="student-semester-record-ledger panel" headerClassName="student-semester-record-head" rowSelector=".student-semester-record-row" columns={studentRecordColumns(history)}><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.insights?.isFinal !== true && row.status !== "Closed"} showStatus={false} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div></DataTable>;
  if (module === "teachers" || module === "courses" || module === "classrooms") return <DataTable className="entity-semester-record-ledger panel" headerClassName="entity-semester-record-head" rowSelector=".entity-semester-record-row" columns={entityRecordColumns(entityModuleName(module), history)}><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></DataTable>;
  if (module === "departments" || module === "timetable") return <DataTable className="structure-semester-record-ledger panel" headerClassName="structure-semester-record-head" rowSelector=".structure-semester-record-row" columns={structureRecordColumns(module)}><div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></DataTable>;
  return <div>{rows.map(row => <OperationalRecordRow row={row} stage={stage} editable={!history && row.status !== "Closed"} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div>;
}

function groupRowsByPeriod(rows: OperationalRecord[]): RecordGroup[] {
  const groups = new Map<string, OperationalRecord[]>();
  for (const row of rows) { const key = `${row.academicYear}|${row.term}`; groups.set(key, [...(groups.get(key) ?? []), row]); }
  return [...groups].map(([key, periodRows]) => ({ key, title: periodRows[0]?.academicYear ?? "Academic year unavailable", label: periodRows[0]?.term ?? "Semester unavailable", rows: periodRows }));
}
function recordGroupEyebrow(module: string, history: boolean) { return module === "sessions" ? history ? "Completed semester" : "Current semester" : history ? module === "students" ? "Graduate academic year" : "Completed academic semester" : module === "students" ? "Student academic cycle" : "Current academic semester"; }
function comparePeriod(left: string, right: string) { return left.localeCompare(right, undefined, { numeric: true, sensitivity: "base" }); }
function recordYear(record: OperationalRecord) { const match = JSON.stringify(record).match(/Year\s+([1-4])/i); return match ? Number(match[1]) : 99; }
function entityModuleName(module: string): "Teacher" | "Course" | "Classroom" { return module === "teachers" ? "Teacher" : module === "courses" ? "Course" : "Classroom"; }
function sessionRecordColumns(history: boolean): DataTableColumn[] { return [centerColumn("record-code", history ? "Session history code" : "Session record code", 110), centerColumn("date", "Date", 88), centerColumn("time", "Time", 76), centerColumn("year", "Year", 72), leftColumn("teacher", "Teacher", 115), leftColumn("course", "Course", 125), centerColumn("classroom", "Classroom", 88), centerColumn("present", "Present", 64), centerColumn("permission", "Permission", 76), centerColumn("absent", "Absent", 64), centerColumn("status", "Status", 82)]; }
function studentRecordColumns(history: boolean): DataTableColumn[] {
  const identity = [centerColumn("record-code", history ? "History code" : "Record code", 92), centerColumn("photo", "Photo", 56), leftColumn("name", "Name", 140), leftColumn("department", "Department", 130), centerColumn("year", history ? "Completed level" : "Year", 90)];
  const attendance = [centerColumn("present", "Present", 64), centerColumn("permission", "Permission", 76), centerColumn("absent", "Absent", 64)];
  if (history) return [...identity, ...attendance, centerColumn("total-score", "Total score", 78), centerColumn("average", "Average", 72), centerColumn("result", "Program result", 96)];
  const grades = Array.from({ length: 5 }, (_, index) => centerColumn(`grade-${index + 1}`, `Course ${index + 1} grade`, 74));
  return [...identity, centerColumn("shift", "Shift", 72), ...attendance, ...grades, centerColumn("total-score", "Total score", 78), centerColumn("average", "Average", 72), centerColumn("result", "Result", 90)];
}

function entityRecordColumns(module: "Teacher" | "Course" | "Classroom", history: boolean): DataTableColumn[] {
  const columns = [centerColumn("record-code", history ? "History code" : "Record code", 100), centerColumn("visual", module === "Teacher" ? "Photo" : "Type", 62), leftColumn("name", module === "Classroom" ? "Classroom" : `${module} name`, 145), leftColumn("department", module === "Classroom" ? "Building" : "Department", 130), centerColumn("year", history ? "Completed level" : "Year", 90), centerColumn("relation", module === "Teacher" ? "Assigned courses" : module === "Course" ? "Students" : "Courses", 86)];
  if (module === "Teacher") columns.push(centerColumn("present", "Present", 64), centerColumn("permission", "Permission", 76), centerColumn("absent", "Absent", 64));
  else columns.push(centerColumn("running", "Running", 72), centerColumn("available", "Available", 76));
  columns.push(centerColumn("activity", "Semester activity", 92));
  return columns;
}

function structureRecordColumns(module: string): DataTableColumn[] {
  const identity = [centerColumn("record-code", module === "departments" ? "Department code" : "Timetable code", 105), centerColumn("type", "Type", 60), leftColumn("name", module === "departments" ? "Department" : "Course", 145), leftColumn("context", module === "departments" ? "Head" : "Department", 130), centerColumn("year", "Year", 86)];
  return module === "departments"
    ? [...identity, centerColumn("students", "Students", 70), centerColumn("teachers", "Teachers", 70), centerColumn("courses", "Courses", 70), centerColumn("rooms", "Rooms", 64), centerColumn("activity", "Recorded periods", 92)]
    : [...identity, centerColumn("day", "Day", 72), centerColumn("time", "Time", 76), centerColumn("teacher", "Teacher", 110), centerColumn("classroom", "Classroom", 86), centerColumn("students", "Students", 70), centerColumn("activity", "Classes", 70)];
}

function centerColumn(key: string, label: string, minimumWidth: number): DataTableColumn { return { key, label, align: "center", minimumWidth }; }
function leftColumn(key: string, label: string, minimumWidth: number): DataTableColumn { return { key, label, align: "left", minimumWidth }; }
