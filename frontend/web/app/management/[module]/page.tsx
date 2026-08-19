"use client";

import { useParams } from "next/navigation";
import { ManagementWorkspace } from "@/components/management-workspace";

export default function ManagementPage() {
  const { module } = useParams<{ module: string }>();
  return <ManagementWorkspace module={module}/>;
}
