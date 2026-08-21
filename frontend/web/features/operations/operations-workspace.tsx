"use client";

import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, MetricCards, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { ActivityPanel } from "./components/activity-panel";
import { OperationPanel } from "./components/operation-panel";
import { operationsApi } from "./operations-api";
import type { Operation } from "./operations-types";

export default function OperationsWorkspace() {
  const { settings } = useInstituteSettings();
  const { module } = useParams<{ module: string }>();
  const sidebarDepartmentId = useSearchParams().get("departmentId") ?? "";
  const timetable = module === "timetable";
  const [data, setData] = useState<Operation>();
  const [error, setError] = useState(false);
  const departmentId = sidebarDepartmentId;
  const load = useCallback(() => operationsApi.get(module, departmentId).then(value => { setData(value); setError(false); }).catch(() => setError(true)), [module, departmentId]);

  useEffect(() => { void load(); }, [load]);
  const refreshSeconds = Math.max(5, Number(settings.system.autoRefreshSeconds) || 30);
  useEffect(() => { const timer = window.setInterval(() => void load(), refreshSeconds * 1000); return () => window.clearInterval(timer); }, [load, refreshSeconds]);
  if (error) return <ErrorPage retry={load}/>;
  if (!data) return <LoadingPage/>;

  const dashboard = data.module === "dashboard";
  const visual = data.module === "classrooms" || data.module === "timetable";
  return <div className={`operations-workspace ${visual ? "operations-visual-workspace" : ""}`}>
    <PageHeading eyebrow={dashboard ? "Institute operations" : "Live operation"} title={data.title} description={data.description} actions={<>{timetable && <Link className="button secondary" href={`/management/timetable${departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : ""}`}>Manage timetable</Link>}<span className="live-pill"><i/> {dashboard ? "Institute status current" : "Auto-refresh on"}</span><button className="button primary" onClick={load}><Icon name={dashboard ? "dashboard" : "pulse"} size={16}/>Refresh</button></>}/>
    {!dashboard && !visual && <MetricCards metrics={data.metrics}/>} 
    {dashboard || visual ? <OperationPanel data={data} departmentId={departmentId} className={dashboard ? "operation-dashboard-page" : "operation-visual-page"} kicker={dashboard ? "Four core institute operations" : timetable ? "One-page weekly schedule" : "Whole view · no scrolling"}/> : <section className="operation-layout"><OperationPanel data={data} departmentId={departmentId} kicker="Live data"/><aside className="operation-side"><ActivityPanel title="Attention" kicker="Requires action" items={data.attention}/><ActivityPanel title="Recent activity" kicker="Stream" items={data.activity}/></aside></section>}
  </div>;
}
