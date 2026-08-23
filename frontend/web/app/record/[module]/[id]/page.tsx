"use client";

import { useParams } from "next/navigation";
import { OperationalRecordDetail } from "@/features/record/operational-record-detail";

export default function RecordDetailPage() {
  const { module, id } = useParams<{ module: string; id: string }>();
  return <OperationalRecordDetail module={module} id={id}/>;
}
