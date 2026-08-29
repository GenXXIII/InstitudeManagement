import { redirect } from "next/navigation";

export default function HistoryIndexPage() {
  redirect("/records/overview");
}
