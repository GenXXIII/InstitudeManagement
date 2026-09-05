import { Icon } from "@/components/icon";
import type { NotificationDraft, NotificationItem } from "./notification-types";

export function NotificationRegister({ rows, editing, draft, saving, onDraft, onOpen, onEdit, onSave, onCancel, onRemove }: {
  rows: NotificationItem[];
  editing?: string;
  draft?: NotificationDraft;
  saving: boolean;
  onDraft: (value: NotificationDraft) => void;
  onOpen: (id: string) => void;
  onEdit: (item: NotificationItem) => void;
  onSave: () => void;
  onCancel: () => void;
  onRemove: (id: string) => void;
}) {
  return <section className="panel horizontal-management-table notification-register">
    <div className="horizontal-management-head">
      <span>Notification code</span>
      <span>Type / severity</span>
      <span>Title and preview</span>
      <span>Received</span>
      <span>Status</span>
      <span>Actions</span>
    </div>
    {rows.map(item => editing === item.id && draft
      ? <article className="horizontal-management-row notification-edit-row" key={item.id}>
        <strong className="management-code-value">{item.notificationCode}</strong>
        <label className="notification-type-editor">
          <span>{item.type}</span>
          <select
            aria-label="Severity"
            value={draft.severity}
            onChange={event => onDraft({ ...draft, severity: event.target.value as NotificationDraft["severity"] })}
          >
            <option>Info</option>
            <option>Warning</option>
            <option>Critical</option>
          </select>
        </label>
        <div className="notification-copy-editor">
          <input
            aria-label="Notification title"
            value={draft.title}
            onChange={event => onDraft({ ...draft, title: event.target.value })}
          />
          <textarea
            aria-label="Notification detail"
            value={draft.message}
            onChange={event => onDraft({ ...draft, message: event.target.value })}
          />
        </div>
        <time>{new Date(item.createAt).toLocaleDateString()}</time>
        <label className="notification-read-editor">
          <input
            type="checkbox"
            checked={draft.isRead}
            onChange={event => onDraft({ ...draft, isRead: event.target.checked })}
          />
          <span>Read</span>
        </label>
        <div className="notification-edit-actions">
          <button onClick={onCancel}>Cancel</button>
          <button disabled={saving} onClick={onSave}>Save</button>
        </div>
      </article>
      : <article
        className={`horizontal-management-row notification-current-row ${item.isRead ? "" : "unread"}`}
        role="link"
        tabIndex={0}
        onClick={() => onOpen(item.id)}
        onKeyDown={event => {
          if (event.key === "Enter" || event.key === " ") onOpen(item.id);
        }}
        key={item.id}
      >
        <strong className="management-code-value">{item.notificationCode}</strong>
        <div className="notification-type-cell">
          <span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span>
          <small>{item.severity}</small>
        </div>
        <div className="notification-inbox-copy">
          <strong>{item.title}</strong>
          <span>{item.message}</span>
        </div>
        <time>{new Date(item.createAt).toLocaleDateString()}</time>
        <span className={`table-status ${item.isRead ? "" : "watch"}`}>{item.isRead ? "Read" : "Unread"}</span>
        <div className="notification-row-actions">
          <button
            title="Edit notification"
            aria-label="Edit notification"
            onClick={event => {
              event.stopPropagation();
              onEdit(item);
            }}
          >
            <Icon name="edit" size={14}/>
          </button>
          <button
            title="Remove notification"
            aria-label="Remove notification"
            onClick={event => {
              event.stopPropagation();
              onRemove(item.id);
            }}
          >
            <Icon name="trash" size={14}/>
          </button>
        </div>
      </article>)}
    {!rows.length && <div className="empty-state">
      <strong>No notifications</strong>
      <span>Published alerts and system notifications will appear here.</span>
    </div>}
  </section>;
}
