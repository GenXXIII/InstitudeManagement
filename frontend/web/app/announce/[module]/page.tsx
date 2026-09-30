import { AnnounceWorkspace } from "@/features/notifications/announce-workspace";

export default async function AnnouncePage({ params }: { params: Promise<{ module: string }> }) {
  const { module } = await params;
  return <AnnounceWorkspace module={module}/>;
}
