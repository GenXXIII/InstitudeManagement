import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { NotificationDraft, NotificationItem } from "./notification-types";

const route = "/api/notification-center/notifications";

export const notificationsApi = {
  get: () => requestAllPages<NotificationItem>(route),
  getPage(options: PageOptions = {}, unreadOnly = false, prioritizeUnread = false) {
    const params = new URLSearchParams();
    if (unreadOnly) params.set("unreadOnly", "true");
    if (prioritizeUnread) params.set("prioritizeUnread", "true");
    return request<PagedResult<NotificationItem>>(`${route}?${appendPage(params, options)}`);
  },
  getItem: (id: string) => request<NotificationItem>(`${route}/${id}`),
  markRead: (id: string) => request<NotificationItem>(`${route}/${id}/read`, { method: "PUT" }),
  markAllRead: () => request<{ markedRead: number }>(`${route}/read-all`, { method: "PUT" }),
  update: (id: string, values: NotificationDraft) => request<NotificationItem>(`${route}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (id: string) => request<void>(`${route}/${id}`, { method: "DELETE" }),
};
