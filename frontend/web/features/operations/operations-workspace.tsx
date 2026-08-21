"use client";

import { useParams, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, MetricCards, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";
import { managementApi } from "@/features/management/management-api";
import type { CatalogItem } from "@/features/management/management-types";
import { ActivityPanel } from "./components/activity-panel";
import { OperationPanel } from "./components/operation-panel";
import { operationsApi } from "./operations-api";
import type { Operation } from "./operations-types";

export default function OperationsWorkspace() {
  const { module } = useParams<{ module: string }>();
  const sidebarDepartmentId = useSearchParams().get("departmentId") ?? "";
  const timetable = module === "timetable";
  const [timetableDepartments, setTimetableDepartments] = useState<CatalogItem[]>([]);
  const [timetableDepartmentId, setTimetableDepartmentId] = useState("");
  const [data, setData] = useState<Operation>();
  const [error, setError] = useState(false);
  const departmentId = timetable ? timetableDepartmentId : sidebarDepartmentId;
  const load = useCallback(() => operationsApi.get(module, departmentId).then(value => { setData(value); setError(false); }).catch(() => setError(true)), [module, departmentId]);

  useEffect(() => {
    if (!timetable) return;
    managementApi.get("departments").then(items => {
      const available = items.filter(item => item.values.status !== "Inactive");
      setTimetableDepartments(available);
      setTimetableDepartmentId(current => available.some(item => item.id === current) ? current : available[0]?.id ?? "");
      if (!available.length) setError(true);
    }).catch(() => setError(true));
  }, [timetable]);
  useEffect(() => { if (!timetable || departmentId) void load(); }, [departmentId, load, timetable]);
  if (error) return <ErrorPage retry={load}/>;
  if (!data) return <LoadingPage/>;

  const dashboard = data.module === "dashboard";
  const visual = data.module === "classrooms" || data.module === "timetable";
  return <div className={`operations-workspace ${visual ? "operations-visual-workspace" : ""}`}>
    <PageHeading eyebrow={dashboard ? "Institute operations" : "Live operation"} title={data.title} description={data.description} actions={<>{timetable && <label className="timetable-department-filter"><span>Department</span><select aria-label="Timetable department" value={timetableDepartmentId} onChange={event => setTimetableDepartmentId(event.target.value)}>{timetableDepartments.map(department => <option value={department.id} key={department.id}>{department.values.name}</option>)}</select></label>}<span className="live-pill"><i/> {dashboard ? "Institute status current" : "Auto-refresh on"}</span><button className="button primary" onClick={load}><Icon name={dashboard ? "dashboard" : "pulse"} size={16}/>Refresh</button></>}/>
    {!dashboard && !visual && <MetricCards metrics={data.metrics}/>} 
    {dashboard || visual ? <OperationPanel data={data} departmentId={departmentId} className={dashboard ? "operation-dashboard-page" : "operation-visual-page"} kicker={dashboard ? "Four core institute operations" : "Whole view · no scrolling"}/> : <section className="operation-layout"><OperationPanel data={data} departmentId={departmentId} kicker="Live data"/><aside className="operation-side"><ActivityPanel title="Attention" kicker="Requires action" items={data.attention}/><ActivityPanel title="Recent activity" kicker="Stream" items={data.activity}/></aside></section>}
  </div>;
}
