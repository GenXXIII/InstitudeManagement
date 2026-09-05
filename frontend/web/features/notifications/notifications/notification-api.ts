import { request } from "@/lib/http";
import type { NotificationDraft, NotificationItem } from "./notification-types";

const route = "/api/notification-center/notifications";

export const notificationsApi = {
  get: () => request<NotificationItem[]>(route),
  getItem: (id: string) => request<NotificationItem>(`${route}/${id}`),
  markRead: (id: string) => request<NotificationItem>(`${route}/${id}/read`, { method: "PUT" }),
  markAllRead: () => request<{ markedRead: number }>(`${route}/read-all`, { method: "PUT" }),
  update: (id: string, values: NotificationDraft) => request<NotificationItem>(`${route}/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  remove: (id: string) => request<void>(`${route}/${id}`, { method: "DELETE" }),
};
