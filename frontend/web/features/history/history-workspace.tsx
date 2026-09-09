"use client";

import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
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
  const load = useCallback(() => historyApi.get(workflowSourceSearch(query), config.type).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [config.type, query]);
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
    <PageHeading eyebrow="Institutional history register" title={config.title} description={config.description} actions={<button className="button secondary" onClick={() => exportCsv(rows)}><Icon name="archive" size={15}/>Export all snapshots</button>}/>
    <section className="record-lock-notice"><div><Icon name="archive" size={18}/></div><p><strong>Permanent read-only history</strong><span>History keeps the Management identity, semester Enrollment, operational evidence, final Record, and every captured change together.</span></p></section>
    <section className="record-overview-grid"><RecordMetric label="All records" value={groups.length} detail="individual profiles"/><RecordMetric label="History snapshots" value={rows.length} detail="complete captured changes" tone="violet"/></section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder={`Search all ${config.title.toLowerCase()}…`} searchAriaLabel={`Search ${config.title}`} resultLabel={<>Showing {visible.length} of {groups.length} records</>} className="record-toolbar panel" searchClassName="record-search management-search module-search-field"/>
    <PaginatedDataRegion items={visible} resetKey={`${resource}-${departmentId}-${year}-${query}`} className="history-paginated-region" empty={<DataTableEmptyState icon={<Icon name="archive" size={28}/>} title="No records found" description="Try another search."/>}>{pageItems => <DataTable className="record-register history-management-table panel" headerClassName="record-register-head history-management-head" rowSelector=".record-row-main" columns={["Record identity", "Latest management data", "Last updated"]}><div className="record-register-list">{pageItems.map(group => <RecordRow group={group} detailHref={`/records/${resource}/${encodeURIComponent(group.key)}${detailQuery ? `?${detailQuery}` : ""}`} key={group.key}/>)}</div></DataTable>}</PaginatedDataRegion>
  </div>;
}
