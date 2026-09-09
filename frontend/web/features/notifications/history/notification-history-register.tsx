import { DataTable } from "@/components/data-table";
import type { NotificationHistoryItem } from "./notification-history-types";

export function NotificationHistoryRegister({ rows, onOpen }: { rows: NotificationHistoryItem[]; onOpen: (code: string) => void }) {
  return <DataTable as="section" className="panel horizontal-management-table notification-history-register" headerClassName="horizontal-management-head" rowSelector=":scope > .notification-history-row" columns={[{ key: "history-code", label: "Notification history code", align: "center" }, { key: "source-code", label: "Source code", align: "center" }, "Source", "Type", "Detail", "Recorded", "Action"]}>
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
      <span className="table-status">{item.kind}</span>
      <span className={`table-status alert-${item.type.toLowerCase()}`}>{item.type}</span>
      <div className="notification-inbox-copy">
        <span>{item.message}</span>
      </div>
      <time>{new Date(item.createAt).toLocaleDateString()}</time>
      <span className="table-status">{item.action}</span>
    </article>)}
    {!rows.length && <div className="empty-state">
      <strong>No notification history</strong>
      <span>Notification and alert activity will appear here in recorded date order.</span>
    </div>}
  </DataTable>;
}
