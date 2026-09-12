import { DataTable } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { alertCodeExample, formatAlertCode } from "@/lib/workflow-code";
import type { AnnouncementDraft, AnnouncementItem } from "./announcement-types";

export function AlertRegister({ rows, draft, saving, onDraft, onOpen, onSave }: {
  rows: AnnouncementItem[];
  draft: AnnouncementDraft;
  saving: boolean;
  onDraft: (value: AnnouncementDraft) => void;
  onOpen: (id: string) => void;
  onSave: () => void;
}) {
  return <>
    <section className="panel announce-alert-form">
      <label className="announce-alert-code-field">
        <span>Alert code</span>
        <input
          className="management-code-value"
          value={draft.announcementCode}
          required
          placeholder="Enter sequence, for example 1"
          aria-label="Alert code sequence"
          onChange={event => onDraft({ ...draft, announcementCode: event.target.value })}
          onBlur={() => { if (draft.announcementCode.trim()) onDraft({ ...draft, announcementCode: formatAlertCode(draft.announcementCode) }); }}
        />
        <small>{`Final code: ${draft.announcementCode.trim() ? formatAlertCode(draft.announcementCode) : alertCodeExample()}`}</small>
      </label>
      <label>
        <span>Alert type</span>
        <select
          value={draft.type}
          onChange={event => onDraft({ ...draft, type: event.target.value as AnnouncementDraft["type"] })}
        >
          <option value="" disabled hidden>Select alert type</option>
          <option>General</option>
          <option>Attendance</option>
          <option>Emergency</option>
          <option>Result</option>
        </select>
        <small>Choose the alert category</small>
      </label>
      <label>
        <span>Title</span>
        <input placeholder="Enter alert title" value={draft.title} onChange={event => onDraft({ ...draft, title: event.target.value })}/>
        <small>Short heading shown to users</small>
      </label>
      <label>
        <span>Announcement detail</span>
        <textarea placeholder="Enter notification message" value={draft.message} onChange={event => onDraft({ ...draft, message: event.target.value })}/>
        <small>Message shown in Notification</small>
      </label>
      <div>
        <button
          className="button primary"
          disabled={saving || !draft.announcementCode.trim() || !draft.type || !draft.title.trim() || !draft.message.trim()}
          onClick={onSave}
        >
          <Icon name="plus" size={15}/>
          {saving ? "Saving..." : "Announce to all"}
        </button>
      </div>
    </section>
    <DataTable as="section" className="panel horizontal-management-table alert-register" headerClassName="horizontal-management-head" rowSelector=":scope > .alert-register-row" columns={["AnnouncementCode", "Detail", "Type", "Create At"]}>
      {rows.map(item => <article
        className={`horizontal-management-row alert-register-row ${item.isRead ? "" : "unread"}`}
        role="link"
        tabIndex={0}
        onClick={() => onOpen(item.id)}
        onKeyDown={event => {
          if (event.currentTarget === event.target && (event.key === "Enter" || event.key === " ")) onOpen(item.id);
        }}
        key={item.id}
      >
        <strong className="management-code-value">{item.announcementCode}</strong>
        <div><span>{item.message}</span></div>
        <span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span>
        <time>{new Date(item.createAt).toLocaleString()}</time>
      </article>)}
      {!rows.length && <div className="empty-state">
        <strong>No active alerts</strong>
        <span>Create an institute announcement above.</span>
      </div>}
    </DataTable>
  </>;
}
