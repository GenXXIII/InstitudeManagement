"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationApi } from "./notification-api";
import type { NotificationHistoryItem } from "./notification-types";

export function NotificationHistoryDetail({ id }: { id: string }) {
  const [item, setItem] = useState<NotificationHistoryItem>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try { setItem(await notificationApi.historyItem(id)); setError(false); }
    catch { setError(true); }
  }, [id]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (error) return <ErrorPage retry={load}/>;
  if (!item) return <LoadingPage/>;

  const recorded = new Date(item.createAt);
  return <>
    <PageHeading eyebrow="Announce" title="Read notification" description="Read-only notification history." actions={<Link className="button secondary" href="/announce/history">Back to history</Link>}/>
    <article className="panel notification-detail">
      <header><time><span>{recorded.toLocaleDateString()}</span><strong>{recorded.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</strong></time></header>
      <section><span>Type</span><strong className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</strong></section>
      <section><span>Title</span><h2>{item.title}</h2></section>
      <section><span>Announcement detail</span><p>{item.message}</p></section>
    </article>
  </>;
}
