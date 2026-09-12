import { DataTable } from "@/components/data-table";
import type { NotificationItem } from "./notification-types";

export function NotificationRegister({ rows, onOpen }: {
  rows: NotificationItem[];
  onOpen: (id: string) => void;
}) {
  return <DataTable as="section" className="panel horizontal-management-table notification-register" headerClassName="horizontal-management-head" rowSelector=":scope > .notification-current-row" columns={["Notification code", "Type", "Detail", "Received"]}>
    {rows.map(item => <article
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
        <span className="table-status">{item.type}</span>
      </div>
      <div className="notification-inbox-copy">
        <span>{item.message}</span>
      </div>
      <time>{new Date(item.createAt).toLocaleDateString()}</time>
    </article>)}
    {!rows.length && <div className="empty-state">
      <strong>No notifications</strong>
      <span>Published alerts and system notifications will appear here.</span>
    </div>}
  </DataTable>;
}
