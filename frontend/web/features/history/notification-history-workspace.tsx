"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { PaginatedDataRegion } from "@/components/data-table";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationHistoryApi } from "@/features/notifications/history/notification-history-api";
import { NotificationHistoryRegister } from "@/features/notifications/history/notification-history-register";
import type { NotificationHistoryItem } from "@/features/notifications/history/notification-history-types";
import { RecordMetric } from "./components/record-metric";

export function NotificationHistoryWorkspace() {
  const router = useRouter();
  const [history, setHistory] = useState<NotificationHistoryItem[]>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      setHistory(await notificationHistoryApi.get());
      setError(false);
    } catch {
      setError(true);
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  if (error) return <ErrorPage retry={load}/>;
  if (!history) return <LoadingPage/>;
  const notifications = history.filter(item => item.kind.toLowerCase().includes("notification")).length;
  const alerts = history.filter(item => item.kind.toLowerCase().includes("alert")).length;

  return <div className="viewport-data-page history-viewport-page announce-viewport-page">
    <PageHeading eyebrow="Permanent lifecycle archive" title="Notification & Alert History" description="Combined read-only notification and alert lifecycle events, ordered by recorded date with the newest entry first."/>
    <section className="record-overview-grid notification-history-totals"><RecordMetric label="All records" value={history.length} detail="Notification and Alert"/><RecordMetric label="Notifications" value={notifications} detail="Recorded lifecycle events" tone="violet"/><RecordMetric label="Alerts" value={alerts} detail="Recorded lifecycle events" tone="green"/></section>
    <PaginatedDataRegion items={history} resetKey="notification-history-register" className="history-paginated-region announce-paginated-region">
      {pageItems => <NotificationHistoryRegister rows={pageItems} onOpen={code => router.push(`/records/notifications/${encodeURIComponent(code)}`)}/>}
    </PaginatedDataRegion>
  </div>;
}
