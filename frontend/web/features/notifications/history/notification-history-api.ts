import { request } from "@/lib/http";
import type { NotificationHistoryItem } from "./notification-history-types";

const route = "/api/notification-center/history";

export const notificationHistoryApi = {
  get: () => request<NotificationHistoryItem[]>(route),
  getItem: (id: string) => request<NotificationHistoryItem>(`${route}/${id}`),
};
