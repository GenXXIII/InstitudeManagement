"use client";

import { useCallback, useEffect, useRef } from "react";
import { usePathname, useRouter } from "next/navigation";

type EntrySequence = {
  routes: string[];
  index: number;
};

type RouteKind = "home" | "workspace" | "detail" | "other";

const workspaceRoots = new Set(["management", "enrollment", "operation", "record", "record-history", "records", "announce", "settings", "search"]);
const detailRoots = new Set(["record", "record-history", "records", "announce"]);

/**
 * Keeps application history shallow and predictable:
 * external -> dashboard -> current workspace -> optional detail.
 * Moving between workspaces replaces the workspace slot instead of accumulating
 * every Management, Enrollment, Record, and History page the user visited.
 */
export function useApplicationEntryNavigation(pathname: string) {
  const router = useRouter();
  const initialEntryChecked = useRef(false);
  const sequence = useRef<EntrySequence | undefined>(undefined);

  useEffect(() => {
    if (initialEntryChecked.current) return;
    initialEntryChecked.current = true;
    if (sequence.current || window.history.state?.inkApplicationBackSequence || window.history.state?.inkRecordBackSequence) return;

    const routes = entryRoutes(pathname, window.location.search);
    if (!routes) return;
    const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined;
    if (navigation?.type === "reload" || navigation?.type === "back_forward") return;
    try {
      if (document.referrer && new URL(document.referrer).origin === window.location.origin && window.history.length > 1) return;
    } catch {
      // An invalid referrer is treated as an external entry.
    }

    sequence.current = { routes, index: 0 };
    router.replace(routes[0], { scroll: false });
  }, [pathname, router]);

  useEffect(() => {
    const current = sequence.current;
    if (!current) return;
    const location = `${pathname}${window.location.search}`;
    if (location !== current.routes[current.index]) return;

    if (current.index < current.routes.length - 1) {
      current.index += 1;
      router.push(current.routes[current.index], { scroll: false });
      return;
    }

    window.history.replaceState({ ...window.history.state, inkApplicationBackSequence: true }, "", current.routes[current.index]);
    sequence.current = undefined;
  }, [pathname, router]);

  const navigateWorkspace = useCallback((href: string) => {
    const target = new URL(href, window.location.href);
    if (target.origin !== window.location.origin) {
      window.location.assign(target.href);
      return;
    }
    moveToWorkspace(router, pathname, `${target.pathname}${target.search}${target.hash}`);
  }, [pathname, router]);

  useEffect(() => {
    function interceptWorkspaceLink(event: MouseEvent) {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
      const target = event.target;
      if (!(target instanceof Element)) return;
      const anchor = target.closest<HTMLAnchorElement>("a[href]");
      if (!anchor || anchor.target || anchor.hasAttribute("download")) return;

      const destination = new URL(anchor.href, window.location.href);
      if (destination.origin !== window.location.origin) return;
      const currentKind = routeKind(pathname);
      const targetKind = routeKind(destination.pathname);
      const href = `${destination.pathname}${destination.search}${destination.hash}`;

      if (targetKind === "home" && (currentKind === "workspace" || currentKind === "detail")) {
        event.preventDefault();
        window.history.go(currentKind === "detail" ? -2 : -1);
        return;
      }
      if (targetKind === "workspace" && currentKind !== "home" && currentKind !== "other") {
        event.preventDefault();
        navigateWorkspace(href);
      }
    }

    document.addEventListener("click", interceptWorkspaceLink, true);
    return () => document.removeEventListener("click", interceptWorkspaceLink, true);
  }, [navigateWorkspace, pathname]);
}

export function useWorkspaceNavigation() {
  const pathname = usePathname();
  const router = useRouter();
  return useCallback((href: string) => moveToWorkspace(router, pathname, href), [pathname, router]);
}

function moveToWorkspace(router: ReturnType<typeof useRouter>, pathname: string, href: string) {
  const currentKind = routeKind(pathname);
  if (currentKind === "home" || currentKind === "other") {
    router.push(href);
    return;
  }
  if (currentKind === "detail") {
    window.addEventListener("popstate", () => window.setTimeout(() => router.push(href, { scroll: false }), 0), { once: true });
    window.history.go(-2);
    return;
  }
  router.replace(href);
}

function entryRoutes(pathname: string, search: string) {
  const kind = routeKind(pathname);
  if (kind !== "workspace" && kind !== "detail") return undefined;
  const destination = `${pathname}${search}`;
  if (kind === "detail") return ["/", `${parentPath(pathname)}${search}`, destination];
  return ["/", destination];
}

function routeKind(pathname: string): RouteKind {
  if (pathname === "/") return "home";
  const segments = pathname.split("/").filter(Boolean);
  if (!segments.length || !workspaceRoots.has(segments[0])) return "other";
  if (detailRoots.has(segments[0]) && segments.length >= 3) return "detail";
  return "workspace";
}

function parentPath(pathname: string) {
  return pathname.split("/").slice(0, -1).join("/") || "/";
}
