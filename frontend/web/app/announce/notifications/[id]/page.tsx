"use client";

import { useParams } from "next/navigation";
import { NotificationDetail } from "@/features/notifications/notifications/notification-detail";

export default function NotificationDetailPage() {
  const { id } = useParams<{ id: string }>();
  return <NotificationDetail id={id}/>;
}
