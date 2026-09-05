import { Icon } from "@/components/icon";
import type { AnnouncementDraft, AnnouncementItem } from "./announcement-types";

export function AlertRegister({ rows, editing, draft, saving, onDraft, onSave, onCancel, onEdit, onRemove }: {
  rows: AnnouncementItem[];
  editing?: string;
  draft: AnnouncementDraft;
  saving: boolean;
  onDraft: (value: AnnouncementDraft) => void;
  onSave: () => void;
  onCancel: () => void;
  onEdit: (item: AnnouncementItem) => void;
  onRemove: (id: string) => void;
}) {
  return <>
    <section className="panel announce-alert-form">
      <label>
        <span>Announcement code</span>
        <input className="management-code-value" value={draft.announcementCode} required onChange={event => onDraft({ ...draft, announcementCode: event.target.value })}/>
      </label>
      <label>
        <span>Alert type</span>
        <select
          value={draft.type}
          onChange={event => onDraft({ ...draft, type: event.target.value as AnnouncementDraft["type"] })}
        >
          <option>General</option>
          <option>Attendance</option>
          <option>Emergency</option>
          <option>Result</option>
        </select>
      </label>
      <label>
        <span>Title</span>
        <input value={draft.title} onChange={event => onDraft({ ...draft, title: event.target.value })}/>
      </label>
      <label>
        <span>Announcement detail</span>
        <textarea value={draft.message} onChange={event => onDraft({ ...draft, message: event.target.value })}/>
      </label>
      <div>
        {editing && <button className="button secondary" onClick={onCancel}>Cancel</button>}
        <button
          className="button primary"
          disabled={saving || !draft.announcementCode.trim() || !draft.title.trim() || !draft.message.trim()}
          onClick={onSave}
        >
          <Icon name={editing ? "edit" : "plus"} size={15}/>
          {saving ? "Saving..." : editing ? "Save alert" : "Announce to all"}
        </button>
      </div>
    </section>
    <section className="panel announce-table alert-register">
      <div className="announce-table-head">
        <span>AnnouncementCode</span>
        <span>Title and detail</span>
        <span>Type</span>
        <span>Create At</span>
        <span>Actions</span>
      </div>
      {rows.map(item => <article className="announce-table-row" key={item.id}>
        <strong className="management-code-value">{item.announcementCode}</strong>
        <div>
          <strong>{item.title}</strong>
          <span>{item.message}</span>
        </div>
        <span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span>
        <time>{new Date(item.createAt).toLocaleString()}</time>
        <div className="notification-row-actions">
          <button title="Edit alert" aria-label="Edit alert" onClick={() => onEdit(item)}>
            <Icon name="edit" size={14}/>
          </button>
          <button title="Remove alert" aria-label="Remove alert" onClick={() => onRemove(item.id)}>
            <Icon name="trash" size={14}/>
          </button>
        </div>
      </article>)}
      {!rows.length && <div className="empty-state">
        <strong>No active alerts</strong>
        <span>Create an institute announcement above.</span>
      </div>}
    </section>
  </>;
}
