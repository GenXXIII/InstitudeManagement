"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ServerPaginatedDataRegion } from "@/components/data-table";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationHistoryApi } from "@/features/notifications/history/notification-history-api";
import { NotificationHistoryRegister } from "@/features/notifications/history/notification-history-register";
import type { NotificationHistoryItem } from "@/features/notifications/history/notification-history-types";
import { RecordMetric } from "./components/record-metric";
import { emptyPage, type PagedResult } from "@/lib/pagination";

export function NotificationHistoryWorkspace() {
  const router = useRouter();
  const [history, setHistory] = useState<PagedResult<NotificationHistoryItem>>(() => emptyPage());
  const [page, setPage] = useState(1);
  const [counts, setCounts] = useState({ notifications: 0, alerts: 0 });
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const [nextHistory, notifications, alerts] = await Promise.all([
        notificationHistoryApi.getPage("", { page }),
        notificationHistoryApi.getPage("notification", { pageSize: 1 }),
        notificationHistoryApi.getPage("alert", { pageSize: 1 }),
      ]);
      setHistory(nextHistory);
      setCounts({ notifications: notifications.totalCount, alerts: alerts.totalCount });
      setError(false);
    } catch {
      setError(true);
    }
  }, [page]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  if (error) return <ErrorPage retry={load}/>;
  if (!history) return <LoadingPage/>;

  return <div className="viewport-data-page history-viewport-page announce-viewport-page">
    <PageHeading eyebrow="Permanent lifecycle archive" title="Notification & Alert History" description="Combined read-only notification and alert lifecycle events, ordered by recorded date with the newest entry first."/>
    <section className="record-overview-grid notification-history-totals"><RecordMetric label="All records" value={history.totalCount} detail="Notification and Alert"/><RecordMetric label="Notifications" value={counts.notifications} detail="Recorded lifecycle events" tone="violet"/><RecordMetric label="Alerts" value={counts.alerts} detail="Recorded lifecycle events" tone="green"/></section>
    <ServerPaginatedDataRegion result={history} onPage={setPage} className="history-paginated-region announce-paginated-region">
      {pageItems => <NotificationHistoryRegister rows={pageItems} onOpen={code => router.push(`/records/notifications/${encodeURIComponent(code)}`)}/>}
    </ServerPaginatedDataRegion>
  </div>;
}
