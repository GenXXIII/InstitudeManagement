"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { ManagementEditor } from "./components/management-editor";
import { ManagementOverview } from "./components/management-overview";
import { ModuleLayout } from "./components/module-layout";
import { classroomApi } from "./classrooms/classroom-api";
import { attendanceApi } from "@/features/attendance/attendance-api";
import { courseApi } from "./courses/course-api";
import { departmentApi } from "./departments/department-api";
import { managementApis } from "./management-apis";
import { emptyReferences, managementCopy } from "./management-config";
import { studentApi } from "./students/student-api";
import { teacherApi } from "./teachers/teacher-api";
import type { ManagementItem, ManagementModule, References } from "./management-types";
import { timetableApi } from "@/features/timetable/timetable-api";
import { TimetableEditor } from "@/features/timetable/timetable-editor";
import type { TimetableItem } from "@/features/timetable/timetable-types";
import {
  filterManagementItemsByYear,
  filterManagementReferencesByYear,
  sortManagementItemsByYear,
  sortManagementReferencesByYear,
} from "./management-workspace-model";

export function ManagementWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = (managementModules.has(rawModule as ManagementModule) ? rawModule : "overview") as ManagementModule;
  const resource = currentModule === "overview" ? "departments" : currentModule;
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [items, setItems] = useState<ManagementItem[]>([]);
  const [references, setReferences] = useState<References>(emptyReferences);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [editing, setEditing] = useState<ManagementItem | null | undefined>();

  const loadReferences = useCallback(() => Promise.all([departmentApi.get(), teacherApi.get(), studentApi.get(), classroomApi.get(), courseApi.get(), timetableApi.get(), attendanceApi.get()]).then(([departments, teachers, students, classrooms, courses, timetable, attendance]) => setReferences({ departments, teachers, students, classrooms, courses, timetable, attendance })).catch(() => setError(true)), []);
  const load = useCallback(() => managementApis[resource].get(workflowSourceSearch(query), departmentId).then(result => { setItems(result); setReady(true); }).catch(() => setError(true)), [resource, query, departmentId]);
  useEffect(() => { void loadReferences(); }, [loadReferences]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const selectedDepartment = references.departments.find(x => x.id === departmentId);
  const visibleItems = useMemo(() => sortManagementItemsByYear(filterManagementItemsByYear(items, currentModule, year), currentModule, references), [currentModule, items, references, year]);
  const pagination = useDataPagination(visibleItems, `${currentModule}-${departmentId}-${year}-${query}`);
  const visibleReferences = useMemo(() => sortManagementReferencesByYear(filterManagementReferencesByYear(references, year)), [references, year]);
  const canCreate = currentModule !== "overview";
  if (error) return <ErrorPage retry={() => { setError(false); void loadReferences(); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  async function deactivate(item: ManagementItem) {
    if (!confirm(`Deactivate or remove this ${managementCopy[currentModule].singular}? Its history will remain read-only.`)) return;
    setActionError("");
    try { await managementApis[resource].remove(item.id); void load(); void loadReferences(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "This record is still used by another active record."); }
  }

  return <div className="viewport-data-page management-viewport-page">
    <PageHeading eyebrow={currentModule === "overview" ? "Academic management control center" : "Current data management"} title={managementCopy[currentModule].title} description={managementCopy[currentModule].description} actions={canCreate ? <button className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>Add {managementCopy[currentModule].singular}</button> : undefined}/>
    <section className="management-toolbar panel management-toolbar-global">{currentModule !== "overview" && <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${currentModule}…`}/></label>}<div className="management-scope"><span>Current scope</span><strong>{selectedDepartment?.values.name ?? "Whole institute"}{year ? ` · Year ${year}` : ""}</strong></div></section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Relationship protected</strong><span>{actionError}</span></div><button onClick={() => setActionError("")}>Dismiss</button></section>}
    {currentModule === "overview" ? <ManagementOverview references={visibleReferences} onSelect={value => router.push(`/management/students?departmentId=${encodeURIComponent(value)}${year ? `&year=${year}` : ""}`)} selected={departmentId} year={year}/> : <section className="management-paginated-region"><ModuleLayout module={currentModule} items={pagination.pageItems} references={visibleReferences} onEdit={setEditing} onDeactivate={deactivate}/><DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visibleItems.length} pageSize={pagination.pageSize} onPage={pagination.setPage}/></section>}
    {editing !== undefined && currentModule !== "overview" && (currentModule === "timetable"
      ? <TimetableEditor item={editing as TimetableItem | null} references={references} scopeDepartmentId={departmentId} scopeYear={year} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>
      : <ManagementEditor module={currentModule} item={editing} references={references} scopeDepartmentId={departmentId} scopeYear={year} studentMode={currentModule === "students" && editing ? "profile" : "full"} teacherMode={currentModule === "teachers" && editing ? "profile" : "full"} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>)}
  </div>;
}

const managementModules = new Set<ManagementModule>(["overview", "students", "teachers", "classrooms", "courses", "timetable", "departments"]);
