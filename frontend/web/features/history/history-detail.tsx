"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { HistoryEntry } from "./components/history-entry";
import { historyApi } from "./history-api";
import { recordTypes } from "./history-config";
import type { RecordGroup } from "./history-types";
import { displayValue, formatDate, groupRecords, isHistoryFieldVisible, pretty } from "./history-utils";

export function HistoryDetail({ resource, id }: { resource: string; id: string }) {
  const searchParams = useSearchParams();
  const config = recordTypes[resource] ?? recordTypes.students;
  const [group, setGroup] = useState<RecordGroup>();
  const [error, setError] = useState(false);
  const key = decodeURIComponent(id);

  const load = useCallback(async () => {
    try {
      const item = groupRecords(await historyApi.get("", config.type)).find(candidate => candidate.key === key);
      if (!item) throw new Error("Record not found");
      setGroup(item);
      setError(false);
    } catch {
      setError(true);
    }
  }, [config.type, key]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (error) return <ErrorPage retry={load}/>;
  if (!group) return <LoadingPage/>;

  const query = searchParams.toString();
  const backHref = `/records/${resource}${query ? `?${query}` : ""}`;
  const latest = group.entries[0];
  return <div className="viewport-data-page history-detail-viewport-page">
    <PageHeading eyebrow="Permanent read-only history" title={group.subject} description={`${group.type} · ${group.entries.length} recorded snapshot${group.entries.length === 1 ? "" : "s"}`} actions={<Link className="button secondary" href={backHref}>Back to history</Link>}/>
    <section className="history-detail-scroll">
      <article className="panel history-detail-summary">
        <header><div><span>Latest captured snapshot</span><strong>{group.subject}</strong></div><time>{formatDate(latest.date)}</time></header>
        <div>{group.values.filter(([name]) => isHistoryFieldVisible(name)).map(([name, value]) => <section key={name}><span>{pretty(name)}</span><strong>{displayValue(name, value)}</strong></section>)}</div>
      </article>
      <section className="record-row-history history-detail-timeline"><div className="record-history-heading"><strong>Complete lifecycle and data snapshots</strong><span>Newest snapshot first</span></div>{group.entries.map(entry => <HistoryEntry entry={entry} key={entry.id}/>)}</section>
    </section>
  </div>;
}
