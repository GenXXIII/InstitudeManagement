"use client";

import {
  type CSSProperties,
  type ElementType,
  type ReactNode,
  useLayoutEffect,
  useMemo,
  useRef,
} from "react";
import { Icon } from "@/components/icon";
import {
  getColumnAlignment,
  observeDataTableLayout,
  type DataTableAlignment,
  type DataTableLayoutColumn,
} from "./data-table-layout";

export type { DataTableAlignment } from "./data-table-layout";
export { DATA_PAGE_SIZE, DataPagination, PaginatedDataRegion, useDataPagination } from "./data-pagination";

export type DataTableColumn = DataTableLayoutColumn & {
  key: string;
  label: ReactNode;
  align?: DataTableAlignment;
};

type DataTableProps = {
  as?: "div" | "section";
  columns: ReadonlyArray<string | DataTableColumn>;
  children: ReactNode;
  className?: string;
  headerClassName?: string;
  rowSelector: string;
  ariaLabel?: string;
};

type DataTableToolbarProps = {
  query?: string;
  onQueryChange?: (value: string) => void;
  searchPlaceholder?: string;
  searchAriaLabel?: string;
  contextLabel?: string;
  contextValue?: ReactNode;
  resultLabel?: ReactNode;
  children?: ReactNode;
  className?: string;
  searchClassName?: string;
  resultClassName?: string;
};

export function DataTable({ as = "div", columns, children, className = "", headerClassName = "", rowSelector, ariaLabel }: DataTableProps) {
  const rootRef = useRef<HTMLElement>(null);
  const normalizedColumns = useMemo(
    () => columns.map((column, index) => typeof column === "string" ? { key: `${index}-${column}`, label: column } : column),
    [columns],
  );

  useLayoutEffect(() => {
    const root = rootRef.current;
    return root ? observeDataTableLayout(root, normalizedColumns, rowSelector) : undefined;
  }, [normalizedColumns, rowSelector]);

  const Tag = as as ElementType;
  return <Tag
    ref={rootRef}
    className={`shared-data-table ${className}`.trim()}
    aria-label={ariaLabel}
    data-adaptive-table=""
    style={{ "--data-table-column-count": normalizedColumns.length } as CSSProperties}
  >
    <div className={`data-table-head ${headerClassName}`.trim()} data-adaptive-table-head="">
      {normalizedColumns.map(column => <span
        data-data-table-cell=""
        data-cell-align={getColumnAlignment(column)}
        data-cell-flow="row"
        key={column.key}
      >{column.label}</span>)}
    </div>
    {children}
  </Tag>;
}

export function DataTableToolbar({
  query,
  onQueryChange,
  searchPlaceholder,
  searchAriaLabel = searchPlaceholder,
  contextLabel,
  contextValue,
  resultLabel,
  children,
  className = "management-toolbar panel management-toolbar-global",
  searchClassName = "management-search module-search-field",
  resultClassName = "record-count shared-data-count",
}: DataTableToolbarProps) {
  return <section className={`shared-data-toolbar ${className}`.trim()}>
    {query !== undefined && onQueryChange && searchPlaceholder && <label className={searchClassName}>
      <Icon name="search" size={16}/>
      <input value={query} onChange={event => onQueryChange(event.target.value)} placeholder={searchPlaceholder} aria-label={searchAriaLabel}/>
    </label>}
    {children}
    {contextLabel && <div className="management-scope shared-data-context"><span>{contextLabel}</span><strong>{contextValue}</strong></div>}
    {resultLabel && <div className={resultClassName}>{resultLabel}</div>}
  </section>;
}

export function DataTableEmptyState({ icon, title, description, className = "" }: { icon?: ReactNode; title: ReactNode; description: ReactNode; className?: string }) {
  return <section className={`panel empty-state shared-data-empty ${className}`.trim()}>
    {icon && <div className="empty-icon">{icon}</div>}
    <strong>{title}</strong>
    <span>{description}</span>
  </section>;
}
