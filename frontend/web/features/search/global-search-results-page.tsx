"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { managementApis } from "@/features/management/management-apis";
import type { ManagementItem } from "@/features/management/management-types";
import {
  itemSuggestion,
  managementSearchHref,
  matchesYear,
  searchResources,
  type SearchResourceDefinition,
  type SearchSuggestion,
} from "@/features/shell/topbar-search-model";
import { workflowSourceSearch } from "@/lib/workflow-code";

type SearchGroup = SearchResourceDefinition & { items: SearchSuggestion[] };
type SearchRow = SearchSuggestion & { resource: SearchResourceDefinition };

export function GlobalSearchResultsPage() {
  const searchParams = useSearchParams();
  const query = searchParams.get("q")?.trim() ?? "";
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [groups, setGroups] = useState<SearchGroup[]>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    if (!query) { setGroups([]); setError(false); return; }
    try {
      const sourceQuery = workflowSourceSearch(query);
      const results = await Promise.all(searchResources.map(async resource => {
        const items = await managementApis[resource.id].get(sourceQuery, departmentId);
        return {
          ...resource,
          items: (items as ManagementItem[])
            .filter(item => matchesYear(item, year))
            .map(item => itemSuggestion(item, resource.id))
            .toSorted((left, right) => left.label.localeCompare(right.label, undefined, { numeric: true })),
        } satisfies SearchGroup;
      }));
      setGroups(results);
      setError(false);
    } catch { setError(true); }
  }, [departmentId, query, year]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  const rows = useMemo<SearchRow[]>(() => (groups ?? []).flatMap(group => group.items.map(item => ({ ...item, resource: group }))), [groups]);
  const pagination = useDataPagination(rows, `${query}-${departmentId}-${year}`);

  if (error) return <ErrorPage retry={load}/>;
  if (!groups) return <LoadingPage/>;

  const matchedGroups = groups.filter(group => group.items.length > 0);
  const scope = `${departmentId ? "Selected department" : "All departments"} · ${year ? `Year ${year}` : "All years"}`;
  return <div className="viewport-data-page global-filter-page">
    <PageHeading eyebrow="Current data management" title={query ? `Global Search · “${query}”` : "Global Search"} description="Review matching institute records and open the correct Management module." actions={<Link className="button secondary" href="/"><Icon name="dashboard" size={15}/>Dashboard</Link>}/>
    <section className="panel global-filter-summary">
      <div><span>Search filter</span><strong>{query || "No search entered"}</strong><small>{scope}</small></div>
      <div><span>Matching data</span><strong>{rows.length.toLocaleString()}</strong><small>Across {matchedGroups.length} modules</small></div>
      <p>Choose a module to keep the same <b>{query || "search"}</b> filter, or choose a row to open that module filtered to the selected name.</p>
    </section>
    {query && <section className="global-filter-module-strip" aria-label="Filtered modules">
      {groups.map(group => <Link className={`panel ${group.items.length ? "has-results" : ""}`} href={managementSearchHref(group.id, query, departmentId, year)} key={group.id}>
        <span><Icon name={group.icon as Parameters<typeof Icon>[0]["name"]} size={17}/></span>
        <div><strong>{group.label}</strong><small>{group.items.length} {group.items.length === 1 ? "match" : "matches"}</small></div>
        <Icon name="arrow" size={13}/>
      </Link>)}
    </section>}
    <section className="global-filter-paginated-region">
      <div className="panel global-filter-register">
        <div className="global-filter-register-head horizontal-management-head"><span>Module</span><span>Code</span><span>Matching data and summary</span><span>Open</span></div>
        {pagination.pageItems.map(row => <Link className="global-filter-register-row" href={managementSearchHref(row.resource.id, row.label, departmentId, year)} key={`${row.resource.id}-${row.id}`}>
          <span className="global-filter-resource"><i><Icon name={row.resource.icon as Parameters<typeof Icon>[0]["name"]} size={14}/></i><strong>{row.resource.label}</strong></span>
          <strong className="management-code-value">{row.code}</strong>
          <span className="global-filter-match"><strong>{row.label}</strong><small>{row.detail || `Matched “${query}”`}</small></span>
          <span className="global-filter-open">View <Icon name="arrow" size={12}/></span>
        </Link>)}
        {!rows.length && <div className="global-search-empty"><span><Icon name="search" size={20}/></span><strong>{query ? `No data matches “${query}”` : "Enter a search in the top bar"}</strong><small>{query ? "Try another name, code, course, classroom, department, or character." : "Your complete filtered result page will appear here."}</small></div>}
      </div>
      {rows.length > 0 && <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={rows.length} pageSize={pagination.pageSize} onPage={pagination.setPage}/>} 
    </section>
  </div>;
}
