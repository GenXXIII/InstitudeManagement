"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { DataTable } from "@/components/data-table";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { managementApis } from "@/features/management/management-apis";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import {
  itemSuggestion,
  matchesYear,
  searchQuerySeed,
  searchResources,
  suggestionSearchMatch,
  type SearchMatch,
  type SearchResourceDefinition,
  type SearchSuggestion,
} from "@/features/shell/topbar-search-model";
import {
  globalSearchWorkflows,
  isGlobalSearchWorkflow,
  workflowResultHref,
  workflowSuggestion,
  type GlobalSearchWorkflow,
} from "./global-search-workflows";

type SearchGroup = SearchResourceDefinition & { items: SearchSuggestion[] };
type SearchRow = {
  id: string;
  resource: SearchResourceDefinition;
  workflow: GlobalSearchWorkflow;
  suggestion: SearchSuggestion;
  match: SearchMatch;
};

export function GlobalSearchResultsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const query = searchParams.get("q")?.trim() ?? "";
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const workflowFilter = isGlobalSearchWorkflow(searchParams.get("workflow")) ? searchParams.get("workflow") as GlobalSearchWorkflow : "all";
  const resourceParam = searchParams.get("resource");
  const resourceFilter = searchResources.some(resource => resource.id === resourceParam) ? resourceParam as ManagementResource : "all";
  const [draft, setDraft] = useState(query);
  const [groups, setGroups] = useState<SearchGroup[]>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    if (!query) { setGroups([]); setError(false); return; }
    const seed = searchQuerySeed(query);
    const results = await Promise.allSettled(searchResources.map(async resource => ({
      ...resource,
      items: ((await managementApis[resource.id].get(seed, departmentId)) as ManagementItem[])
        .filter(item => matchesYear(item, year))
        .map(item => itemSuggestion(item, resource.id)),
    } satisfies SearchGroup)));
    const successful = results.flatMap(result => result.status === "fulfilled" ? [result.value] : []);
    setGroups(successful);
    setError(successful.length === 0 && results.some(result => result.status === "rejected"));
  }, [departmentId, query, year]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setDraft(query), 0); return () => window.clearTimeout(timer); }, [query]);

  const allRows = useMemo<SearchRow[]>(() => (groups ?? []).flatMap(group => group.items.flatMap(item => globalSearchWorkflows.flatMap(workflow => {
    const suggestion = workflowSuggestion(item, group.id, workflow.id);
    const match = suggestionSearchMatch(suggestion, query);
    return match ? [{ id: `${workflow.id}-${group.id}-${item.id}`, resource: group, workflow: workflow.id, suggestion, match }] : [];
  }))).toSorted((left, right) => left.match.rank - right.match.rank || left.suggestion.label.localeCompare(right.suggestion.label, undefined, { numeric: true }) || workflowOrder(left.workflow) - workflowOrder(right.workflow)), [groups, query]);

  const visibleRows = useMemo(() => allRows.filter(row =>
    (workflowFilter === "all" || row.workflow === workflowFilter) &&
    (resourceFilter === "all" || row.resource.id === resourceFilter)), [allRows, resourceFilter, workflowFilter]);
  const pagination = useDataPagination(visibleRows, `${query}-${departmentId}-${year}-${workflowFilter}-${resourceFilter}`);

  if (error) return <ErrorPage retry={load}/>;
  if (!groups) return <LoadingPage/>;

  const identityCount = new Set(allRows.map(row => `${row.resource.id}-${row.suggestion.id}`)).size;
  const workflowResultCount = allRows.filter(row => workflowFilter === "all" || row.workflow === workflowFilter).length;

  function submit(event: FormEvent) {
    event.preventDefault();
    const nextQuery = draft.trim();
    if (!nextQuery) return;
    router.replace(searchPageHref(nextQuery, departmentId, year, "all", "all"));
  }

  return <div className="viewport-data-page management-viewport-page global-filter-page">
    <PageHeading
      eyebrow="Current data search"
      title="Institute data search"
      description={query ? `Review matches for "${query}" across Management, Enrollment, current Record, and read-only History.` : "Find an identity by character, word, name, or code across every institute workflow."}
      actions={query ? <Link className="button primary" href="/search"><Icon name="search" size={16}/>New search</Link> : undefined}
    />

    <section className="panel global-filter-command">
      <form onSubmit={submit}><label className="management-search module-search-field global-filter-search"><Icon name="search" size={16}/><input value={draft} onChange={event => setDraft(event.target.value)} placeholder="Search by character, word, name, or code..." aria-label="Search institute data"/></label><button className="button primary" type="submit">Search</button></form>
      <div className="global-filter-command-context">
        <div className="management-scope global-filter-current-scope"><span>Current scope</span><strong>{departmentId ? "Selected department" : "All departments"} {year ? `- Year ${year}` : "- All years"}</strong><small>{identityCount} matching identities across {allRows.length} workflow destinations</small></div>
        <label className="department-switcher global-filter-resource-select"><span>Data module</span><select aria-label="Filter by data module" value={resourceFilter} onChange={event => router.replace(searchPageHref(query, departmentId, year, workflowFilter, event.target.value as ManagementResource | "all"))}><option value="all">All data ({workflowResultCount})</option>{searchResources.map(resource => <option value={resource.id} key={resource.id}>{resource.label} ({allRows.filter(row => row.resource.id === resource.id && (workflowFilter === "all" || row.workflow === workflowFilter)).length})</option>)}</select></label>
      </div>
    </section>

    <nav className="panel global-filter-workflow-tabs" aria-label="Filter search by workflow">
      <Link className={workflowFilter === "all" ? "active" : ""} href={searchPageHref(query, departmentId, year, "all", resourceFilter)}><span><Icon name="search" size={15}/></span><div><strong>All workflows</strong><small>Combined results</small></div><b>{allRows.length}</b></Link>
      {globalSearchWorkflows.map(workflow => <Link className={workflowFilter === workflow.id ? "active" : ""} href={searchPageHref(query, departmentId, year, workflow.id, resourceFilter)} key={workflow.id}><span><Icon name={workflow.icon} size={15}/></span><div><strong>{workflow.label}</strong><small>{workflow.detail}</small></div><b>{allRows.filter(row => row.workflow === workflow.id && (resourceFilter === "all" || row.resource.id === resourceFilter)).length}</b></Link>)}
    </nav>

    <section className="global-filter-results global-filter-results-direct">
      <div className="global-filter-paginated-region">
        <DataTable className="panel horizontal-management-table global-filter-register" headerClassName="horizontal-management-head global-filter-register-head" rowSelector=":scope > .global-filter-register-row" columns={["Workflow / module", "Code and name", "Why it matched", "Record detail", "Open"]}>
          {pagination.pageItems.map(row => <Link className="horizontal-management-row global-filter-register-row" href={workflowResultHref(row.workflow, row.resource.id, row.suggestion.code || row.suggestion.label, departmentId, year)} key={row.id}>
            <span className={`global-filter-workflow workflow-${row.workflow}`}><i><Icon name={globalSearchWorkflows.find(item => item.id === row.workflow)?.icon ?? "folder"} size={14}/></i><span><strong>{workflowLabel(row.workflow)}</strong><small>{row.resource.label}</small></span></span>
            <span className="global-filter-identity"><strong>{row.suggestion.code}</strong><small>{row.suggestion.label}</small></span>
            <span className="global-filter-match-reason"><b>{row.match.label}</b><small>{row.match.field} field</small></span>
            <span className="global-filter-detail">{row.suggestion.detail || `Matched "${query}"`}</span>
            <span className="global-filter-open">View <Icon name="arrow" size={12}/></span>
          </Link>)}
          {!visibleRows.length && <div className="global-search-empty"><span><Icon name="search" size={20}/></span><strong>{query ? `No results match "${query}" in this filter` : "Enter a search above"}</strong><small>Try a shorter character group, another word, or select All workflows and All data.</small></div>}
        </DataTable>
        {visibleRows.length > 0 && <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visibleRows.length} pageSize={pagination.pageSize} onPage={pagination.setPage}/>}
      </div>
    </section>
  </div>;
}

function searchPageHref(query: string, departmentId: string, year: string, workflow: GlobalSearchWorkflow | "all", resource: ManagementResource | "all") {
  const params = new URLSearchParams();
  if (query.trim()) params.set("q", query.trim());
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  if (workflow !== "all") params.set("workflow", workflow);
  if (resource !== "all") params.set("resource", resource);
  return `/search${params.size ? `?${params}` : ""}`;
}

function workflowLabel(workflow: GlobalSearchWorkflow) {
  return globalSearchWorkflows.find(item => item.id === workflow)?.label ?? workflow;
}

function workflowOrder(workflow: GlobalSearchWorkflow) {
  return globalSearchWorkflows.findIndex(item => item.id === workflow);
}
