"use client";

import { useParams } from "next/navigation";
import { OperationalRecordWorkspace } from "@/features/record/operational-record-workspace";
import { ResultWorkspace } from "@/features/results/result-workspace";

export default function RecordPage() {
  const { module } = useParams<{ module: string }>();
  return module === "results" ? <ResultWorkspace mode="record"/> : <OperationalRecordWorkspace module={module}/>;
}
