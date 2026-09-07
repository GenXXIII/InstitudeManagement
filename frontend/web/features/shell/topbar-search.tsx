"use client";

import { FormEvent, KeyboardEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/icon";
import { managementApis } from "@/features/management/management-apis";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import { globalSearchWorkflows, workflowResultHref, workflowSuggestion } from "@/features/search/global-search-workflows";
import {
  filterAndRankSuggestions,
  globalSearchHref,
  itemSuggestion,
  matchesYear,
  moduleSearchMatch,
  moduleSearchResults,
  scopedHref,
  searchQuerySeed,
  searchResources,
  suggestionSearchMatch,
  type ModuleSearchResult,
  type SearchMatch,
  type SearchSuggestion,
} from "./topbar-search-model";
import { useWorkspaceNavigation } from "./use-record-entry-navigation";

type SearchGroup = {
  resource: ManagementResource;
  label: string;
  icon: string;
  items: SearchSuggestion[];
};

type SearchAction = {
  id: string;
  label: string;
  detail: string;
  href: string;
  icon: string;
  kind: "record" | "all" | "module" | "full";
  query: string;
  match?: SearchMatch;
  module?: string;
  code?: string;
};

export function TopbarSearch({ departmentId, year }: { departmentId: string; year: string }) {
  const pathname = usePathname();
  const navigateWorkspace = useWorkspaceNavigation();
  const input = useRef<HTMLInputElement>(null);
  const resultsId = useId();
  const [query, setQuery] = useState("");
  const [groups, setGroups] = useState<SearchGroup[]>([]);
  const [open, setOpen] = useState(false);
  const [searching, setSearching] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const modules = useMemo(() => moduleSearchResults(query), [query]);
  const matchedGroups = useMemo(() => groups.filter(group => group.items.length), [groups]);
  const actions = useMemo(() => buildActions(matchedGroups, modules, query, departmentId, year), [departmentId, matchedGroups, modules, query, year]);
  const previewActions = useMemo(() => actions.filter(action => action.kind === "record"), [actions]);
  const totalRecords = matchedGroups.reduce((total, group) => total + group.items.length, 0);

  useEffect(() => {
    const timer = window.setTimeout(() => resetSearch(), 0);
    return () => window.clearTimeout(timer);
  }, [pathname]);

  useEffect(() => {
    const text = searchQuerySeed(query);
    if (!text) {
      const timer = window.setTimeout(() => { setGroups([]); setSearching(false); }, 0);
      return () => window.clearTimeout(timer);
    }

    let cancelled = false;
    const timer = window.setTimeout(async () => {
      setSearching(true);
      const results = await Promise.allSettled(searchResources.map(async resource => {
        const items = await managementApis[resource.id].get(text, departmentId);
        const suggestions = (items as ManagementItem[])
          .filter(item => matchesYear(item, year))
          .map(item => itemSuggestion(item, resource.id));
        return {
          resource: resource.id,
          label: resource.label,
          icon: resource.icon,
          items: filterAndRankSuggestions(suggestions, query).map(result => result.item),
        } satisfies SearchGroup;
      }));
      if (cancelled) return;
      setGroups(results.flatMap(result => result.status === "fulfilled" ? [result.value] : []));
      setSearching(false);
    }, 220);
    return () => { cancelled = true; window.clearTimeout(timer); };
  }, [departmentId, query, year]);

  function resetSearch() {
    setQuery("");
    setGroups([]);
    setOpen(false);
    setSearching(false);
    setActiveIndex(-1);
  }

  function navigate(action: SearchAction) {
    resetSearch();
    input.current?.blur();
    navigateWorkspace(action.href);
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    const action = activeIndex >= 0 ? actions[activeIndex] : actions.find(candidate => candidate.kind === "full");
    if (action) navigate(action);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setOpen(true);
      setActiveIndex(index => actions.length ? index < 0 ? 0 : (index + 1) % actions.length : -1);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setOpen(true);
      setActiveIndex(index => actions.length ? index < 0 ? actions.length - 1 : (index - 1 + actions.length) % actions.length : -1);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      input.current?.blur();
    }
  }

  const showResults = open && Boolean(query.trim());
  return <form className="global-search" role="search" onSubmit={submit}>
    <div className="global-search-input">
      <Icon name="search" size={18}/>
      <input
        ref={input}
        aria-label="Search all institute modules"
        aria-autocomplete="list"
        aria-controls={resultsId}
        aria-expanded={showResults}
        aria-activedescendant={showResults && activeIndex >= 0 ? actions[activeIndex]?.id : undefined}
        role="combobox"
        placeholder="Search by character, word, code, or page..."
        value={query}
        onFocus={() => setOpen(true)}
        onBlur={() => window.setTimeout(() => setOpen(false), 140)}
        onChange={event => { setQuery(event.target.value); setGroups([]); setOpen(true); setSearching(Boolean(event.target.value.trim())); setActiveIndex(-1); }}
        onKeyDown={handleKeyDown}
      />
      {query && <button className="global-search-clear" type="button" aria-label="Clear global search" onMouseDown={event => event.preventDefault()} onClick={() => { resetSearch(); input.current?.focus(); }}><Icon name="close" size={14}/></button>}
    </div>

    {showResults && <div className="global-search-results" role="listbox" id={resultsId}>
      <header className="global-search-results-head">
        <span className="global-search-head-icon"><Icon name="search" size={17}/></span>
        <div><small>Institute-wide filter</small><strong>{query.trim()}</strong><span>{searching ? "Reading current institute data..." : "Ranked by exact, word, then character match."}</span></div>
        <div className="global-search-result-totals"><span><b>{totalRecords}</b> records</span><span><b>{modules.length}</b> pages</span></div>
      </header>
      <div className="global-search-results-body">
        {previewActions.length > 0 && <div className="global-search-groups">
          <div className="global-search-section-label"><span>Top workflow matches</span><small>A few strongest results with module, code, name, and match detail</small></div>
          <div className="global-search-preview-list">{previewActions.map(action => <ResultAction action={action} actions={actions} activeIndex={activeIndex} onNavigate={navigate} onActive={setActiveIndex} key={action.id}/>)}</div>
        </div>}
        {modules.length > 0 && <ModuleResults modules={modules.slice(0, 4)} actions={actions} activeIndex={activeIndex} departmentId={departmentId} year={year} onNavigate={navigate} onActive={setActiveIndex}/>}
        {!searching && !previewActions.length && !modules.length && <div className="global-search-empty"><span><Icon name="search" size={20}/></span><strong>No matches for &quot;{query.trim()}&quot;</strong><small>Try a shorter word, a code fragment, a person&apos;s name, course, room, department, or page.</small></div>}
      </div>
      {!searching && <footer className="global-search-results-foot">
        {actions.filter(action => action.kind === "full").map(action => <ResultAction action={action} actions={actions} activeIndex={activeIndex} onNavigate={navigate} onActive={setActiveIndex} key={action.id}/>) }
        <span>Use <kbd>Enter</kbd> for all workflows</span>
      </footer>}
    </div>}
  </form>;
}

function ModuleResults({ modules, actions, activeIndex, departmentId, year, onNavigate, onActive }: { modules: ModuleSearchResult[]; actions: SearchAction[]; activeIndex: number; departmentId: string; year: string; onNavigate: (action: SearchAction) => void; onActive: (index: number) => void }) {
  return <section className="global-search-pages">
    <header className="global-search-section-label"><span>Matching pages</span><small>Management, Enrollment, Record, and History workspaces</small></header>
    <div>{modules.map(module => {
      const action = actions.find(candidate => candidate.id === `search-module-${module.id}`) ?? {
        id: `search-module-${module.id}`,
        label: module.label,
        detail: module.section,
        href: scopedHref(module.href, departmentId, year),
        icon: module.icon,
        kind: "module" as const,
        query: "",
        module: `${module.section} / Page`,
        code: "PAGE",
      };
      return <ResultAction action={action} actions={actions} activeIndex={activeIndex} onNavigate={onNavigate} onActive={onActive} key={module.id}/>;
    })}</div>
  </section>;
}

function ResultAction({ action, actions, activeIndex, onNavigate, onActive }: { action: SearchAction; actions: SearchAction[]; activeIndex: number; onNavigate: (action: SearchAction) => void; onActive: (index: number) => void }) {
  const index = actions.indexOf(action);
  return <button id={action.id} className={`global-search-result global-search-result-${action.kind} ${index === activeIndex ? "active" : ""}`} type="button" role="option" aria-selected={index === activeIndex} onMouseDown={event => event.preventDefault()} onMouseEnter={() => onActive(index)} onClick={() => onNavigate(action)}>
    {action.kind !== "all" && <span className="global-search-result-icon"><Icon name={action.icon as Parameters<typeof Icon>[0]["name"]} size={14}/></span>}
    <span>{(action.module || action.code) && <span className="global-search-result-context">{action.module && <b>{action.module}</b>}{action.code && <code>{action.code}</code>}</span>}<strong><HighlightedText text={action.label} query={action.query}/></strong><small>{action.match && <b>{action.match.label} in {action.match.field}</b>}{action.detail}</small></span>
    <Icon name="arrow" size={13}/>
  </button>;
}

function buildActions(groups: SearchGroup[], modules: ModuleSearchResult[], query: string, departmentId: string, year: string): SearchAction[] {
  const recordActions = groups.flatMap(group => group.items.flatMap(item => globalSearchWorkflows.flatMap(workflow => {
    const staged = workflowSuggestion(item, group.resource, workflow.id);
    const match = suggestionSearchMatch(staged, query);
    return match ? [{
      id: `search-record-${workflow.id}-${group.resource}-${item.id}`,
      label: staged.label,
      detail: staged.detail,
      href: workflowResultHref(workflow.id, group.resource, staged.code || staged.label, departmentId, year),
      icon: group.icon,
      kind: "record" as const,
      query,
      match,
      module: `${workflow.label} / ${group.label}`,
      code: staged.code,
    }] : [];
  }))).toSorted((left, right) => (left.match?.rank ?? 9) - (right.match?.rank ?? 9) || left.label.localeCompare(right.label, undefined, { numeric: true })).slice(0, 6);
  const moduleActions = modules.slice(0, 4).map(module => ({
    id: `search-module-${module.id}`,
    label: module.label,
    detail: module.section,
    href: scopedHref(module.href, departmentId, year),
    icon: module.icon,
    kind: "module" as const,
    query,
    match: moduleSearchMatch(module, query),
    module: `${module.section} / Page`,
    code: "PAGE",
  }));
  const fullAction = allResultsAction(query, departmentId, year);
  return [...recordActions, ...moduleActions, ...(fullAction ? [fullAction] : [])];
}

function allResultsAction(query: string, departmentId: string, year: string): SearchAction | undefined {
  if (!query.trim()) return undefined;
  return {
    id: "search-all-results",
    label: "Open complete filtered results",
    detail: `Review every workflow match for "${query.trim()}"`,
    href: globalSearchHref(query, departmentId, year),
    icon: "search",
    kind: "full",
    query,
  };
}

function HighlightedText({ text, query }: { text: string; query: string }) {
  const terms = query.trim().split(/\s+/).filter(Boolean);
  if (!terms.length) return text;
  const pattern = new RegExp(`(${terms.map(escapeRegExp).join("|")})`, "ig");
  return <>{text.split(pattern).map((part, index) => terms.some(term => term.toLowerCase() === part.toLowerCase()) ? <mark key={`${part}-${index}`}>{part}</mark> : part)}</>;
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
