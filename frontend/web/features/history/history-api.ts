import { request } from "@/lib/http";
import type { RecordItem } from "./history-types";

export const historyApi = { get: (search = "", type = "all") => request<RecordItem[]>(`/api/records?search=${encodeURIComponent(search)}&type=${encodeURIComponent(type)}`) };
