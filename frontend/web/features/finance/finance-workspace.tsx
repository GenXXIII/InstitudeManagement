"use client";

import dynamic from "next/dynamic";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTableEmptyState, DataTableToolbar, ServerPaginatedDataRegion, useServerPage } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import { compareFinanceAccounts, FinanceTable } from "./finance-table";
import type { FinanceClosureReadiness, FinancialAccount, FinanceOptions } from "./finance-types";

import { formatDateTime, money } from "./finance-format";
import { emptyPage, type PagedResult } from "@/lib/pagination";

const FinanceAccountModal = dynamic(() => import("./accounts/finance-account-modal").then(module => module.FinanceAccountModal), { ssr: false });

const statuses = ["All", "Pending", "Partial", "Paid", "Closed", "Cancelled", "Refunded"];

export function FinanceWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [options, setOptions] = useState<FinanceOptions>();
  const [closure, setClosure] = useState<FinanceClosureReadiness>();
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [result, setResult] = useState<PagedResult<FinancialAccount>>(() => emptyPage());
  const [page, setPage] = useServerPage(`${departmentId}-${year}-${query}-${status}`);
  const [selectedId, setSelectedId] = useState("");
  const [accountsError, setAccountsError] = useState(false);
  const [contextError, setContextError] = useState(false);
  const [bulkDeclaring, setBulkDeclaring] = useState(false);
  const [closingAll, setClosingAll] = useState(false);
  const [bulkNotice, setBulkNotice] = useState<{ message: string; error: boolean }>();

  const loadAccounts = useCallback(async () => {
    try {
      const nextAccounts = await financeApi.getPage(query, status, departmentId, year, { page });
      setResult(nextAccounts);
      setAccountsError(false);
    } catch (reason) {
      setAccountsError(true);
      throw reason;
    }
  }, [departmentId, page, query, status, year]);

  const loadContext = useCallback(async () => {
    try {
      const [nextOptions, nextClosure] = await Promise.all([financeApi.getOptions(), financeApi.getClosureReadiness(departmentId, year)]);
      setOptions(nextOptions);
      setClosure(nextClosure);
      setContextError(false);
    } catch (reason) {
      setContextError(true);
      throw reason;
    }
  }, [departmentId, year]);

  const load = useCallback(() => Promise.all([loadAccounts(), loadContext()]), [loadAccounts, loadContext]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadAccounts().catch(() => undefined), 180);
    return () => window.clearTimeout(timer);
  }, [loadAccounts]);
  useEffect(() => {
    const timer = window.setTimeout(() => void loadContext().catch(() => undefined), 0);
    return () => window.clearTimeout(timer);
  }, [loadContext]);

  const view = useMemo(() => {
    const items = result.items;
    return {
      items: items.toSorted(compareFinanceAccounts),
      due: closure?.totalDue ?? 0,
      collected: closure?.totalCollected ?? 0,
      outstanding: closure?.totalOutstanding ?? 0,
      currency: closure?.currency ?? items[0]?.currency ?? "USD",
    };
  }, [closure, result.items]);

  async function declareAll() {
    if (!window.confirm(`Issue payment notices to all active students ${scopeDescription(departmentId, year)}? Existing unpaid notices will use the current configured price and receive a fresh expiry countdown.`)) return;
    setBulkDeclaring(true);
    setBulkNotice(undefined);
    try {
      const result = await financeApi.declareAll(departmentId, year);
      await load();
      setBulkNotice({
        message: result.declaredCount > 0
          ? `Payment notices issued to ${result.declaredCount} ${result.declaredCount === 1 ? "student" : "students"}. The expiry countdown started now and ends ${formatDateTime(result.expiresAtUtc)}.`
          : `Every active student ${scopeDescription(departmentId, year)} already has a payment notice.`,
        error: false,
      });
    } catch (reason) {
      setBulkNotice({ message: reason instanceof Error ? reason.message : "Could not issue payment notices.", error: true });
    } finally {
      setBulkDeclaring(false);
    }
  }

  async function closeAllPayments() {
    if (!closure?.canCloseAll || !window.confirm(`Finalize all ${closure.totalAccounts} current student payments ${scopeDescription(departmentId, year)}? Every matching account will become final and read-only together.`)) return;
    setClosingAll(true);
    setBulkNotice(undefined);
    try {
      const result = await financeApi.closeAllPayments(departmentId, year);
      await load();
      setBulkNotice({ message: `${result.closedCount} ${departmentId || year ? "filtered " : ""}student payment${result.closedCount === 1 ? "" : "s"} finalized together. They stay current until Semester Results are released and the semester ends.`, error: false });
    } catch (reason) {
      setBulkNotice({ message: reason instanceof Error ? reason.message : "Could not finalize semester payments.", error: true });
    } finally {
      setClosingAll(false);
    }
  }

  if (accountsError || contextError) return <ErrorPage retry={() => void load().catch(() => undefined)}/>;
  if (!options || !closure) return <LoadingPage/>;
  const selected = result.items.find(account => account.id === selectedId);
  const closeLabel = financeCloseLabel(closure, Boolean(departmentId || year), closingAll);

  return <div className="viewport-data-page management-viewport-page finance-viewport-page">
    <PageHeading eyebrow="Current financial state" title="Finance" description="Finance owns enrollment-linked fees, received payments, balances, adjustments, and financial eligibility. Audit events stay in History." actions={<div className="management-actions finance-heading-actions"><button type="button" className="button secondary" disabled={bulkDeclaring || closingAll} onClick={() => void declareAll()}>{bulkDeclaring ? "Issuing…" : "Issue Payment Notices"}</button><button type="button" className="button primary" disabled={!closure.canCloseAll || closingAll || bulkDeclaring} onClick={() => void closeAllPayments()}>{closeLabel}</button></div>}/>
    {bulkNotice && <div className={`finance-bulk-notice${bulkNotice.error ? " is-error" : ""}`} role={bulkNotice.error ? "alert" : "status"}><Icon name="finance" size={17}/><span>{bulkNotice.message}</span><button type="button" onClick={() => setBulkNotice(undefined)} aria-label="Dismiss message">Dismiss</button></div>}
    <section className="finance-metrics" aria-label="Finance summary">
      <FinanceMetric label="Total due" value={money(view.due, view.currency)} tone="blue"/>
      <FinanceMetric label="Collected" value={money(view.collected, view.currency)} tone="green"/>
      <FinanceMetric label="Outstanding" value={money(view.outstanding, view.currency)} tone="amber"/>
      <FinanceMetric label="Students paid" value={`${closure.paidAccounts}/${closure.totalAccounts}`} tone="violet"/>
    </section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search account, payment, student, or enrollment..." searchAriaLabel="Search Finance" resultLabel={`${result.totalCount} accounts`} className="record-toolbar panel finance-toolbar" searchClassName="record-search management-search module-search-field">
      <select className="finance-status-filter" aria-label="Filter finance accounts by status" value={status} onChange={event => setStatus(event.target.value)}>{statuses.map(item => <option key={item}>{item}</option>)}</select>
    </DataTableToolbar>
    <ServerPaginatedDataRegion result={{ ...result, items: view.items }} onPage={setPage} className="management-paginated-region" empty={<DataTableEmptyState icon={<Icon name="finance" size={24}/>} title="No active finance accounts" description="Payments archive only after all payments are finalized, Semester Results are released, and the semester ends."/>}>{pageItems => <FinanceTable accounts={pageItems} onSelect={account => setSelectedId(account.id)}/>}</ServerPaginatedDataRegion>
    {selected && <FinanceAccountModal
      account={selected}
      options={options}
      onClose={() => setSelectedId("")}
      onUpdated={updated => { if (updated.closedAtUtc && updated.periodState === "Retained") { setResult(current => ({ ...current, items: current.items.filter(account => account.id !== updated.id), totalCount: Math.max(0, current.totalCount - 1) })); setSelectedId(""); } else setResult(current => ({ ...current, items: current.items.map(account => account.id === updated.id ? updated : account) })); }}
    />}
  </div>;
}

function scopeDescription(departmentId: string, year: string) {
  if (departmentId && year) return "in the selected department and year";
  if (departmentId) return "in the selected department";
  if (year) return "in the selected year";
  return "in the current academic period";
}

function financeCloseLabel(closure: FinanceClosureReadiness, scoped: boolean, closing: boolean) {
  if (closing) return "Finalizing…";
  const finalized = closure.openPaidAccounts === 0 && closure.totalAccounts > 0 && closure.paidAccounts === closure.totalAccounts;
  if (finalized) return scoped ? "Filtered Payments Finalized" : "Semester Payments Finalized";
  return scoped ? "Finalize Filtered Payments" : "Finalize Semester Payments";
}

function FinanceMetric({ label, value, tone }: { label: string; value: string; tone: string }) {
  return <article className={`panel finance-metric finance-tone-${tone}`}><span>{label}</span><strong>{value}</strong></article>;
}
