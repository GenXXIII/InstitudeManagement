"use client";

import { useParams } from "next/navigation";
import { OperationalRecordDetail } from "@/features/record/operational-record-detail";

export default function RecordHistoryDetailPage() {
  const { module, id } = useParams<{ module: string; id: string }>();
  return <OperationalRecordDetail module={module} id={id} history/>;
}
