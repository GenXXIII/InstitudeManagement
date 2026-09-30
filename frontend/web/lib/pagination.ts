import { request } from "@/lib/http";

export const DATA_PAGE_SIZE = 40;
export const MAX_DATA_PAGE_SIZE = 100;

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type PageOptions = {
  page?: number;
  pageSize?: number;
};

export function appendPage(params: URLSearchParams, options: PageOptions = {}) {
  params.set("page", String(options.page ?? 1));
  params.set("pageSize", String(options.pageSize ?? DATA_PAGE_SIZE));
  return params;
}

export function emptyPage<T>(): PagedResult<T> {
  return { items: [], page: 1, pageSize: DATA_PAGE_SIZE, totalCount: 0, totalPages: 0 };
}

export async function requestAllPages<T>(path: string, params = new URLSearchParams(), signal?: AbortSignal) {
  const items: T[] = [];
  let page = 1;
  let totalPages = 1;
  do {
    const pageParams = new URLSearchParams(params);
    appendPage(pageParams, { page, pageSize: MAX_DATA_PAGE_SIZE });
    const result = await request<PagedResult<T>>(`${path}?${pageParams}`, { signal });
    items.push(...result.items);
    totalPages = result.totalPages;
    page += 1;
  } while (page <= totalPages);
  return items;
}
