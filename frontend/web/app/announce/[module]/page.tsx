"use client";

import { useParams } from "next/navigation";
import { AnnounceWorkspace } from "@/features/notifications/announce-workspace";

export default function AnnouncePage() {
  const { module } = useParams<{ module: string }>();
  return <AnnounceWorkspace module={module}/>;
}
