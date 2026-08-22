import { request } from "@/lib/http";
import type { AnnouncementDraft, AnnouncementItem, NotificationDraft, NotificationHistoryItem, NotificationItem } from "./notification-types";

export const notificationApi = {
  notifications: () => request<NotificationItem[]>("/api/notification-center/notifications"),
  notification: (id: string) => request<NotificationItem>(`/api/notification-center/notifications/${id}`),
  readNotification: (id: string) => request<NotificationItem>(`/api/notification-center/notifications/${id}/read`, { method: "PUT" }),
  updateNotification: (id: string, values: NotificationDraft) => request<NotificationItem>(`/api/notification-center/notifications/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  removeNotification: (id: string) => request<void>(`/api/notification-center/notifications/${id}`, { method: "DELETE" }),
  alerts: () => request<AnnouncementItem[]>("/api/notification-center/alerts"),
  createAlert: (values: AnnouncementDraft) => request<AnnouncementItem>("/api/notification-center/alerts", { method: "POST", body: JSON.stringify(values) }),
  updateAlert: (id: string, values: AnnouncementDraft) => request<AnnouncementItem>(`/api/notification-center/alerts/${id}`, { method: "PUT", body: JSON.stringify(values) }),
  removeAlert: (id: string) => request<void>(`/api/notification-center/alerts/${id}`, { method: "DELETE" }),
  history: () => request<NotificationHistoryItem[]>("/api/notification-center/history"),
  historyItem: (id: string) => request<NotificationHistoryItem>(`/api/notification-center/history/${id}`),
};
