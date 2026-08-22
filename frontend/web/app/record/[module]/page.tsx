"use client";

import { useParams } from "next/navigation";
import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";

export default function RecordPage() {
  const { module } = useParams<{ module: string }>();
  return <OperationalRecordWorkspace module={module}/>;
}
