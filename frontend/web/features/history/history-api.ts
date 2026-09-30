import { request } from "@/lib/http";
import { appendPage, requestAllPages, type PageOptions, type PagedResult } from "@/lib/pagination";
import type { RecordItem } from "./history-types";

const route = "/api/records";
const params = (search: string, type: string) => new URLSearchParams({ search, type });

export type HistoryPage = PagedResult<RecordItem> & { totalEvents: number };

export const historyApi = {
  get: (search = "", type = "all") => requestAllPages<RecordItem>(route, params(search, type)),
  getPage: (search = "", type = "all", options: PageOptions = {}) => request<HistoryPage>(`${route}?${appendPage(params(search, type), options)}`),
};
