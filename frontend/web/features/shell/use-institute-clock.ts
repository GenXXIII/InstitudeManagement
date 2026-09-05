"use client";

import { useEffect, useState } from "react";

export function useInstituteClock(language: string, timeZone: string, dateFormat: string) {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1_000);
    return () => window.clearInterval(timer);
  }, []);

  const locale = language.toLowerCase().startsWith("kh") ? "km-KH" : "en-GB";
  const zone = timeZone || "Asia/Bangkok";
  const date = formatDate(now, locale, zone, dateFormat);
  const time = new Intl.DateTimeFormat(locale, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hourCycle: "h23",
    timeZone: zone,
  }).format(now);

  return `${date} · ${time}`;
}

function formatDate(date: Date, locale: string, timeZone: string, format = "DD MMM YYYY") {
  const normalized = format.trim().toUpperCase();
  if (normalized === "YYYY-MM-DD") {
    return new Intl.DateTimeFormat("en-CA", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  }
  if (normalized === "MM/DD/YYYY") {
    return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  }
  if (normalized === "DD/MM/YYYY") {
    return new Intl.DateTimeFormat("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  }
  return new Intl.DateTimeFormat(locale, { day: "2-digit", month: "short", year: "numeric", timeZone }).format(date);
}
