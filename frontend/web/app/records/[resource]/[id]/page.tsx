"use client";

import { useParams } from "next/navigation";
import { HistoryDetail } from "@/features/history/history-detail";

export default function HistoryDetailPage() {
  const { resource, id } = useParams<{ resource: string; id: string }>();
  return <HistoryDetail resource={resource} id={id}/>;
}
