"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationApi } from "./notification-api";
import type { NotificationItem } from "./notification-types";

export function NotificationDetail({ id }: { id: string }) {
  const [item, setItem] = useState<NotificationItem>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      setItem(await notificationApi.readNotification(id));
      setError(false);
      window.dispatchEvent(new Event("ink:notifications-changed"));
    } catch {
      setError(true);
    }
  }, [id]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (error) return <ErrorPage retry={load}/>;
  if (!item) return <LoadingPage/>;

  const created = new Date(item.createAt);
  return <>
    <PageHeading eyebrow="Announce" title="Notification detail" description="Read the complete institute notification." actions={<Link className="button secondary" href="/announce/notifications">Back to notifications</Link>}/>
    <article className="panel notification-detail notification-full-detail">
      <header><div><span>NotificationCode</span><strong className="management-code-value">{item.notificationCode}</strong></div><time><span>{created.toLocaleDateString()}</span><strong>{created.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</strong></time></header>
      <section className="notification-detail-grid" aria-label="Notification information">
        <div><span>Notification code</span><strong className="management-code-value">{item.notificationCode}</strong></div>
        <div><span>Type</span><strong className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</strong></div>
        <div><span>Severity</span><strong className={`table-status notification-severity-${item.severity.toLowerCase()}`}>{item.severity}</strong></div>
        <div><span>Read state</span><strong className="table-status">{item.isRead ? "Read" : "Unread"}</strong></div>
        <div><span>Received date</span><strong>{created.toLocaleDateString()}</strong></div>
        <div><span>Received time</span><strong>{created.toLocaleTimeString()}</strong></div>
      </section>
      <section><span>Title</span><h2>{item.title}</h2></section>
      <section><span>Full notification detail</span><p>{item.message}</p></section>
    </article>
  </>;
}
