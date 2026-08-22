"use client";

import { useId, useMemo, useRef, useState } from "react";
import { Icon } from "./icon";

export type SearchableOption = { id: string; label: string; detail?: string };

export function SearchableSelect({ value, options, placeholder, ariaLabel, required = false, className = "", onChange }: {
  value: string;
  options: SearchableOption[];
  placeholder: string;
  ariaLabel: string;
  required?: boolean;
  className?: string;
  onChange: (value: string) => void;
}) {
  const selected = options.find(option => option.id === value);
  const listId = useId();
  const input = useRef<HTMLInputElement>(null);
  const closeTimer = useRef<number | undefined>(undefined);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const visible = useMemo(() => {
    const text = query.trim().toLowerCase();
    if (!text || text === selected?.label.toLowerCase()) return options;
    return options.filter(option => option.label.toLowerCase().split(/\s+/).some(word => word.startsWith(text)) || option.detail?.toLowerCase().split(/\s+/).some(word => word.startsWith(text)));
  }, [options, query, selected?.label]);

  function cancelClose() {
    if (closeTimer.current !== undefined) window.clearTimeout(closeTimer.current);
    closeTimer.current = undefined;
  }

  function openMenu() {
    cancelClose();
    setOpen(true);
    setQuery(selected?.label ?? "");
    setActive(Math.max(options.findIndex(option => option.id === value), 0));
    window.requestAnimationFrame(() => input.current?.select());
  }

  function choose(option: SearchableOption) {
    cancelClose();
    onChange(option.id);
    setQuery(option.label);
    setOpen(false);
    setActive(0);
  }

  return <div className={`searchable-select ${open ? "open" : ""} ${className}`} onMouseDown={event => {
    const target = event.target as HTMLElement;
    if (target === input.current || target.closest(".searchable-select-menu,.searchable-select-arrow")) return;
    event.preventDefault();
    input.current?.focus();
    openMenu();
  }}>
    <Icon name="search" size={14}/>
    <input
      ref={input}
      aria-label={ariaLabel}
      aria-autocomplete="list"
      aria-controls={listId}
      aria-expanded={open}
      role="combobox"
      required={required}
      value={open ? query : selected?.label ?? ""}
      placeholder={placeholder}
      onFocus={openMenu}
      onChange={event => { cancelClose(); setQuery(event.target.value); setOpen(true); setActive(0); }}
      onBlur={() => { closeTimer.current = window.setTimeout(() => { setOpen(false); setQuery(""); closeTimer.current = undefined; }, 150); }}
      onKeyDown={event => {
        if (event.key === "ArrowDown") { event.preventDefault(); setOpen(true); setActive(index => Math.min(index + 1, Math.max(visible.length - 1, 0))); }
        if (event.key === "ArrowUp") { event.preventDefault(); setActive(index => Math.max(index - 1, 0)); }
        if (event.key === "Enter" && open && visible[active]) { event.preventDefault(); choose(visible[active]); }
        if (event.key === "Escape") { cancelClose(); setOpen(false); setQuery(""); input.current?.blur(); }
      }}
    />
    <button className="searchable-select-arrow" type="button" aria-label={open ? "Close options" : "Open options"} onMouseDown={event => event.preventDefault()} onClick={() => { if (open) { cancelClose(); setOpen(false); setQuery(""); input.current?.blur(); } else { input.current?.focus(); openMenu(); } }}>⌄</button>
    {open && <div className="searchable-select-menu" role="listbox" id={listId}>
      {visible.length ? visible.map((option, index) => <button className={`${index === active ? "active" : ""} ${option.id === value ? "selected" : ""}`} type="button" role="option" aria-selected={option.id === value} onMouseDown={event => event.preventDefault()} onClick={() => choose(option)} key={option.id || "all"}><span>{option.label}</span>{option.detail && <small>{option.detail}</small>}</button>) : <p>No matching options</p>}
    </div>}
  </div>;
}
