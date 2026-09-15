"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { PaginatedDataRegion } from "@/components/data-table";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationHistoryApi } from "@/features/notifications/history/notification-history-api";
import { NotificationHistoryRegister } from "@/features/notifications/history/notification-history-register";
import type { NotificationHistoryItem } from "@/features/notifications/history/notification-history-types";

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

  return <div className="viewport-data-page history-viewport-page announce-viewport-page">
    <PageHeading eyebrow="Permanent lifecycle archive" title="Notification & Alert History" description="Combined read-only notification and alert lifecycle events, ordered by recorded date with the newest entry first."/>
    <PaginatedDataRegion items={history} resetKey="notification-history-register" className="history-paginated-region announce-paginated-region">
      {pageItems => <NotificationHistoryRegister rows={pageItems} onOpen={code => router.push(`/records/notifications/${encodeURIComponent(code)}`)}/>}
    </PaginatedDataRegion>
  </div>;
}
