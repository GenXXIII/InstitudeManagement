"use client";

import { useEffect, useRef } from "react";
import { useRouter } from "next/navigation";

type RecordEntrySequence = {
  parent: string;
  detail: string;
  step: "home" | "parent" | "detail";
};

export function useRecordEntryNavigation(pathname: string) {
  const router = useRouter();
  const initialEntryChecked = useRef(false);
  const sequence = useRef<RecordEntrySequence | undefined>(undefined);

  useEffect(() => {
    if (initialEntryChecked.current) return;
    initialEntryChecked.current = true;
    if (sequence.current || window.history.state?.inkRecordBackSequence) return;

    const parentPath = recordParentPath(pathname);
    if (!parentPath) return;
    const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined;
    if (navigation?.type === "reload" || navigation?.type === "back_forward") return;
    try {
      if (document.referrer && new URL(document.referrer).origin === window.location.origin) return;
    } catch {
      // An invalid referrer is treated as an external entry.
    }

    const search = window.location.search;
    sequence.current = {
      parent: `${parentPath}${search}`,
      detail: `${pathname}${search}`,
      step: "home",
    };
    router.replace("/", { scroll: false });
  }, [pathname, router]);

  useEffect(() => {
    const current = sequence.current;
    if (!current) return;

    if (current.step === "home" && pathname === "/") {
      current.step = "parent";
      router.push(current.parent, { scroll: false });
      return;
    }
    if (current.step === "parent" && `${pathname}${window.location.search}` === current.parent) {
      current.step = "detail";
      router.push(current.detail, { scroll: false });
      return;
    }
    if (current.step === "detail" && `${pathname}${window.location.search}` === current.detail) {
      window.history.replaceState({ ...window.history.state, inkRecordBackSequence: true }, "", current.detail);
      sequence.current = undefined;
    }
  }, [pathname, router]);
}

function recordParentPath(pathname: string) {
  const match = pathname.match(/^\/(record|records)\/([^/]+)\/[^/]+$/);
  return match ? `/${match[1]}/${match[2]}` : undefined;
}
