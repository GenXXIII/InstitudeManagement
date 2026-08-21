"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementEditor } from "./components/management-editor";
import { ManagementOverview } from "./components/management-overview";
import { ModuleLayout } from "./components/module-layout";
import { classroomApi } from "./classrooms/classroom-api";
import { attendanceApi } from "./attendance/attendance-api";
import { courseApi } from "./courses/course-api";
import { departmentApi } from "./departments/department-api";
import { managementApis } from "./management-apis";
import { emptyReferences, managementCopy } from "./management-config";
import { studentApi } from "./students/student-api";
import { teacherApi } from "./teachers/teacher-api";
import { timetableApi } from "./timetable/timetable-api";
import type { ManagementItem, ManagementModule, References } from "./management-types";
import { TimetableEditor } from "./timetable/timetable-editor";
import type { TimetableItem } from "./types/timetable";

export function ManagementWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = (rawModule in managementCopy ? rawModule : "overview") as ManagementModule;
  const resource = currentModule === "overview" ? "departments" : currentModule;
  const router = useRouter();
  const departmentId = useSearchParams().get("departmentId") ?? "";
  const [items, setItems] = useState<ManagementItem[]>([]);
  const [references, setReferences] = useState<References>(emptyReferences);
  const [query, setQuery] = useState("");
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [editing, setEditing] = useState<ManagementItem | null | undefined>();

  const loadReferences = useCallback(() => Promise.all([departmentApi.get(), teacherApi.get(), studentApi.get(), classroomApi.get(), courseApi.get(), timetableApi.get(), attendanceApi.get()]).then(([departments, teachers, students, classrooms, courses, timetable, attendance]) => setReferences({ departments, teachers, students, classrooms, courses, timetable, attendance })).catch(() => setError(true)), []);
  const load = useCallback(() => managementApis[resource].get(query, departmentId).then(result => { setItems(result); setReady(true); }).catch(() => setError(true)), [resource, query, departmentId]);
  useEffect(() => { void loadReferences(); }, [loadReferences]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);

  const selectedDepartment = references.departments.find(x => x.id === departmentId);
  const activeItems = useMemo(() => items.filter(item => !["Inactive", "Cancelled"].includes(item.values.status)), [items]);
  if (error) return <ErrorPage retry={() => { setError(false); void loadReferences(); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  async function deactivate(item: ManagementItem) {
    if (!confirm(`Deactivate or remove this ${managementCopy[currentModule].singular}? Its history will remain read-only.`)) return;
    setActionError("");
    try { await managementApis[resource].remove(item.id); void load(); void loadReferences(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "This record is still used by another active record."); }
  }

  return <>
    <PageHeading eyebrow="Current data management" title={managementCopy[currentModule].title} description={managementCopy[currentModule].description} actions={currentModule !== "overview" ? <button className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>Add {managementCopy[currentModule].singular}</button> : undefined}/>
    <section className="management-toolbar panel management-toolbar-global">{currentModule !== "overview" && <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${currentModule}…`}/></label>}<div className="management-scope"><span>Current scope</span><strong>{selectedDepartment?.values.name ?? "Whole institute"}</strong></div><div className="management-total"><span>Active records</span><strong>{activeItems.length}</strong></div></section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Relationship protected</strong><span>{actionError}</span></div><button onClick={() => setActionError("")}>Dismiss</button></section>}
    {currentModule === "overview" ? <ManagementOverview references={references} onSelect={value => router.push(`/management/students?departmentId=${encodeURIComponent(value)}`)} selected={departmentId}/> : <ModuleLayout module={currentModule} items={items} references={references} onEdit={setEditing} onDeactivate={deactivate}/>} 
    {editing !== undefined && currentModule !== "overview" && (currentModule === "timetable"
      ? <TimetableEditor item={editing as TimetableItem | null} references={references} scopeDepartmentId={departmentId} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>
      : <ManagementEditor module={currentModule} item={editing} references={references} scopeDepartmentId={departmentId} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>)}
  </>;
}
