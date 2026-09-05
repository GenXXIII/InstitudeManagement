"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
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
import type { NotificationDraft, NotificationItem } from "./notifications/notification-types";
import { AnnounceOverview } from "./overview/announce-overview";

const emptyAlert: AnnouncementDraft = { announcementCode: "", type: "General", title: "", message: "" };

export function AnnounceWorkspace({ module }: { module: string }) {
  const router = useRouter();
  const current = ["overview", "notifications", "alerts", "history"].includes(module) ? module : "overview";
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [alerts, setAlerts] = useState<AnnouncementItem[]>([]);
  const [history, setHistory] = useState<NotificationHistoryItem[]>([]);
  const [ready, setReady] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [error, setError] = useState("");
  const [editingNotification, setEditingNotification] = useState<string>();
  const [notificationDraft, setNotificationDraft] = useState<NotificationDraft>();
  const [editingAlert, setEditingAlert] = useState<string>();
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
  const notificationPages = useDataPagination(notifications, "notification-register");
  const alertPages = useDataPagination(alerts, "alert-register");
  const historyPages = useDataPagination(history, "notification-history-register");
  if (loadError) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function saveNotification() {
    if (!editingNotification || !notificationDraft) return;
    setSaving(true); setError("");
    try { await notificationsApi.update(editingNotification, notificationDraft); notifyBell(); setEditingNotification(undefined); setNotificationDraft(undefined); await load(); }
    catch (reason) { setError(message(reason)); } finally { setSaving(false); }
  }
  async function removeNotification(id: string) {
    if (!confirm("Remove this notification? Its history will remain read-only.")) return;
    try { await notificationsApi.remove(id); notifyBell(); await load(); } catch (reason) { setError(message(reason)); }
  }
  async function markAllNotificationsAsRead() {
    if (!notifications.length || markingAll) return;
    setMarkingAll(true); setError("");
    try { await notificationsApi.markAllRead(); notifyBell(); await load(); }
    catch (reason) { setError(message(reason)); } finally { setMarkingAll(false); }
  }
  async function saveAlert() {
    setSaving(true); setError("");
    try { if (editingAlert) await announcementsApi.update(editingAlert, alertDraft); else await announcementsApi.create(alertDraft); notifyBell(); setEditingAlert(undefined); setAlertDraft(emptyAlert); await load(); }
    catch (reason) { setError(message(reason)); } finally { setSaving(false); }
  }
  async function removeAlert(id: string) {
    if (!confirm("Remove this alert? Its history will remain read-only.")) return;
    try { await announcementsApi.remove(id); notifyBell(); await load(); } catch (reason) { setError(message(reason)); }
  }

  const copy = current === "overview"
    ? { eyebrow: "Announce", title: "Announce Overview", description: "Review every coded notification, institute alert, and read-only announcement history from one place." }
    : current === "alerts"
    ? { eyebrow: "Announce", title: "Alert", description: "Publish institute-wide general, attendance, emergency, or semester result alerts." }
    : current === "history"
      ? { eyebrow: "Announce", title: "History", description: "Read-only history of every notification and alert lifecycle event." }
      : { eyebrow: "Announce", title: "Notification", description: "Review, edit, mark, or remove current system notifications." };

  return <div className="viewport-data-page announce-viewport-page">
    <PageHeading eyebrow={copy.eyebrow} title={copy.title} description={copy.description} actions={current === "notifications" ? <button className="button secondary notification-mark-all-button" disabled={!notifications.length || markingAll} onClick={() => void markAllNotificationsAsRead()}>{markingAll ? "Marking all as read..." : "Mark all as read"}</button> : undefined}/>
    {error && <section className="management-rule-error" role="alert"><Icon name="bell" size={16}/><div><strong>Could not apply change</strong><span>{error}</span></div><button onClick={() => setError("")}>Dismiss</button></section>}
    {current === "overview" && <AnnounceOverview notifications={notifications} alerts={alerts} history={history}/>}
    {current === "notifications" && <section className="announce-paginated-region"><NotificationRegister rows={notificationPages.pageItems} editing={editingNotification} draft={notificationDraft} saving={saving} onDraft={setNotificationDraft} onOpen={id => router.push(`/announce/notifications/${id}`)} onEdit={item => { setEditingNotification(item.id); setNotificationDraft({ title: item.title, message: item.message, severity: item.severity, isRead: item.isRead }); }} onSave={saveNotification} onCancel={() => setEditingNotification(undefined)} onRemove={removeNotification}/><DataPagination page={notificationPages.page} pageCount={notificationPages.pageCount} total={notifications.length} onPage={notificationPages.setPage}/></section>}
    {current === "alerts" && <section className="announce-paginated-region"><AlertRegister rows={alertPages.pageItems} editing={editingAlert} draft={alertDraft} saving={saving} onDraft={setAlertDraft} onSave={saveAlert} onCancel={() => { setEditingAlert(undefined); setAlertDraft(emptyAlert); }} onEdit={item => { setEditingAlert(item.id); setAlertDraft({ announcementCode: item.announcementCode, type: item.type, title: item.title, message: item.message }); }} onRemove={removeAlert}/><DataPagination page={alertPages.page} pageCount={alertPages.pageCount} total={alerts.length} onPage={alertPages.setPage}/></section>}
    {current === "history" && <section className="announce-paginated-region"><NotificationHistoryRegister rows={historyPages.pageItems} onOpen={code => router.push(`/announce/history/${encodeURIComponent(code)}`)}/><DataPagination page={historyPages.page} pageCount={historyPages.pageCount} total={history.length} onPage={historyPages.setPage}/></section>}
  </div>;
}
function message(reason: unknown) { return reason instanceof Error ? reason.message : "Could not apply this change."; }
function notifyBell() { window.dispatchEvent(new Event("ink:notifications-changed")); }
