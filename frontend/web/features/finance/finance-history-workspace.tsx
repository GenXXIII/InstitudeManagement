"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import { compareFinanceAccounts, FinanceTable } from "./finance-table";
import type { FinancialAccount } from "./finance-types";

export function FinanceHistoryWorkspace() {
  const [accounts, setAccounts] = useState<FinancialAccount[]>();
  const [query, setQuery] = useState("");
  const [error, setError] = useState(false);
  const load = useCallback(() => financeApi.get(query, "Paid").then(rows => { setAccounts(rows); setError(false); }).catch(() => setError(true)), [query]);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 180); return () => window.clearTimeout(timer); }, [load]);
  const groups = useMemo(() => {
    const values = new Map<string, FinancialAccount[]>();
    for (const account of accounts ?? []) {
      const key = `${account.academicYear}|${account.semester}`;
      values.set(key, [...(values.get(key) ?? []), account]);
    }
    return [...values.entries()].toSorted(([left], [right]) => left.localeCompare(right, undefined, { numeric: true })).map(([key, rows]) => ({ key, academicYear: rows[0].academicYear, semester: rows[0].semester, rows: rows.toSorted(compareFinanceAccounts) }));
  }, [accounts]);
  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!accounts) return <LoadingPage/>;
  return <div className="viewport-data-page history-viewport-page finance-history-page">
    <PageHeading eyebrow="Paid semester archive" title="Finance history" description="Every fully paid semester moves out of active Finance and remains here as a read-only financial record."/>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search paid account or student…" searchAriaLabel="Search Finance history" resultLabel={`${accounts.length} paid semesters`} className="record-toolbar panel" searchClassName="record-search management-search module-search-field"/>
    {groups.length ? <div className="finance-history-groups">{groups.map(group => <section className="semester-history-group" key={group.key}><header><div><span>Paid semester</span><h2>{group.academicYear}</h2></div><strong>{group.semester}</strong><small>{group.rows.length} paid account{group.rows.length === 1 ? "" : "s"}</small></header><PaginatedDataRegion items={group.rows} resetKey={group.key} as="fragment">{pageItems => <FinanceTable accounts={pageItems} history/>}</PaginatedDataRegion></section>)}</div> : <DataTableEmptyState icon={<Icon name="finance" size={28}/>} title="No paid semester history" description="A semester appears here immediately after its account becomes Paid."/>}
  </div>;
}
