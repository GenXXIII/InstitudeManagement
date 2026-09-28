"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import { compareFinanceAccounts, FinanceTable } from "./finance-table";
import type { FinanceClosureReadiness, FinancialAccount, FinanceOptions } from "./finance-types";

import { FinanceAccountModal } from "./accounts/finance-account-modal";
import { formatDateTime, money } from "./finance-format";

const statuses = ["All", "Pending", "Partial", "Paid", "Closed", "Cancelled", "Refunded"];

export function FinanceWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [accounts, setAccounts] = useState<FinancialAccount[]>();
  const [options, setOptions] = useState<FinanceOptions>();
  const [closure, setClosure] = useState<FinanceClosureReadiness>();
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [selectedId, setSelectedId] = useState("");
  const [error, setError] = useState(false);
  const [bulkDeclaring, setBulkDeclaring] = useState(false);
  const [closingAll, setClosingAll] = useState(false);
  const [bulkNotice, setBulkNotice] = useState<{ message: string; error: boolean }>();

  const load = useCallback(async () => {
    try {
      const [nextAccounts, nextOptions, nextClosure] = await Promise.all([financeApi.get(query, status, departmentId, year), financeApi.getOptions(), financeApi.getClosureReadiness(departmentId, year)]);
      setAccounts(nextAccounts);
      setOptions(nextOptions);
      setClosure(nextClosure);
      setError(false);
    } catch {
      setError(true);
    }
  }, [departmentId, query, status, year]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  const view = useMemo(() => {
    const items = accounts ?? [];
    const declared = items.filter(account => account.isDeclared);
    return {
      items: items.toSorted(compareFinanceAccounts),
      declared: declared.length,
      due: declared.reduce((total, account) => total + account.totalDue, 0),
      collected: declared.reduce((total, account) => total + account.totalPaid, 0),
      outstanding: declared.reduce((total, account) => total + account.balance, 0),
      currency: items[0]?.currency ?? "USD",
    };
  }, [accounts]);

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

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!accounts || !options || !closure) return <LoadingPage/>;
  const selected = accounts.find(account => account.id === selectedId);
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
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search account, payment, student, or enrollment..." searchAriaLabel="Search Finance" resultLabel={`${view.items.length} accounts`} className="record-toolbar panel finance-toolbar" searchClassName="record-search management-search module-search-field">
      <select className="finance-status-filter" aria-label="Filter finance accounts by status" value={status} onChange={event => setStatus(event.target.value)}>{statuses.map(item => <option key={item}>{item}</option>)}</select>
    </DataTableToolbar>
    <PaginatedDataRegion items={view.items} resetKey={`${departmentId}-${year}-${query}-${status}`} className="management-paginated-region" empty={<DataTableEmptyState icon={<Icon name="finance" size={24}/>} title="No active finance accounts" description="Payments archive only after all payments are finalized, Semester Results are released, and the semester ends."/>}>{pageItems => <FinanceTable accounts={pageItems} onSelect={account => setSelectedId(account.id)}/>}</PaginatedDataRegion>
    {selected && <FinanceAccountModal
      account={selected}
      options={options}
      onClose={() => setSelectedId("")}
      onUpdated={updated => { if (updated.closedAtUtc && updated.periodState === "Retained") { setAccounts(current => current?.filter(account => account.id !== updated.id)); setSelectedId(""); } else setAccounts(current => current?.map(account => account.id === updated.id ? updated : account)); }}
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
