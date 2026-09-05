"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/departments/department-types";

export type ShellScopeKey = "departmentId" | "year";

export function useShellScopes(pathname: string, enabled: boolean) {
  const router = useRouter();
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [departmentScope, setDepartmentScope] = useState("");
  const [yearScope, setYearScope] = useState("");

  useEffect(() => {
    if (!enabled) return;
    departmentApi.get().then(setDepartments).catch(() => setDepartments([]));
  }, [enabled]);

  useEffect(() => {
    const sync = () => {
      const params = new URLSearchParams(window.location.search);
      setDepartmentScope(params.get("departmentId") ?? "");
      setYearScope(params.get("year") ?? "");
    };
    const timer = window.setTimeout(sync, 0);
    window.addEventListener("popstate", sync);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener("popstate", sync);
    };
  }, [pathname]);

  const departmentOptions = useMemo(
    () => [
      { id: "", label: "All departments" },
      ...departments.map((department) => ({ id: department.id, label: department.values.name })),
    ],
    [departments],
  );

  function changeScope(key: ShellScopeKey, value: string) {
    if (key === "departmentId") setDepartmentScope(value);
    else setYearScope(value);

    const params = new URLSearchParams(window.location.search);
    if (value) params.set(key, value);
    else params.delete(key);
    router.replace(`${pathname}${params.size ? `?${params}` : ""}`, { scroll: false });
  }

  return { departmentOptions, departmentScope, yearScope, changeScope };
}
