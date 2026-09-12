"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { BrowserBackButton } from "@/components/browser-back-button";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { notificationsApi } from "./notification-api";
import type { NotificationDraft, NotificationItem } from "./notification-types";

export function NotificationDetail({ id }: { id: string }) {
  const router = useRouter();
  const [item, setItem] = useState<NotificationItem>();
  const [draft, setDraft] = useState<NotificationDraft>();
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [actionError, setActionError] = useState("");

  const load = useCallback(async () => {
    try {
      setItem(await notificationsApi.markRead(id));
      setLoadError(false);
      notifyBell();
    } catch {
      setLoadError(true);
    }
  }, [id]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  if (loadError) return <ErrorPage retry={load}/>;
  if (!item) return <LoadingPage/>;

  function beginEdit() {
    setDraft({ title: item!.title, message: item!.message, severity: item!.severity, isRead: item!.isRead });
    setEditing(true);
    setActionError("");
  }

  function cancelEdit() {
    setDraft(undefined);
    setEditing(false);
    setActionError("");
  }

  async function save() {
    const current = item;
    if (!current || !draft || !draft.title.trim() || !draft.message.trim()) return;
    setSaving(true); setActionError("");
    try {
      setItem(await notificationsApi.update(current.id, draft));
      setDraft(undefined);
      setEditing(false);
      notifyBell();
    } catch (reason) {
      setActionError(message(reason));
    } finally {
      setSaving(false);
    }
  }

  async function remove() {
    const current = item;
    if (!current) return;
    if (!confirm("Remove this notification? Its history will remain read-only.")) return;
    setSaving(true); setActionError("");
    try {
      await notificationsApi.remove(current.id);
      notifyBell();
      router.replace("/announce/notifications");
    } catch (reason) {
      setActionError(message(reason));
      setSaving(false);
    }
  }

  const created = new Date(item.createAt);
  const displayedSeverity = draft?.severity ?? item.severity;
  return <>
    <PageHeading eyebrow="Announce" title="Notification detail" description="Read and manage the complete institute notification." actions={<BrowserBackButton>Back to notifications</BrowserBackButton>}/>
    <article className="panel notification-detail notification-full-detail">
      <header>
        <div><span>NotificationCode</span><strong className="management-code-value">{item.notificationCode}</strong></div>
        <div className="notification-detail-header-side">
          <time><span>{created.toLocaleDateString()}</span><strong>{created.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</strong></time>
          <div className="notification-detail-header-actions">
            {editing
              ? <><button className="button secondary" disabled={saving} onClick={cancelEdit}>Cancel</button><button className="button primary" disabled={saving || !draft?.title.trim() || !draft.message.trim()} onClick={() => void save()}>{saving ? "Saving..." : "Save changes"}</button></>
              : <button className="button secondary" disabled={saving} onClick={beginEdit}>Edit</button>}
            <button className="button notification-detail-remove" disabled={saving} onClick={() => void remove()}>Remove</button>
          </div>
        </div>
      </header>
      {actionError && <div className="notification-detail-action-error" role="alert">{actionError}</div>}
      <section className="notification-detail-grid" aria-label="Notification information">
        <div><span>Notification code</span><strong className="management-code-value">{item.notificationCode}</strong></div>
        <div><span>Type</span><strong className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</strong></div>
        <div><span>Severity</span><strong className={`table-status notification-severity-${displayedSeverity.toLowerCase()}`}>{displayedSeverity}</strong></div>
        <div><span>Read state</span><strong className="table-status">{item.isRead ? "Read" : "Unread"}</strong></div>
        <div><span>Received date</span><strong>{created.toLocaleDateString()}</strong></div>
        <div><span>Received time</span><strong>{created.toLocaleTimeString()}</strong></div>
      </section>
      {editing && draft
        ? <section className="notification-detail-editor" aria-label="Edit notification">
          <label><span>Title</span><input value={draft.title} onChange={event => setDraft({ ...draft, title: event.target.value })}/></label>
          <label><span>Severity</span><select value={draft.severity} onChange={event => setDraft({ ...draft, severity: event.target.value as NotificationDraft["severity"] })}><option>Info</option><option>Warning</option><option>Critical</option></select></label>
          <label><span>Full notification detail</span><textarea value={draft.message} onChange={event => setDraft({ ...draft, message: event.target.value })}/></label>
        </section>
        : <><section><span>Title</span><h2>{item.title}</h2></section><section><span>Full notification detail</span><p>{item.message}</p></section></>}
    </article>
  </>;
}

function notifyBell() { window.dispatchEvent(new Event("ink:notifications-changed")); }
function message(reason: unknown) { return reason instanceof Error ? reason.message : "Could not apply this change."; }
