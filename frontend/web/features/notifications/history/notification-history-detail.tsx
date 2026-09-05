"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationHistoryApi } from "./notification-history-api";
import type { NotificationHistoryItem } from "./notification-history-types";

export function NotificationHistoryDetail({ id }: { id: string }) {
  const [item, setItem] = useState<NotificationHistoryItem>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try { setItem(await notificationHistoryApi.getItem(id)); setError(false); }
    catch { setError(true); }
  }, [id]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (error) return <ErrorPage retry={load}/>;
  if (!item) return <LoadingPage/>;

  const recorded = new Date(item.createAt);
  return <>
    <PageHeading eyebrow="Announce" title="Read notification" description="Read-only notification lifecycle entry." actions={<Link className="button secondary" href="/announce/history">Back to notification history</Link>}/>
    <article className="panel notification-detail notification-full-detail">
      <header><div><span>NotificationHistoryCode</span><strong className="management-code-value">{item.notificationHistoryCode}</strong></div><time><span>{recorded.toLocaleDateString()}</span><strong>{recorded.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</strong></time></header>
      <section className="notification-detail-grid" aria-label="Notification history information">
        <div><span>Notification history code</span><strong className="management-code-value">{item.notificationHistoryCode}</strong></div>
        <div><span>Original notification code</span><strong className="management-code-value">{item.sourceCode}</strong></div>
        <div><span>Source kind</span><strong>{item.kind}</strong></div>
        <div><span>Type</span><strong className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</strong></div>
        <div><span>Lifecycle action</span><strong className="table-status">{item.action}</strong></div>
        <div><span>Recorded at</span><strong>{recorded.toLocaleString()}</strong></div>
      </section>
      <section><span>Title</span><h2>{item.title}</h2></section>
      <section><span>Full notification detail</span><p>{item.message}</p></section>
    </article>
  </>;
}
