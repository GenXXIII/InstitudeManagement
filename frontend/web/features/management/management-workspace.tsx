"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementEditor } from "./components/management-editor";
import { ManagementOverview } from "./components/management-overview";
import { ModuleLayout } from "./components/module-layout";
import { managementApi } from "./management-api";
import { emptyReferences, managementCopy } from "./management-config";
import type { CatalogItem, ManagementModule, References } from "./management-types";

export function ManagementWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = (rawModule in managementCopy ? rawModule : "overview") as ManagementModule;
  const router = useRouter();
  const departmentId = useSearchParams().get("departmentId") ?? "";
  const [items, setItems] = useState<CatalogItem[]>([]);
  const [references, setReferences] = useState<References>(emptyReferences);
  const [query, setQuery] = useState("");
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [editing, setEditing] = useState<CatalogItem | null | undefined>();

  const loadReferences = useCallback(() => Promise.all([managementApi.get("departments"), managementApi.get("teachers"), managementApi.get("students"), managementApi.get("classrooms"), managementApi.get("courses")]).then(([departments, teachers, students, classrooms, courses]) => setReferences({ departments, teachers, students, classrooms, courses })).catch(() => setError(true)), []);
  const load = useCallback(() => managementApi.get(currentModule === "overview" ? "departments" : currentModule, query, departmentId).then(result => { setItems(result); setReady(true); }).catch(() => setError(true)), [currentModule, query, departmentId]);
  useEffect(() => { void loadReferences(); }, [loadReferences]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);

  const selectedDepartment = references.departments.find(x => x.id === departmentId);
  const activeItems = useMemo(() => items.filter(item => !["Inactive", "Cancelled"].includes(item.values.status)), [items]);
  if (error) return <ErrorPage retry={() => { setError(false); void loadReferences(); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  async function deactivate(item: CatalogItem) {
    if (!confirm(`Deactivate or remove this ${managementCopy[currentModule].singular}? Its history will remain read-only.`)) return;
    setActionError("");
    try { await managementApi.remove(currentModule, item.id); void load(); void loadReferences(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "This record is still used by another active record."); }
  }

  return <>
    <PageHeading eyebrow="Current data management" title={managementCopy[currentModule].title} description={managementCopy[currentModule].description} actions={currentModule !== "overview" ? <button className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>Add {managementCopy[currentModule].singular}</button> : undefined}/>
    <section className="management-toolbar panel management-toolbar-global">{currentModule !== "overview" && <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${currentModule}…`}/></label>}<div className="management-scope"><span>Current scope</span><strong>{selectedDepartment?.values.name ?? "Whole institute"}</strong></div><div className="management-total"><span>Active records</span><strong>{activeItems.length}</strong></div></section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Relationship protected</strong><span>{actionError}</span></div><button onClick={() => setActionError("")}>Dismiss</button></section>}
    {currentModule === "overview" ? <ManagementOverview references={references} onSelect={value => router.push(`/management/students?departmentId=${encodeURIComponent(value)}`)} selected={departmentId}/> : <ModuleLayout module={currentModule} items={items} references={references} onEdit={setEditing} onDeactivate={deactivate}/>} 
    {editing !== undefined && currentModule !== "overview" && <ManagementEditor module={currentModule} item={editing} references={references} scopeDepartmentId={departmentId} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>} 
  </>;
}
