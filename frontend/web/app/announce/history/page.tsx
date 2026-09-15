import { redirect } from "next/navigation";

export default function LegacyNotificationHistoryPage() {
  redirect("/records/notifications");
}
