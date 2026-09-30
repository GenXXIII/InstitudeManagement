import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { NotificationHistoryItem } from "./notification-history-types";

const route = "/api/notification-center/history";

export const notificationHistoryApi = {
  get: (search = "") => requestAllPages<NotificationHistoryItem>(route, new URLSearchParams({ search })),
  getPage: (search = "", options: PageOptions = {}) => request<PagedResult<NotificationHistoryItem>>(`${route}?${appendPage(new URLSearchParams({ search }), options)}`),
  getItem: (id: string) => request<NotificationHistoryItem>(`${route}/${id}`),
};
