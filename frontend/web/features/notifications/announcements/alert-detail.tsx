"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { BrowserBackButton } from "@/components/browser-back-button";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { announcementsApi } from "./announcement-api";
import type { AnnouncementDraft, AnnouncementItem } from "./announcement-types";

export function AlertDetail({ id }: { id: string }) {
  const router = useRouter();
  const [item, setItem] = useState<AnnouncementItem>();
  const [draft, setDraft] = useState<AnnouncementDraft>();
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const [actionError, setActionError] = useState("");

  const load = useCallback(async () => {
    try {
      setItem(await announcementsApi.markRead(id));
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
    setDraft({ announcementCode: item!.announcementCode, type: item!.type, title: item!.title, message: item!.message });
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
      setItem(await announcementsApi.update(current.id, draft));
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
    if (!confirm("Remove this alert? Its history will remain read-only.")) return;
    setSaving(true); setActionError("");
    try {
      await announcementsApi.remove(current.id);
      notifyBell();
      router.replace("/announce/alerts");
    } catch (reason) {
      setActionError(message(reason));
      setSaving(false);
    }
  }

  const created = new Date(item.createAt);
  const displayedType = draft?.type ?? item.type;
  return <>
    <PageHeading eyebrow="Announce" title="Alert detail" description="Read and manage the complete institute alert." actions={<BrowserBackButton>Back to alerts</BrowserBackButton>}/>
    <article className="panel notification-detail notification-full-detail">
      <header>
        <div><span>AlertCode</span><strong className="management-code-value">{item.announcementCode}</strong></div>
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
      <section className="notification-detail-grid" aria-label="Alert information">
        <div><span>Alert code</span><strong className="management-code-value">{item.announcementCode}</strong></div>
        <div><span>Type</span><strong className={`table-status alert-${displayedType.toLowerCase()}`}>{displayedType}</strong></div>
        <div><span>Read state</span><strong className="table-status">{item.isRead ? "Read" : "Unread"}</strong></div>
        <div><span>Created date</span><strong>{created.toLocaleDateString()}</strong></div>
        <div><span>Created time</span><strong>{created.toLocaleTimeString()}</strong></div>
      </section>
      {editing && draft
        ? <section className="notification-detail-editor" aria-label="Edit alert">
          <label><span>Title</span><input value={draft.title} onChange={event => setDraft({ ...draft, title: event.target.value })}/></label>
          <label><span>Alert type</span><select value={draft.type} onChange={event => setDraft({ ...draft, type: event.target.value as AnnouncementDraft["type"] })}><option>General</option><option>Attendance</option><option>Emergency</option><option>Result</option></select></label>
          <label><span>Full alert detail</span><textarea value={draft.message} onChange={event => setDraft({ ...draft, message: event.target.value })}/></label>
        </section>
        : <><section><span>Title</span><h2>{item.title}</h2></section><section><span>Full alert detail</span><p>{item.message}</p></section></>}
    </article>
  </>;
}

function notifyBell() { window.dispatchEvent(new Event("ink:notifications-changed")); }
function message(reason: unknown) { return reason instanceof Error ? reason.message : "Could not apply this change."; }
