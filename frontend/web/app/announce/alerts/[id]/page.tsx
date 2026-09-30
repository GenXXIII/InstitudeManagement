import { AlertDetail } from "@/features/notifications/announcements/alert-detail";

export default async function AlertDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <AlertDetail id={id}/>;
}
