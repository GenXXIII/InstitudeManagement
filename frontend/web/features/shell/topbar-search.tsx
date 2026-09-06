"use client";

import { FormEvent, KeyboardEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { managementApis } from "@/features/management/management-apis";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import { workflowSourceSearch } from "@/lib/workflow-code";
import {
  itemSuggestion,
  managementSearchHref,
  matchesYear,
  moduleSearchResults,
  resourceFromPath,
  scopedHref,
  searchResources,
  type ModuleSearchResult,
  type SearchSuggestion,
} from "./topbar-search-model";

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
  kind: "record" | "all" | "module";
};

export function TopbarSearch({ departmentId, year }: { departmentId: string; year: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const input = useRef<HTMLInputElement>(null);
  const resultsId = useId();
  const [query, setQuery] = useState("");
  const [groups, setGroups] = useState<SearchGroup[]>([]);
  const [open, setOpen] = useState(false);
  const [searching, setSearching] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const modules = useMemo(() => moduleSearchResults(query), [query]);
  const visibleGroups = useMemo(() => groups.filter(group => group.items.length), [groups]);
  const actions = useMemo(() => buildActions(visibleGroups, modules, query, departmentId, year), [departmentId, modules, query, visibleGroups, year]);
  const totalRecords = visibleGroups.reduce((total, group) => total + group.items.length, 0);

  useEffect(() => {
    const timer = window.setTimeout(() => resetSearch(), 0);
    return () => window.clearTimeout(timer);
  }, [pathname]);

  useEffect(() => {
    const text = workflowSourceSearch(query);
    if (!text) {
      const timer = window.setTimeout(() => { setGroups([]); setSearching(false); }, 0);
      return () => window.clearTimeout(timer);
    }

    let cancelled = false;
    const timer = window.setTimeout(async () => {
      setSearching(true);
      const results = await Promise.allSettled(searchResources.map(async resource => {
        const items = await managementApis[resource.id].get(text, departmentId);
        return {
          resource: resource.id,
          label: resource.label,
          icon: resource.icon,
          items: (items as ManagementItem[]).filter(item => matchesYear(item, year)).map(item => itemSuggestion(item, resource.id)),
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
    router.push(action.href);
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    const exactModule = modules.find(module => module.label.toLowerCase() === query.trim().toLowerCase());
    const action = activeIndex >= 0
      ? actions[activeIndex]
      : exactModule
        ? actions.find(candidate => candidate.id === `search-module-${exactModule.id}`)
        : fallbackAction(pathname, query, departmentId, year, visibleGroups[0]?.resource);
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
        placeholder="Search people, courses, rooms, or pages..."
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
        <div><strong>Search across all modules</strong><span>{searching ? "Finding live institute data..." : `${totalRecords} data matches · ${modules.length} page matches`}</span></div>
        <small>Use ↑ ↓ and Enter</small>
      </header>
      <div className="global-search-results-body">
        {visibleGroups.length > 0 && <div className="global-search-groups">
          {visibleGroups.map(group => <SearchResultGroup group={group} actions={actions} activeIndex={activeIndex} onNavigate={navigate} onActive={setActiveIndex} key={group.resource}/>) }
        </div>}
        {modules.length > 0 && <ModuleResults modules={modules} actions={actions} activeIndex={activeIndex} departmentId={departmentId} year={year} onNavigate={navigate} onActive={setActiveIndex}/>}
        {!searching && !visibleGroups.length && !modules.length && <div className="global-search-empty"><span><Icon name="search" size={20}/></span><strong>No matches for “{query.trim()}”</strong><small>Try a person’s name, code, course, room, department, or module name.</small></div>}
      </div>
      <footer className="global-search-footer"><span>Results use the department and year selected in the top bar.</span><span><kbd>Enter</kbd> Open <kbd>Esc</kbd> Close</span></footer>
    </div>}
  </form>;
}

function SearchResultGroup({ group, actions, activeIndex, onNavigate, onActive }: { group: SearchGroup; actions: SearchAction[]; activeIndex: number; onNavigate: (action: SearchAction) => void; onActive: (index: number) => void }) {
  const visibleItems = group.items.slice(0, 2);
  const allAction = actions.find(action => action.id === `search-all-${group.resource}`);
  return <section className="global-search-group">
    <header><span><Icon name={group.icon as Parameters<typeof Icon>[0]["name"]} size={15}/></span><div><strong>{group.label}</strong><small>{group.items.length} {group.items.length === 1 ? "match" : "matches"}</small></div></header>
    {visibleItems.map(item => {
      const action = actions.find(candidate => candidate.id === `search-record-${group.resource}-${item.id}`);
      if (!action) return null;
      return <ResultAction action={action} actions={actions} activeIndex={activeIndex} onNavigate={onNavigate} onActive={onActive} key={item.id}/>;
    })}
    {allAction && <ResultAction action={allAction} actions={actions} activeIndex={activeIndex} onNavigate={onNavigate} onActive={onActive}/>}
  </section>;
}

function ModuleResults({ modules, actions, activeIndex, departmentId, year, onNavigate, onActive }: { modules: ModuleSearchResult[]; actions: SearchAction[]; activeIndex: number; departmentId: string; year: string; onNavigate: (action: SearchAction) => void; onActive: (index: number) => void }) {
  return <section className="global-search-pages">
    <header><div><strong>Pages and modules</strong><small>Jump directly to a workspace</small></div></header>
    <div>{modules.map(module => {
      const action = actions.find(candidate => candidate.id === `search-module-${module.id}`) ?? {
        id: `search-module-${module.id}`,
        label: module.label,
        detail: module.section,
        href: scopedHref(module.href, departmentId, year),
        icon: module.icon,
        kind: "module" as const,
      };
      return <ResultAction action={action} actions={actions} activeIndex={activeIndex} onNavigate={onNavigate} onActive={onActive} key={module.id}/>;
    })}</div>
  </section>;
}

function ResultAction({ action, actions, activeIndex, onNavigate, onActive }: { action: SearchAction; actions: SearchAction[]; activeIndex: number; onNavigate: (action: SearchAction) => void; onActive: (index: number) => void }) {
  const index = actions.indexOf(action);
  return <button id={action.id} className={`${action.kind === "all" ? "global-search-view-all" : ""} ${index === activeIndex ? "active" : ""}`} type="button" role="option" aria-selected={index === activeIndex} onMouseDown={event => event.preventDefault()} onMouseEnter={() => onActive(index)} onClick={() => onNavigate(action)}><span className="global-search-result-icon"><Icon name={action.icon as Parameters<typeof Icon>[0]["name"]} size={14}/></span><span><strong>{action.label}</strong><small>{action.detail}</small></span><Icon name="arrow" size={13}/></button>;
}

function buildActions(groups: SearchGroup[], modules: ModuleSearchResult[], query: string, departmentId: string, year: string): SearchAction[] {
  const recordActions = groups.flatMap(group => [
    ...group.items.slice(0, 2).map(item => ({
      id: `search-record-${group.resource}-${item.id}`,
      label: item.label,
      detail: item.detail,
      href: managementSearchHref(group.resource, item.label, departmentId, year),
      icon: group.icon,
      kind: "record" as const,
    })),
    {
      id: `search-all-${group.resource}`,
      label: `View all ${group.label.toLowerCase()} matches`,
      detail: `Open ${group.label} filtered by “${query.trim()}”`,
      href: managementSearchHref(group.resource, query, departmentId, year),
      icon: "search",
      kind: "all" as const,
    },
  ]);
  const moduleActions = modules.map(module => ({
    id: `search-module-${module.id}`,
    label: module.label,
    detail: module.section,
    href: scopedHref(module.href, departmentId, year),
    icon: module.icon,
    kind: "module" as const,
  }));
  return [...recordActions, ...moduleActions];
}

function fallbackAction(pathname: string, query: string, departmentId: string, year: string, preferredResource?: ManagementResource): SearchAction | undefined {
  if (!query.trim()) return undefined;
  const resource = preferredResource ?? resourceFromPath(pathname);
  const definition = searchResources.find(item => item.id === resource) ?? searchResources[0];
  return {
    id: "search-fallback",
    label: `Search ${definition.label}`,
    detail: query.trim(),
    href: managementSearchHref(resource, query, departmentId, year),
    icon: definition.icon,
    kind: "all",
  };
}
