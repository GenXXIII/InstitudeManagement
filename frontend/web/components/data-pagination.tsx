"use client";

import { useMemo, useState } from "react";

export const DATA_PAGE_SIZE = 40;

export function useDataPagination<T>(items: T[], resetKey: string) {
  const [state, setState] = useState({ resetKey, page: 1 });
  const pageCount = Math.max(1, Math.ceil(items.length / DATA_PAGE_SIZE));
  const page = Math.min(state.resetKey === resetKey ? state.page : 1, pageCount);
  const setPage = (nextPage: number) => setState({ resetKey, page: Math.max(1, Math.min(nextPage, pageCount)) });
  const pageItems = useMemo(() => items.slice((page - 1) * DATA_PAGE_SIZE, page * DATA_PAGE_SIZE), [items, page]);
  return { page, pageCount, pageItems, setPage };
}

export function DataPagination({ page, pageCount, total, onPage }: { page: number; pageCount: number; total: number; onPage: (page: number) => void }) {
  const start = total ? (page - 1) * DATA_PAGE_SIZE + 1 : 0;
  const end = Math.min(page * DATA_PAGE_SIZE, total);
  return <nav className="data-pagination" aria-label="Data pages">
    <span>{start}–{end} of {total.toLocaleString()}</span>
    <div><button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</button><strong>{page} / {pageCount}</strong><button type="button" disabled={page >= pageCount} onClick={() => onPage(page + 1)}>Next</button></div>
  </nav>;
}
