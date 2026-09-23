"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentHistoryGroups, type AssessmentHistoryGroup } from "./assessment-history-model";
import { historyApi } from "./history-api";
import type { RecordItem } from "./history-types";

export function AssessmentHistoryWorkspace() {
  const router = useRouter();
  const [rows, setRows] = useState<RecordItem[]>();
  const [query, setQuery] = useState("");
  const [period, setPeriod] = useState("All");
  const [error, setError] = useState(false);
  const load = useCallback(() => historyApi.get("", "Grade").then(value => { setRows(value); setError(false); }).catch(() => setError(true)), []);
  useEffect(() => { void load(); }, [load]);
  const groups = useMemo(() => assessmentHistoryGroups(rows ?? []), [rows]);
  const periods = useMemo(() => [...new Set(groups.map(group => `${group.academicYear} · ${group.term}`))].toSorted().reverse(), [groups]);
  const visible = useMemo(() => {
    const text = workflowSourceSearch(query).toLowerCase();
    return groups.filter(group => (period === "All" || `${group.academicYear} · ${group.term}` === period) && (!text || [group.courseCode, group.course, group.teacher, group.department, group.yearLevel, group.shift, ...group.grades.flatMap(item => [item.student, item.studentCode, item.gradeCode])].some(value => value.toLowerCase().includes(text))));
  }, [groups, period, query]);

  if (error) return <ErrorPage retry={load}/>;
  if (!rows) return <LoadingPage/>;
  return <div className="viewport-data-page history-viewport-page assessment-history-page">
    <PageHeading eyebrow="Permanent academic archive" title="Assessment History" description="Read-only course assessment records. Select a course summary to open every Student grade, assigned Teacher, submission date, and Administrator approval date."/>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search course, Teacher, or Student..." searchAriaLabel="Search Assessment History" resultLabel={`${visible.length} course records`} className="record-toolbar panel assessment-toolbar" searchClassName="record-search management-search module-search-field"><select value={period} onChange={event => setPeriod(event.target.value)} aria-label="Academic period"><option>All</option>{periods.map(value => <option value={value} key={value}>{value}</option>)}</select></DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${query}-${period}`} className="assessment-paginated-region" empty={<DataTableEmptyState icon={<Icon name="archive" size={28}/>} title="No Assessment History" description="Accepted and retained course assessment records will appear here."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table assessment-history-register" headerClassName="horizontal-management-head assessment-history-register-head" rowSelector=":scope > .assessment-history-register-row" columns={[{ key: "course", label: "Course", minimumWidth: 190 }, { key: "teacher", label: "Assigned Teacher", minimumWidth: 150 }, { key: "cohort", label: "Cohort", minimumWidth: 145 }, { key: "period", label: "Academic Period", minimumWidth: 125 }, { key: "students", label: "Students", minimumWidth: 80, align: "right" }, { key: "submitted", label: "Teacher Submitted", minimumWidth: 140, align: "center" }, { key: "approved", label: "Admin Approved", minimumWidth: 140, align: "center" }, { key: "action", label: "Action", minimumWidth: 80, align: "center" }]} ariaLabel="Assessment course history">{pageItems.map(group => <AssessmentHistoryRow group={group} onOpen={() => router.push(`/records/assessment/${encodeURIComponent(group.routeId)}`)} key={group.key}/>)}</DataTable>}</PaginatedDataRegion>
  </div>;
}

function AssessmentHistoryRow({ group, onOpen }: { group: AssessmentHistoryGroup; onOpen: () => void }) {
  return <article className="horizontal-management-row assessment-history-register-row" role="link" tabIndex={0} onClick={onOpen} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onOpen(); } }}>
    <span className="assessment-register-person"><span className="assessment-student-icon"><Icon name="archive" size={17}/></span><span><strong>{group.course}</strong><small>{group.courseCode}</small></span></span>
    <span className="assessment-register-copy"><strong>{group.teacher}</strong><small>Assigned Teacher</small></span>
    <span className="assessment-register-copy"><strong>{group.department}</strong><small>{group.yearLevel ? `Year ${group.yearLevel}` : "Year not assigned"} / {group.shift || "Shift not assigned"}</small></span>
    <span className="assessment-register-copy"><strong>{group.academicYear}</strong><small>{group.term}</small></span>
    <span className="assessment-register-center"><strong>{group.grades.length}</strong><small>Version {group.submissionVersion}</small></span>
    <time className="assessment-register-date">{formatDate(group.submittedAtUtc)}</time>
    <time className="assessment-register-date">{formatDate(group.reviewedAtUtc)}</time>
    <span className="assessment-register-action"><button type="button" className="button secondary" onClick={event => { event.stopPropagation(); onOpen(); }}>Open</button></span>
  </article>;
}

function formatDate(value: string) {
  if (!value) return "Not recorded";
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date);
}
