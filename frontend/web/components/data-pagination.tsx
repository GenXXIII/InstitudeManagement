"use client";

import { type ReactNode, useMemo, useState } from "react";
import type { PagedResult } from "@/lib/pagination";
import { DATA_PAGE_SIZE } from "@/lib/pagination";

type PaginatedDataRegionProps<T> = {
  items: T[];
  resetKey: string;
  children: (pageItems: T[]) => ReactNode;
  empty?: ReactNode;
  className?: string;
  pageSize?: number;
  as?: "section" | "div" | "fragment";
};

export { DATA_PAGE_SIZE } from "@/lib/pagination";

export function useDataPagination<T>(items: T[], resetKey: string, pageSize = DATA_PAGE_SIZE) {
  const [state, setState] = useState({ resetKey, page: 1 });
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const page = Math.min(state.resetKey === resetKey ? state.page : 1, pageCount);
  const setPage = (nextPage: number) => setState({ resetKey, page: Math.max(1, Math.min(nextPage, pageCount)) });
  const pageItems = useMemo(() => items.slice((page - 1) * pageSize, page * pageSize), [items, page, pageSize]);
  return { page, pageCount, pageItems, pageSize, setPage };
}

export function PaginatedDataRegion<T>({ items, resetKey, children, empty, className = "", pageSize = DATA_PAGE_SIZE, as = "section" }: PaginatedDataRegionProps<T>) {
  const pagination = useDataPagination(items, resetKey, pageSize);
  if (!items.length && empty) return <>{empty}</>;
  const content = <>
    {children(pagination.pageItems)}
    {items.length > 0 && <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={items.length} pageSize={pagination.pageSize} onPage={pagination.setPage}/>}
  </>;
  if (as === "fragment") return content;
  const Region = as;
  return <Region className={`shared-data-region ${className}`.trim()}>{content}</Region>;
}

export function useServerPage(resetKey: string) {
  const [state, setState] = useState({ resetKey, page: 1 });
  const page = state.resetKey === resetKey ? state.page : 1;
  const setPage = (nextPage: number) => setState({ resetKey, page: Math.max(1, nextPage) });
  return [page, setPage] as const;
}

export function ServerPaginatedDataRegion<T>({ result, children, empty, className = "", as = "section", onPage }: {
  result: PagedResult<T>;
  children: (pageItems: T[]) => ReactNode;
  empty?: ReactNode;
  className?: string;
  as?: "section" | "div" | "fragment";
  onPage: (page: number) => void;
}) {
  const content = <>
    {!result.items.length && empty ? empty : children(result.items)}
    {result.totalCount > 0 && <DataPagination page={result.page} pageCount={Math.max(1, result.totalPages)} total={result.totalCount} pageSize={result.pageSize} onPage={onPage}/>}
  </>;
  if (as === "fragment") return content;
  const Region = as;
  return <Region className={`shared-data-region ${className}`.trim()}>{content}</Region>;
}

export function DataPagination({ page, pageCount, total, pageSize = DATA_PAGE_SIZE, onPage }: { page: number; pageCount: number; total: number; pageSize?: number; onPage: (page: number) => void }) {
  const start = total ? (page - 1) * pageSize + 1 : 0;
  const end = Math.min(page * pageSize, total);
  return <nav className="data-pagination" aria-label="Data pages">
    <span>{start}–{end} of {total.toLocaleString()}</span>
    <div><button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)}>Previous</button><strong>{page} / {pageCount}</strong><button type="button" disabled={page >= pageCount} onClick={() => onPage(page + 1)}>Next</button></div>
  </nav>;
}
