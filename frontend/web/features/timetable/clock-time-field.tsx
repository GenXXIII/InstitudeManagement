"use client";

import { type CSSProperties, useEffect, useRef, useState } from "react";

type ClockMode = "hour" | "minute";

const minuteValues = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55];
const clockDialRadius = 82;
const clockInnerHandLength = 52;

export function ClockTimeField({ label, value, required = false, error, onChange }: {
  label: string;
  value: string;
  required?: boolean;
  error?: string;
  onChange: (value: string) => void;
}) {
  const root = useRef<HTMLDivElement>(null);
  const initial = parseTime(value);
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<ClockMode>("hour");
  const [hour, setHour] = useState(initial.hour);
  const [minute, setMinute] = useState(initial.minute);

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

  const hourAngle = (hour % 12) * 30;
  const minuteAngle = minute * 6;
  const period = hour < 12 ? "AM" : "PM";
  const formatted = formatTime(hour, minute);
  const values = mode === "hour"
    ? Array.from({ length: 12 }, (_, index) => period === "AM" ? index : index + 12)
    : minuteValues;

  function openPicker() {
    const next = parseTime(value);
    setHour(next.hour);
    setMinute(next.minute);
    setMode("hour");
    setOpen(true);
  }

  function selectDial(value: number) {
    if (mode === "hour") {
      setHour(value);
      setMode("minute");
      return;
    }
    setMinute(value);
  }

  function setPeriod(next: "AM" | "PM") {
    setHour(current => next === "PM" ? current % 12 + 12 : current % 12);
    setMode("hour");
  }

  function positionFor(index: number): CSSProperties {
    const angle = index * Math.PI / 6;
    return {
      left: `calc(50% + ${Math.sin(angle) * clockDialRadius}px)`,
      top: `calc(50% - ${Math.cos(angle) * clockDialRadius}px)`,
    };
  }

  return <div ref={root} className={`editor-field clock-time-field ${open ? "open" : ""} ${error ? "invalid" : ""}`}>
    <span>{label}{required && <b className="required-field-marker" aria-hidden> *</b>}</span>
    <button className="clock-time-trigger" type="button" aria-label={`${label}: ${value || "not selected"}`} aria-expanded={open} onClick={() => open ? setOpen(false) : openPicker()}>
      <span className={`clock-time-mini ${value ? "active" : ""}`} aria-hidden>
        <i className="clock-time-mini-hour" style={{ transform: `translateX(-50%) rotate(${hourAngle}deg)` }}/>
        <i className="clock-time-mini-minute" style={{ transform: `translateX(-50%) rotate(${minuteAngle}deg)` }}/>
      </span>
      <strong className={value ? "" : "placeholder"}>{value || "Choose time"}</strong>
      <small>Open clock</small>
    </button>
    {open && <section className="clock-time-popover" aria-label={`${label} clock picker`}>
      <header>
        <span>{mode === "hour" ? "Choose hour (00–23)" : "Choose minute (00–59)"}</span>
        <div className="clock-time-display">
          <button type="button" className={mode === "hour" ? "active" : ""} onClick={() => setMode("hour")}>{String(hour).padStart(2, "0")}</button>
          <b>:</b>
          <button type="button" className={mode === "minute" ? "active" : ""} onClick={() => setMode("minute")}>{String(minute).padStart(2, "0")}</button>
          <div className="clock-time-period">
            <button type="button" className={period === "AM" ? "active" : ""} onClick={() => setPeriod("AM")}>AM</button>
            <button type="button" className={period === "PM" ? "active" : ""} onClick={() => setPeriod("PM")}>PM</button>
          </div>
        </div>
      </header>
      <div className="clock-dial" aria-label={mode === "hour" ? "Select hour" : "Select minute"}>
        {mode === "hour" && <i
          className="clock-hand hour"
          style={{ height: hour >= 12 ? clockInnerHandLength : clockDialRadius, transform: `translateX(-50%) rotate(${hourAngle}deg)` }}
        />}
        {mode === "minute" && <i className="clock-hand minute" style={{ transform: `translateX(-50%) rotate(${minuteAngle}deg)` }}/>} 
        <i className="clock-pin"/>
        {values.map((dialValue, index) => <button
          type="button"
          className={(mode === "hour" ? hour === dialValue : minute === dialValue) ? "selected" : ""}
          style={positionFor(index)}
          onClick={() => selectDial(dialValue)}
          key={dialValue}
        >{mode === "minute" ? String(dialValue).padStart(2, "0") : dialValue}</button>)}
      </div>
      {mode === "minute" && <label className="clock-exact-minute"><span>Exact minute</span><input type="number" min="0" max="59" value={minute} onChange={event => setMinute(clampMinute(event.target.value))}/></label>}
      <footer><button type="button" onClick={() => setOpen(false)}>Cancel</button><button type="button" className="apply" onClick={() => { onChange(formatted); setOpen(false); }}>Use {formatted}</button></footer>
    </section>}
    {error && <small className="field-error">{error}</small>}
  </div>;
}

function parseTime(value: string) {
  const [hourText, minuteText] = value.split(":");
  const parsedHour = Number(hourText);
  const parsedMinute = Number(minuteText);
  return {
    hour: Number.isInteger(parsedHour) && parsedHour >= 0 && parsedHour <= 23 ? parsedHour : 8,
    minute: Number.isInteger(parsedMinute) && parsedMinute >= 0 && parsedMinute <= 59 ? parsedMinute : 0,
  };
}

function formatTime(hour: number, minute: number) {
  return `${String(hour).padStart(2, "0")}:${String(minute).padStart(2, "0")}`;
}

function clampMinute(value: string) {
  const minute = Number(value);
  if (!Number.isFinite(minute)) return 0;
  return Math.min(59, Math.max(0, Math.round(minute)));
}
