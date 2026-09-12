"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { PaginatedDataRegion } from "@/components/data-table";
import { CodeRecommendation } from "@/components/code-recommendation";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { AlertRegister } from "./announcements/alert-register";
import { announcementsApi } from "./announcements/announcement-api";
import type { AnnouncementDraft, AnnouncementItem } from "./announcements/announcement-types";
import { notificationHistoryApi } from "./history/notification-history-api";
import { NotificationHistoryRegister } from "./history/notification-history-register";
import type { NotificationHistoryItem } from "./history/notification-history-types";
import { notificationsApi } from "./notifications/notification-api";
import { NotificationRegister } from "./notifications/notification-register";
import type { NotificationItem } from "./notifications/notification-types";
import { AnnounceOverview } from "./overview/announce-overview";
import { formatAlertCode } from "@/lib/workflow-code";
import { recommendedCodeFromError } from "@/lib/code-recommendation";

const emptyAlert: AnnouncementDraft = { announcementCode: "", type: "", title: "", message: "" };

export function AnnounceWorkspace({ module }: { module: string }) {
  const router = useRouter();
  const current = ["overview", "notifications", "alerts", "history"].includes(module) ? module : "overview";
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [alerts, setAlerts] = useState<AnnouncementItem[]>([]);
  const [history, setHistory] = useState<NotificationHistoryItem[]>([]);
  const [ready, setReady] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [error, setError] = useState("");
  const [alertDraft, setAlertDraft] = useState<AnnouncementDraft>(emptyAlert);
  const [saving, setSaving] = useState(false);
  const [markingAll, setMarkingAll] = useState(false);

  const load = useCallback(async () => {
    try {
      const [nextNotifications, nextAlerts, nextHistory] = await Promise.all([notificationsApi.get(), announcementsApi.get(), notificationHistoryApi.get()]);
      setNotifications(nextNotifications); setAlerts(nextAlerts); setHistory(nextHistory); setReady(true); setLoadError(false);
    } catch { setLoadError(true); }
  }, []);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (loadError) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function markAllNotificationsAsRead() {
    if (!notifications.some(item => !item.isRead) || markingAll) return;
    setMarkingAll(true); setError("");
    try { await notificationsApi.markAllRead(); notifyBell(); await load(); }
    catch (reason) { setError(message(reason)); } finally { setMarkingAll(false); }
  }
  async function saveAlert() {
    if (!alertDraft.type) return;
    setSaving(true); setError("");
    const submittedDraft = { ...alertDraft, announcementCode: formatAlertCode(alertDraft.announcementCode) };
    try { await announcementsApi.create(submittedDraft); notifyBell(); setAlertDraft(emptyAlert); await load(); }
    catch (reason) { setError(message(reason)); } finally { setSaving(false); }
  }

  const copy = current === "overview"
    ? { eyebrow: "Announce", title: "Announce Overview", description: "Review every coded notification, institute alert, and read-only announcement history from one place." }
    : current === "alerts"
    ? { eyebrow: "Announce", title: "Alert", description: "Publish institute-wide alerts with a required permanent code formatted by Administration > Code formats." }
    : current === "history"
      ? { eyebrow: "Announce", title: "History", description: "Read-only history of every notification and alert lifecycle event." }
      : { eyebrow: "Announce", title: "Notification", description: "Review, edit, mark, or remove current system notifications." };
  const recommendation = recommendedCodeFromError(error);

  return <div className="viewport-data-page announce-viewport-page">
    <PageHeading eyebrow={copy.eyebrow} title={copy.title} description={copy.description} actions={current === "notifications" ? <button className="button secondary notification-mark-all-button" disabled={!notifications.some(item => !item.isRead) || markingAll} onClick={() => void markAllNotificationsAsRead()}>{markingAll ? "Marking all as read..." : "Mark all as read"}</button> : undefined}/>
    {recommendation ? <CodeRecommendation code={recommendation} onUse={() => { setAlertDraft(currentDraft => ({ ...currentDraft, announcementCode: recommendation })); setError(""); }}/>
      : error && <section className="management-rule-error" role="alert"><Icon name="bell" size={16}/><div><strong>Could not apply change</strong><span>{error}</span></div><button onClick={() => setError("")}>Dismiss</button></section>}
    {current === "overview" && <AnnounceOverview notifications={notifications} alerts={alerts} history={history}/>}
    {current === "notifications" && <PaginatedDataRegion items={notifications} resetKey="notification-register" className="announce-paginated-region">{pageItems => <NotificationRegister rows={pageItems} onOpen={id => router.push(`/announce/notifications/${id}`)}/>}</PaginatedDataRegion>}
    {current === "alerts" && <PaginatedDataRegion items={alerts} resetKey="alert-register" className="announce-paginated-region">{pageItems => <AlertRegister rows={pageItems} draft={alertDraft} saving={saving} onDraft={setAlertDraft} onOpen={id => router.push(`/announce/alerts/${id}`)} onSave={saveAlert}/>}</PaginatedDataRegion>}
    {current === "history" && <PaginatedDataRegion items={history} resetKey="notification-history-register" className="announce-paginated-region">{pageItems => <NotificationHistoryRegister rows={pageItems} onOpen={code => router.push(`/announce/history/${encodeURIComponent(code)}`)}/>}</PaginatedDataRegion>}
  </div>;
}
function message(reason: unknown) { return reason instanceof Error ? reason.message : "Could not apply this change."; }
function notifyBell() { window.dispatchEvent(new Event("ink:notifications-changed")); }
