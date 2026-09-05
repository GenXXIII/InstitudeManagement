"use client";

import { useParams } from "next/navigation";
import { NotificationHistoryDetail } from "@/features/notifications/history/notification-history-detail";

export default function NotificationHistoryDetailPage() {
  const { id } = useParams<{ id: string }>();
  return <NotificationHistoryDetail id={id}/>;
}
