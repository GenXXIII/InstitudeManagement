"use client";

import {
  type CSSProperties,
  type ElementType,
  type ReactNode,
  useLayoutEffect,
  useMemo,
  useRef,
} from "react";

export type DataTableAlignment = "left" | "center" | "right";

export type DataTableColumn = {
  key: string;
  label: ReactNode;
  align?: DataTableAlignment;
  minimumWidth?: number;
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

const minimumColumnWidth = 44;
const minimumFontSize = 9;
const textSelector = "strong, small, time, b, span, button, a, p";
let measureContext: CanvasRenderingContext2D | null | undefined;

export function DataTable({
  as = "div",
  columns,
  children,
  className = "",
  headerClassName = "",
  rowSelector,
  ariaLabel,
}: DataTableProps) {
  const rootRef = useRef<HTMLElement>(null);
  const normalizedColumns = useMemo(
    () => columns.map((column, index) => typeof column === "string"
      ? { key: `${index}-${column}`, label: column }
      : column),
    [columns],
  );
  const columnSignature = normalizedColumns
    .map(column => `${column.key}:${column.align ?? "auto"}:${column.minimumWidth ?? "auto"}:${String(column.label)}`)
    .join("|");

  useLayoutEffect(() => {
    const root = rootRef.current;
    if (!root) return;

    let frame = 0;
    const resizeObserver = new ResizeObserver(schedule);
    const mutationObserver = new MutationObserver(schedule);

    function prepare() {
      frame = 0;
      const header = root?.querySelector<HTMLElement>(":scope > .data-table-head");
      if (!root || !header) return;

      const headerCells = elementChildren(header);
      const rows = [...root.querySelectorAll<HTMLElement>(rowSelector)]
        .filter(row => elementChildren(row).length === headerCells.length);

      for (const row of rows) row.dataset.dataTableRow = "";
      alignColumns(normalizedColumns, headerCells, rows);
      sizeColumns(root, normalizedColumns, headerCells, rows);
    }

    function schedule() {
      if (!frame) frame = window.requestAnimationFrame(prepare);
    }

    resizeObserver.observe(root);
    mutationObserver.observe(root, { childList: true, characterData: true, subtree: true });
    schedule();

    return () => {
      if (frame) window.cancelAnimationFrame(frame);
      resizeObserver.disconnect();
      mutationObserver.disconnect();
    };
  }, [columnSignature, normalizedColumns, rowSelector]);

  const Tag = as as ElementType;
  return <Tag
    ref={rootRef}
    className={`shared-data-table ${className}`.trim()}
    aria-label={ariaLabel}
    style={{ "--data-table-column-count": normalizedColumns.length } as CSSProperties}
  >
    <div className={`data-table-head ${headerClassName}`.trim()}>
      {normalizedColumns.map(column => <span
        data-data-table-cell=""
        data-cell-align={alignmentFor(column)}
        data-cell-flow="row"
        key={column.key}
      >{column.label}</span>)}
    </div>
    {children}
  </Tag>;
}

function alignColumns(columns: DataTableColumn[], headerCells: HTMLElement[], rows: HTMLElement[]) {
  headerCells.forEach((header, index) => {
    const alignment = alignmentFor(columns[index]);
    applyAlignment(header, alignment);
    for (const row of rows) {
      const cell = elementChildren(row)[index];
      if (cell) applyAlignment(cell, alignment);
    }
  });
}

function sizeColumns(root: HTMLElement, columns: DataTableColumn[], headerCells: HTMLElement[], rows: HTMLElement[]) {
  const desired = headerCells.map((header, index) => Math.max(
    columns[index]?.minimumWidth ?? minimumColumnWidth,
    naturalCellWidth(header, true),
    ...rows.map(row => naturalCellWidth(elementChildren(row)[index])),
  ));
  const available = Math.max(headerCells.length, contentWidth(root));
  const widths = distributeWidths(desired, available);
  const template = widths.map(width => `${Math.floor(width * 10) / 10}px`).join(" ");
  root.style.setProperty("--data-table-columns", template);

  headerCells.forEach(cell => fitCellText(cell, true));
  for (const row of rows) elementChildren(row).forEach(cell => fitCellText(cell));
}

function alignmentFor(column?: DataTableColumn): DataTableAlignment {
  if (column?.align) return column.align;
  const label = String(column?.label ?? "")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[^a-zA-Z0-9]+/g, " ")
    .trim()
    .toLowerCase();
  const compact = label.replaceAll(" ", "");

  if (/(?:code|codes|id|ids|identifier|identifiers)$/.test(compact)) return "center";
  if (/\b(code|codes|id|ids|identifier|identity|status|state|type|types|category|categories|boolean|year|semester|term|shift|date|day|time|received|recorded|action|actions|open|checkbox|checkboxes|icon|icons|photo|image|device|create|created|updated)\b/.test(label)) return "center";
  if (/\b(number|numbers|quantity|quantities|count|counts|amount|amounts|money|score|scores|percentage|percentages|percent|decimal|decimals|measurement|measurements|capacity|total|average|rate|coverage|attendance|grade|grades|students|teachers|rooms|seats)\b/.test(label)) return "right";
  return "left";
}

function applyAlignment(cell: HTMLElement, alignment: DataTableAlignment) {
  cell.dataset.dataTableCell = "";
  cell.dataset.cellAlign = alignment;
  const style = window.getComputedStyle(cell);
  if (style.display.includes("flex")) cell.dataset.cellFlow = style.flexDirection.startsWith("column") ? "column" : "row";
  else if (style.display.includes("grid")) cell.dataset.cellFlow = "grid";
  else delete cell.dataset.cellFlow;
}

function elementChildren(element?: Element) {
  if (!element) return [];
  return [...element.children].filter((child): child is HTMLElement => child instanceof HTMLElement);
}

function naturalCellWidth(cell?: HTMLElement, header = false) {
  if (!cell) return minimumColumnWidth;
  const style = window.getComputedStyle(cell);
  const padding = numeric(style.paddingLeft) + numeric(style.paddingRight);
  const text = Math.max(...textElements(cell, header).map(textWidth), 0);
  const actions = cell.querySelector<HTMLElement>(".management-actions, .notification-row-actions");
  const actionWidth = actions
    ? [...actions.querySelectorAll<HTMLElement>("button, a")].reduce((total, action) => total + action.getBoundingClientRect().width, 0)
      + Math.max(0, actions.children.length - 1) * numeric(window.getComputedStyle(actions).columnGap)
    : 0;
  const fixedVisual = Math.max(...[...cell.querySelectorAll<HTMLElement>("img, .initial-chip, .horizontal-portrait")]
    .map(item => item.getBoundingClientRect().width), 0);
  return Math.ceil(Math.max(text, actionWidth, fixedVisual, minimumColumnWidth - padding) + padding + 2);
}

function distributeWidths(desired: number[], available: number) {
  const equal = available / desired.length;
  const widths = desired.map(() => equal);
  const floor = Math.min(minimumColumnWidth, equal);
  const needs = desired.map((width, index) => ({ index, amount: Math.max(0, width - equal) })).filter(item => item.amount > 0);
  const donors = desired.map((width, index) => ({ index, amount: Math.max(0, equal - Math.max(floor, width)) })).filter(item => item.amount > 0);
  const totalNeed = needs.reduce((total, item) => total + item.amount, 0);
  const totalAvailable = donors.reduce((total, item) => total + item.amount, 0);
  const transfer = Math.min(totalNeed, totalAvailable);

  if (transfer) {
    for (const item of needs) widths[item.index] += transfer * item.amount / totalNeed;
    for (const item of donors) widths[item.index] -= transfer * item.amount / totalAvailable;
  }
  return widths;
}

function fitCellText(cell: HTMLElement, header = false) {
  const availableCellWidth = contentWidth(cell);
  if (availableCellWidth <= 0) return;
  const targets = textElements(cell, header);
  for (const target of targets) target.style.removeProperty("font-size");

  for (const target of targets) {
    const style = window.getComputedStyle(target);
    const normal = numeric(style.fontSize);
    const required = textWidth(target);
    const targetPadding = numeric(style.paddingLeft) + numeric(style.paddingRight);
    const rendered = target.getBoundingClientRect().width - targetPadding;
    const available = Math.min(availableCellWidth, rendered > 1 ? rendered : availableCellWidth);
    if (!normal || !available || required <= available) continue;
    const fitted = Math.max(minimumFontSize, Math.floor((normal * available / required) * 10) / 10);
    target.style.setProperty("font-size", `${fitted}px`, "important");
  }
}

function textElements(cell: HTMLElement, header = false) {
  if (header) return [cell];
  const targets = [...cell.querySelectorAll<HTMLElement>(textSelector)].filter(target =>
    target.innerText.trim()
    && !target.classList.contains("management-cell-label")
    && !target.closest("svg"),
  );
  if (cell.matches(textSelector) && cell.innerText.trim()) targets.unshift(cell);
  return [...new Set(targets)];
}

function textWidth(element: HTMLElement) {
  const text = element.innerText.trim();
  if (!text) return 0;
  measureContext ??= document.createElement("canvas").getContext("2d");
  if (!measureContext) return 0;
  const style = window.getComputedStyle(element);
  measureContext.font = `${style.fontStyle} ${style.fontVariant} ${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
  const letterSpacing = numeric(style.letterSpacing);
  const lines = text.split(/\n+/).map(line => line.trim()).filter(Boolean);
  return Math.max(...lines.map(line => {
    const displayed = style.textTransform === "uppercase" ? line.toUpperCase() : line;
    return measureContext!.measureText(displayed).width + Math.max(0, displayed.length - 1) * letterSpacing;
  }), 0);
}

function contentWidth(element: HTMLElement) {
  const style = window.getComputedStyle(element);
  return element.clientWidth - numeric(style.paddingLeft) - numeric(style.paddingRight);
}

function numeric(value: string) {
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : 0;
}
