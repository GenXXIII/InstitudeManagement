import type { NotificationHistoryItem } from "./notification-history-types";

export function NotificationHistoryRegister({ rows, onOpen }: { rows: NotificationHistoryItem[]; onOpen: (code: string) => void }) {
  return <section className="panel horizontal-management-table notification-history-register">
    <div className="horizontal-management-head">
      <span>Notification history code</span>
      <span>Notification code</span>
      <span>Type</span>
      <span>Title and preview</span>
      <span>Recorded</span>
      <span>Action</span>
    </div>
    {rows.map(item => <article
      className="horizontal-management-row notification-history-row"
      role="link"
      tabIndex={0}
      onClick={() => onOpen(item.notificationHistoryCode)}
      onKeyDown={event => {
        if (event.key === "Enter" || event.key === " ") onOpen(item.notificationHistoryCode);
      }}
      key={item.id}
    >
      <strong className="management-code-value">{item.notificationHistoryCode}</strong>
      <strong className="management-code-value">{item.sourceCode}</strong>
      <span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span>
      <div className="notification-inbox-copy">
        <strong>{item.title}</strong>
        <span>{item.message}</span>
      </div>
      <time>{new Date(item.createAt).toLocaleDateString()}</time>
      <span className="table-status">{item.action}</span>
    </article>)}
    {!rows.length && <div className="empty-state">
      <strong>No read history</strong>
      <span>Notifications move here after they are opened.</span>
    </div>}
  </section>;
}
