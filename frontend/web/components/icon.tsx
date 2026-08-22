type IconName = "dashboard" | "bolt" | "users" | "teacher" | "room" | "book" | "calendar" | "check" | "building" | "grade" | "chart" | "archive" | "settings" | "bell" | "search" | "menu" | "plus" | "arrow" | "pulse" | "close" | "trash" | "edit";

const paths: Record<IconName, React.ReactNode> = {
  dashboard: <><rect x="3" y="3" width="7" height="7" rx="2"/><rect x="14" y="3" width="7" height="7" rx="2"/><rect x="3" y="14" width="7" height="7" rx="2"/><rect x="14" y="14" width="7" height="7" rx="2"/></>,
  bolt: <path d="m13 2-9 12h8l-1 8 9-12h-8z"/>, users: <><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></>,
  teacher: <><path d="M3 3h18v12H3z"/><path d="m8 21 4-6 4 6M8 8h8"/></>, room: <><path d="M4 21V3h13v18M9 9h.01M9 13h.01M9 17h.01M17 8h3v13H2h20"/></>,
  book: <><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20V4H6.5A2.5 2.5 0 0 0 4 6.5z"/><path d="M4 6v13.5"/></>, calendar: <><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M16 3v4M8 3v4M3 11h18"/></>,
  check: <><path d="m9 12 2 2 4-4"/><circle cx="12" cy="12" r="9"/></>, building: <><path d="M3 21h18M6 21V7l6-4 6 4v14M9 10h.01M15 10h.01M9 14h.01M15 14h.01M10 21v-3h4v3"/></>,
  grade: <><path d="m12 3 3 6 6 .9-4.5 4.4 1 6.2-5.5-2.9-5.5 2.9 1-6.2L3 9.9 9 9z"/></>, chart: <><path d="M4 19V9M10 19V5M16 19v-7M22 19H2"/></>,
  archive: <><rect x="3" y="4" width="18" height="5" rx="1"/><path d="M5 9v11h14V9M10 13h4"/></>, settings: <><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.2h-4V21a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1L4.2 17l.1-.1a1.7 1.7 0 0 0 .3-1.9A1.7 1.7 0 0 0 3 14H2.8v-4H3a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9L4.2 7 7 4.2l.1.1a1.7 1.7 0 0 0 1.9.3 1.7 1.7 0 0 0 1-1.6v-.2h4V3a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.6 1h.2v4H21a1.7 1.7 0 0 0-1.6 1z"/></>,
  bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/></>, search: <><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></>,
  menu: <path d="M4 6h16M4 12h16M4 18h16"/>, plus: <path d="M12 5v14M5 12h14"/>, arrow: <path d="m9 18 6-6-6-6"/>, pulse: <path d="M3 12h4l2-6 4 12 2-6h6"/>, close: <path d="M18 6 6 18M6 6l12 12"/>, trash: <><path d="M3 6h18M8 6V4h8v2M19 6l-1 15H6L5 6M10 11v6M14 11v6"/></>, edit: <><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4z"/></>,
};

export function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden>{paths[name]}</svg>;
}
