import { redirect } from "next/navigation";

export default async function LegacyNotificationHistoryDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  redirect(`/records/notifications/${encodeURIComponent(id)}`);
}
