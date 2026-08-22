"use client";

import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { RecordMetric } from "./components/record-metric";
import { RecordRow } from "./components/record-row";
import { historyApi } from "./history-api";
import { recordTypes } from "./history-config";
import type { LifecycleFilter, RecordItem } from "./history-types";
import { exportCsv, groupRecords, isInactive } from "./history-utils";

export default function RecordsRoute() {
  return <Suspense fallback={<LoadingPage/>}><RecordRegister/></Suspense>;
}

function RecordRegister() {
  const { resource } = useParams<{ resource: string }>();
  const config = recordTypes[resource] ?? recordTypes.students;
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [filter, setFilter] = useState<LifecycleFilter>("all");
  const [rows, setRows] = useState<RecordItem[]>([]);
  const [error, setError] = useState(false);
  const [ready, setReady] = useState(false);
  const load = useCallback(() => historyApi.get(query, config.type).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [config.type, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const groups = useMemo(() => groupRecords(rows).filter(group => {
    const yearValues = group.values.filter(([key]) => ["year", "yearlevel"].includes(key.toLowerCase())).map(([, value]) => value);
    return (!departmentId || group.key.includes(departmentId) || group.entries.some(entry => entry.details.includes(departmentId))) && (!year || !yearValues.length || yearValues.includes(year));
  }), [departmentId, rows, year]);
  const inactiveCount = groups.filter(group => isInactive(group.status)).length;
  const visible = groups.filter(group => filter === "all" || (filter === "inactive" ? isInactive(group.status) : !isInactive(group.status)));
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <>
    <PageHeading eyebrow="Institutional record register" title={config.title} description={config.description} actions={<button className="button secondary" onClick={() => exportCsv(rows)}><Icon name="archive" size={15}/>Export all snapshots</button>}/>
    <section className="record-lock-notice"><div><Icon name="archive" size={18}/></div><p><strong>Permanent read-only history</strong><span>Management controls current data. This register preserves active, inactive, cancelled, and removed records together with every captured change.</span></p></section>
    <section className="record-overview-grid"><RecordMetric label="All records" value={groups.length} detail="individual profiles"/><RecordMetric label="Current" value={groups.length - inactiveCount} detail="active or recorded" tone="green"/><RecordMetric label="Inactive / archived" value={inactiveCount} detail="still fully visible" tone="red"/><RecordMetric label="History snapshots" value={rows.length} detail="complete captured changes" tone="violet"/></section>
    <section className="record-toolbar panel"><div className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search all ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></div><select value={filter} onChange={event => setFilter(event.target.value as LifecycleFilter)} aria-label="Record status"><option value="all">All statuses</option><option value="current">Current / active</option><option value="inactive">Inactive / archived</option></select><span className="record-count">Showing {visible.length} of {groups.length} records</span></section>
    {visible.length ? <section className="record-register history-management-table panel"><div className="record-register-head history-management-head"><span>Record identity</span><span>Latest management data</span><span>Lifecycle</span><span>Last updated</span><span>Actions</span></div><div className="record-register-list">{visible.map(group => <RecordRow group={group} key={group.key}/>)}</div></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>No records found</strong><span>Try another search or status filter.</span></section>}
  </>;
}
