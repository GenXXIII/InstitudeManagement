"use client";

import { useEffect, useRef, useState } from "react";
import { Icon } from "@/components/icon";

type ScheduleOption = { id: string; label: string };

export function ScheduleSelectField({ label, value, options, required = false, error, onChange }: {
  label: string;
  value: string;
  options: ScheduleOption[];
  required?: boolean;
  error?: string;
  onChange: (value: string) => void;
}) {
  const root = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const selected = options.find(option => option.id === value);

  useEffect(() => {
    if (!open) return;
    function close(event: MouseEvent) {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    }
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [open]);

  return <div ref={root} className={`editor-field schedule-select-field ${open ? "open" : ""} ${error ? "invalid" : ""}`}>
    <span>{label}{required && <b className="required-field-marker" aria-hidden> *</b>}</span>
    <button
      className="schedule-select-trigger"
      type="button"
      aria-label={label}
      aria-haspopup="listbox"
      aria-expanded={open}
      onClick={() => setOpen(current => !current)}
    >
      <span className={selected ? "" : "placeholder"}>{selected?.label ?? `Select ${label.toLowerCase()}`}</span>
      <Icon name="arrow" size={15}/>
    </button>
    {open && <div className="schedule-select-menu" role="listbox" aria-label={`${label} options`}>
      {options.map(option => <button
        type="button"
        role="option"
        aria-selected={option.id === value}
        className={option.id === value ? "selected" : ""}
        onClick={() => { onChange(option.id); setOpen(false); }}
        key={option.id}
      >
        <span>{option.label}</span>
        {option.id === value && <Icon name="check" size={15}/>}
      </button>)}
    </div>}
    {error && <small className="field-error">{error}</small>}
  </div>;
}
