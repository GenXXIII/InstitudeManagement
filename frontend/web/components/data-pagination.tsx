"use client";

import { useMemo, useState } from "react";

export const DATA_PAGE_SIZE = 40;

export function useDataPagination<T>(items: T[], resetKey: string, pageSize = DATA_PAGE_SIZE) {
  const [state, setState] = useState({ resetKey, page: 1 });
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const page = Math.min(state.resetKey === resetKey ? state.page : 1, pageCount);
  const setPage = (nextPage: number) => setState({ resetKey, page: Math.max(1, Math.min(nextPage, pageCount)) });
  const pageItems = useMemo(() => items.slice((page - 1) * pageSize, page * pageSize), [items, page, pageSize]);
  return { page, pageCount, pageItems, pageSize, setPage };
}

export function DataPagination({ page, pageCount, total, pageSize = DATA_PAGE_SIZE, onPage }: { page: number; pageCount: number; total: number; pageSize?: number; onPage: (page: number) => void }) {
  const start = total ? (page - 1) * pageSize + 1 : 0;
  const end = Math.min(page * pageSize, total);
  return <nav className="data-pagination" aria-label="Data pages">
    <span>{start}–{end} of {total.toLocaleString()}</span>
    <div><button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</button><strong>{page} / {pageCount}</strong><button type="button" disabled={page >= pageCount} onClick={() => onPage(page + 1)}>Next</button></div>
  </nav>;
}
