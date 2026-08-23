"use client";

import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { RecordMetric } from "./components/record-metric";
import { RecordRow } from "./components/record-row";
import { historyApi } from "./history-api";
import { recordTypes } from "./history-config";
import type { RecordItem } from "./history-types";
import { exportCsv, groupRecords } from "./history-utils";

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
  const visible = groups;
  const detailQuery = searchParams.toString();
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page history-viewport-page">
    <PageHeading eyebrow="Institutional record register" title={config.title} description={config.description} actions={<button className="button secondary" onClick={() => exportCsv(rows)}><Icon name="archive" size={15}/>Export all snapshots</button>}/>
    <section className="record-lock-notice"><div><Icon name="archive" size={18}/></div><p><strong>Permanent read-only history</strong><span>Management controls current data. This register preserves every captured snapshot and change.</span></p></section>
    <section className="record-overview-grid"><RecordMetric label="All records" value={groups.length} detail="individual profiles"/><RecordMetric label="History snapshots" value={rows.length} detail="complete captured changes" tone="violet"/></section>
    <section className="record-toolbar panel"><div className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search all ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></div><span className="record-count">Showing {visible.length} of {groups.length} records</span></section>
    {visible.length ? <section className="record-register history-management-table panel"><div className="record-register-head history-management-head"><span>Record identity</span><span>Latest management data</span><span>Last updated</span></div><div className="record-register-list">{visible.map(group => <RecordRow group={group} detailHref={`/records/${resource}/${encodeURIComponent(group.key)}${detailQuery ? `?${detailQuery}` : ""}`} key={group.key}/>)}</div></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>No records found</strong><span>Try another search.</span></section>}
  </div>;
}
