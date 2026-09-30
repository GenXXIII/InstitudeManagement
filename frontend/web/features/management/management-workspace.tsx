"use client";

import dynamic from "next/dynamic";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataTableToolbar, ServerPaginatedDataRegion, useServerPage } from "@/components/data-table";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { ManagementOverview } from "./components/management-overview";
import { ModuleLayout } from "./components/module-layout";
import { classroomApi } from "./classrooms/classroom-api";
import { courseApi } from "./courses/course-api";
import { departmentApi } from "./departments/department-api";
import { managementApis } from "./management-apis";
import { emptyReferences, managementCopy } from "./management-config";
import { studentApi } from "./students/student-api";
import { teacherApi } from "./teachers/teacher-api";
import type { ManagementItem, ManagementModule, References } from "./management-types";
import { timetableApi } from "@/features/timetable/timetable-api";
import type { TimetableItem } from "@/features/timetable/timetable-types";
import { emptyPage, type PagedResult } from "@/lib/pagination";
import {
  filterManagementItemsByYear,
  filterManagementReferencesByYear,
  sortManagementItemsByYear,
  sortManagementReferencesByYear,
} from "./management-workspace-model";

const ManagementEditor = dynamic(() => import("./components/management-editor").then(module => module.ManagementEditor), { ssr: false });
const TimetableEditor = dynamic(() => import("@/features/timetable/timetable-editor").then(module => module.TimetableEditor), { ssr: false });

export function ManagementWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = (managementModules.has(rawModule as ManagementModule) ? rawModule : "overview") as ManagementModule;
  const resource = currentModule === "overview" ? "departments" : currentModule;
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [references, setReferences] = useState<References>(emptyReferences);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [result, setResult] = useState<PagedResult<ManagementItem>>(() => emptyPage());
  const [page, setPage] = useServerPage(`${resource}-${departmentId}-${year}-${query}`);
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [editing, setEditing] = useState<ManagementItem | null | undefined>();

  const loadReferences = useCallback(() => {
    const overview = currentModule === "overview";
    return Promise.all([
      departmentApi.get(),
      overview || currentModule === "departments" ? teacherApi.get() : Promise.resolve([]),
      overview ? studentApi.get() : Promise.resolve([]),
      overview ? classroomApi.get() : Promise.resolve([]),
      overview ? courseApi.get() : Promise.resolve([]),
      overview ? timetableApi.get() : Promise.resolve([]),
    ]).then(([departments, teachers, students, classrooms, courses, timetable]) => setReferences({ departments, teachers, students, classrooms, courses, timetable, attendance: [] })).catch(() => setError(true));
  }, [currentModule]);
  const load = useCallback(() => managementApis[resource].getPage(workflowSourceSearch(query), departmentId, { page }).then(value => { setResult(value); setReady(true); }).catch(() => setError(true)), [resource, query, departmentId, page]);
  useEffect(() => { void loadReferences(); }, [loadReferences]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const selectedDepartment = references.departments.find(x => x.id === departmentId);
  const visibleItems = useMemo(() => sortManagementItemsByYear(filterManagementItemsByYear(result.items, currentModule, year), currentModule, references), [currentModule, result.items, references, year]);
  const visibleResult = useMemo(() => ({ ...result, items: visibleItems }), [result, visibleItems]);
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
    <DataTableToolbar query={currentModule === "overview" ? undefined : query} onQueryChange={setQuery} searchPlaceholder={`Search ${currentModule}…`} searchAriaLabel={`Search ${currentModule}`} contextLabel="Current scope" contextValue={<>{selectedDepartment?.values.name ?? "Whole institute"}{year ? ` · Year ${year}` : ""}</>}/>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Relationship protected</strong><span>{actionError}</span></div><button onClick={() => setActionError("")}>Dismiss</button></section>}
    {currentModule === "overview" ? <ManagementOverview references={visibleReferences} onSelect={value => router.replace(`/management/students?departmentId=${encodeURIComponent(value)}${year ? `&year=${year}` : ""}`)} selected={departmentId} year={year}/> : <ServerPaginatedDataRegion result={visibleResult} onPage={setPage} className="management-paginated-region">{pageItems => <ModuleLayout module={currentModule} items={pageItems} references={visibleReferences} onEdit={setEditing} onDeactivate={deactivate}/>}</ServerPaginatedDataRegion>}
    {editing !== undefined && currentModule !== "overview" && (currentModule === "timetable"
      ? <TimetableEditor item={editing as TimetableItem | null} scopeDepartmentId={departmentId} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>
      : <ManagementEditor module={currentModule} item={editing} references={references} scopeDepartmentId={departmentId} scopeYear={year} studentMode={currentModule === "students" && editing ? "profile" : "full"} teacherMode={currentModule === "teachers" && editing ? "profile" : "full"} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>)}
  </div>;
}

const managementModules = new Set<ManagementModule>(["overview", "students", "teachers", "classrooms", "courses", "timetable", "departments"]);
