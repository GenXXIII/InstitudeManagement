"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { OperationalRecordRow } from "./components/operational-record-row";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

export function OperationalRecordDetail({ module, id, history = false }: { module: string; id: string; history?: boolean }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const [item, setItem] = useState<OperationalRecord>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const rows = await recordApi.get(module, "", departmentId, history);
      const record = rows.find(row => row.id === id);
      if (!record) throw new Error("Record not found");
      setItem(record);
      setError(false);
    } catch {
      setError(true);
    }
  }, [departmentId, history, id, module]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (error) return <ErrorPage retry={load}/>;
  if (!item) return <LoadingPage/>;

  const query = searchParams.toString();
  const backHref = `${history ? "/record-history" : "/record"}/${module}${query ? `?${query}` : ""}`;
  return <div className="viewport-data-page record-detail-viewport-page">
    <PageHeading eyebrow={history ? "Read-only record history" : "Active-semester record"} title={item.subject} description={`${item.identifier} · ${item.summary}`} actions={<Link className="button secondary" href={backHref}>Back to records</Link>}/>
    <section className="record-detail-scroll">
      <OperationalRecordRow row={item} editable={!history} showStatus={!history} detailPage onUpdated={load}/>
    </section>
  </div>;
}
