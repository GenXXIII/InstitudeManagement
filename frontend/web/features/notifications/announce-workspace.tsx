"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationApi } from "./notification-api";
import type { AnnouncementDraft, AnnouncementItem, NotificationDraft, NotificationHistoryItem, NotificationItem } from "./notification-types";

const emptyAlert: AnnouncementDraft = { type: "General", title: "", message: "" };

export function AnnounceWorkspace({ module }: { module: string }) {
  const router = useRouter();
  const current = ["notifications", "alerts", "history"].includes(module) ? module : "notifications";
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

  const load = useCallback(async () => {
    try {
      const [nextNotifications, nextAlerts, nextHistory] = await Promise.all([notificationApi.notifications(), notificationApi.alerts(), notificationApi.history()]);
      setNotifications(nextNotifications); setAlerts(nextAlerts); setHistory(nextHistory); setReady(true); setLoadError(false);
    } catch { setLoadError(true); }
  }, []);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (loadError) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function saveNotification() {
    if (!editingNotification || !notificationDraft) return;
    setSaving(true); setError("");
    try { await notificationApi.updateNotification(editingNotification, notificationDraft); notifyBell(); setEditingNotification(undefined); setNotificationDraft(undefined); await load(); }
    catch (reason) { setError(message(reason)); } finally { setSaving(false); }
  }
  async function removeNotification(id: string) {
    if (!confirm("Remove this notification? Its history will remain read-only.")) return;
    try { await notificationApi.removeNotification(id); notifyBell(); await load(); } catch (reason) { setError(message(reason)); }
  }
  async function saveAlert() {
    setSaving(true); setError("");
    try { if (editingAlert) await notificationApi.updateAlert(editingAlert, alertDraft); else await notificationApi.createAlert(alertDraft); notifyBell(); setEditingAlert(undefined); setAlertDraft(emptyAlert); await load(); }
    catch (reason) { setError(message(reason)); } finally { setSaving(false); }
  }
  async function removeAlert(id: string) {
    if (!confirm("Remove this alert? Its history will remain read-only.")) return;
    try { await notificationApi.removeAlert(id); notifyBell(); await load(); } catch (reason) { setError(message(reason)); }
  }

  const copy = current === "alerts"
    ? { eyebrow: "Announce", title: "Alert", description: "Publish institute-wide general, attendance, emergency, or semester result alerts." }
    : current === "history"
      ? { eyebrow: "Announce", title: "History", description: "Read-only history of every notification and alert lifecycle event." }
      : { eyebrow: "Announce", title: "Notification", description: "Review, edit, mark, or remove current system notifications." };

  return <>
    <PageHeading eyebrow={copy.eyebrow} title={copy.title} description={copy.description}/>
    {error && <section className="management-rule-error" role="alert"><Icon name="bell" size={16}/><div><strong>Could not apply change</strong><span>{error}</span></div><button onClick={() => setError("")}>Dismiss</button></section>}
    {current === "notifications" && <NotificationRegister rows={notifications} editing={editingNotification} draft={notificationDraft} saving={saving} onDraft={setNotificationDraft} onOpen={id => router.push(`/announce/notifications/${id}`)} onEdit={item => { setEditingNotification(item.id); setNotificationDraft({ title: item.title, message: item.message, severity: item.severity, isRead: item.isRead }); }} onSave={saveNotification} onCancel={() => setEditingNotification(undefined)} onRemove={removeNotification}/>} 
    {current === "alerts" && <AlertRegister rows={alerts} editing={editingAlert} draft={alertDraft} saving={saving} onDraft={setAlertDraft} onSave={saveAlert} onCancel={() => { setEditingAlert(undefined); setAlertDraft(emptyAlert); }} onEdit={item => { setEditingAlert(item.id); setAlertDraft({ type: item.type, title: item.title, message: item.message }); }} onRemove={removeAlert}/>} 
    {current === "history" && <HistoryRegister rows={history} onOpen={id => router.push(`/announce/history/${id}`)}/>} 
  </>;
}

function NotificationRegister({ rows, editing, draft, saving, onDraft, onOpen, onEdit, onSave, onCancel, onRemove }: { rows: NotificationItem[]; editing?: string; draft?: NotificationDraft; saving: boolean; onDraft: (value: NotificationDraft) => void; onOpen: (id: string) => void; onEdit: (item: NotificationItem) => void; onSave: () => void; onCancel: () => void; onRemove: (id: string) => void }) {
  return <section className="panel notification-inbox"><div className="notification-inbox-head"><span/><span>Type</span><span>Title and preview</span><span>Received</span><span>Actions</span></div>{rows.map(item => editing === item.id && draft
    ? <div className="notification-inbox-edit" key={item.id}><input aria-label="Notification title" value={draft.title} onChange={event => onDraft({ ...draft, title: event.target.value })}/><textarea aria-label="Notification detail" value={draft.message} onChange={event => onDraft({ ...draft, message: event.target.value })}/><select aria-label="Severity" value={draft.severity} onChange={event => onDraft({ ...draft, severity: event.target.value as NotificationDraft["severity"] })}><option>Info</option><option>Warning</option><option>Critical</option></select><label><input type="checkbox" checked={draft.isRead} onChange={event => onDraft({ ...draft, isRead: event.target.checked })}/>Read</label><div><button onClick={onCancel}>Cancel</button><button disabled={saving} onClick={onSave}>Save</button></div></div>
    : <article className={`notification-inbox-row ${item.isRead ? "" : "unread"}`} role="link" tabIndex={0} onClick={() => onOpen(item.id)} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") onOpen(item.id); }} key={item.id}><i/><span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span><div className="notification-inbox-copy"><strong>{item.title}</strong><span>{item.message}</span></div><time>{new Date(item.createAt).toLocaleDateString()}</time><div className="notification-row-actions"><button title="Edit notification" aria-label="Edit notification" onClick={event => { event.stopPropagation(); onEdit(item); }}><Icon name="edit" size={14}/></button><button title="Remove notification" aria-label="Remove notification" onClick={event => { event.stopPropagation(); onRemove(item.id); }}><Icon name="trash" size={14}/></button></div></article>)}{!rows.length && <div className="empty-state"><strong>No notifications</strong><span>Published alerts and system notifications will appear here.</span></div>}</section>;
}
function AlertRegister({ rows, editing, draft, saving, onDraft, onSave, onCancel, onEdit, onRemove }: { rows: AnnouncementItem[]; editing?: string; draft: AnnouncementDraft; saving: boolean; onDraft: (value: AnnouncementDraft) => void; onSave: () => void; onCancel: () => void; onEdit: (item: AnnouncementItem) => void; onRemove: (id: string) => void }) {
  return <><section className="panel announce-alert-form"><label><span>Alert type</span><select value={draft.type} onChange={event => onDraft({ ...draft, type: event.target.value as AnnouncementDraft["type"] })}><option>General</option><option>Attendance</option><option>Emergency</option><option>Result</option></select></label><label><span>Title</span><input value={draft.title} onChange={event => onDraft({ ...draft, title: event.target.value })}/></label><label><span>Announcement detail</span><textarea value={draft.message} onChange={event => onDraft({ ...draft, message: event.target.value })}/></label><div>{editing && <button className="button secondary" onClick={onCancel}>Cancel</button>}<button className="button primary" disabled={saving || !draft.title.trim() || !draft.message.trim()} onClick={onSave}><Icon name={editing ? "edit" : "plus"} size={15}/>{saving ? "Saving..." : editing ? "Save alert" : "Announce to all"}</button></div></section><section className="panel announce-table alert-register"><div className="announce-table-head"><span>AnnouncementCode</span><span>Title and detail</span><span>Type</span><span>Create At</span><span>Actions</span></div>{rows.map(item => <article className="announce-table-row" key={item.id}><strong className="management-code-value">{item.announcementCode}</strong><div><strong>{item.title}</strong><span>{item.message}</span></div><span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span><time>{date(item.createAt)}</time><div className="notification-row-actions"><button title="Edit alert" aria-label="Edit alert" onClick={() => onEdit(item)}><Icon name="edit" size={14}/></button><button title="Remove alert" aria-label="Remove alert" onClick={() => onRemove(item.id)}><Icon name="trash" size={14}/></button></div></article>)}{!rows.length && <div className="empty-state"><strong>No active alerts</strong><span>Create an institute announcement above.</span></div>}</section></>;
}
function HistoryRegister({ rows, onOpen }: { rows: NotificationHistoryItem[]; onOpen: (id: string) => void }) {
  return <section className="panel notification-inbox"><div className="notification-inbox-head"><span/><span>Type</span><span>Title and preview</span><span>Recorded</span><span>Action</span></div>{rows.map(item => <article className="notification-inbox-row" role="link" tabIndex={0} onClick={() => onOpen(item.id)} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") onOpen(item.id); }} key={item.id}><i/><span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span><div className="notification-inbox-copy"><strong>{item.title}</strong><span>{item.message}</span></div><time>{new Date(item.createAt).toLocaleDateString()}</time><span className="table-status">{item.action}</span></article>)}{!rows.length && <div className="empty-state"><strong>No read history</strong><span>Notifications move here after they are opened.</span></div>}</section>;
}
function date(value: string) { return new Date(value).toLocaleString(); }
function message(reason: unknown) { return reason instanceof Error ? reason.message : "Could not apply this change."; }
function notifyBell() { window.dispatchEvent(new Event("ink:notifications-changed")); }
