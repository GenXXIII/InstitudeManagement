"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ManagementDataCell } from "@/components/management-data-cell";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import type { FinancialAccount, FinancialPayment, FinanceOptions, PaymentDraft, PaymentStatus } from "./finance-types";

const columns = ["Account", "Student", "Enrollment", "Period", "Fees", "Paid", "Balance", "Status", "Due", "Actions"];
const statuses = ["All", "Pending", "Partial", "Paid", "Cancelled", "Refunded"];

export function FinanceWorkspace() {
  const [accounts, setAccounts] = useState<FinancialAccount[]>();
  const [options, setOptions] = useState<FinanceOptions>();
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [selectedId, setSelectedId] = useState("");
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const [nextAccounts, nextOptions] = await Promise.all([financeApi.get(query, status), financeApi.getOptions()]);
      setAccounts(nextAccounts);
      setOptions(nextOptions);
      setError(false);
    } catch {
      setError(true);
    }
  }, [query, status]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  const view = useMemo(() => {
    const items = accounts ?? [];
    return {
      items,
      due: items.reduce((total, account) => total + account.totalDue, 0),
      collected: items.reduce((total, account) => total + account.totalPaid, 0),
      outstanding: items.reduce((total, account) => total + account.balance, 0),
      eligible: items.filter(account => account.status === "Paid").length,
      currency: items[0]?.currency ?? "USD",
    };
  }, [accounts]);

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!accounts || !options) return <LoadingPage/>;
  const selected = accounts.find(account => account.id === selectedId);

  return <div className="viewport-data-page management-viewport-page finance-viewport-page">
    <PageHeading eyebrow="Current financial state" title="Finance" description="Finance owns enrollment-linked fees, received payments, balances, adjustments, and financial eligibility. Configuration stays in Settings; audit events stay in History." actions={<Link className="button secondary" href="/settings/finance">Finance Settings</Link>}/>
    <section className="finance-metrics" aria-label="Finance summary">
      <FinanceMetric label="Total due" value={money(view.due, view.currency)} detail={`${view.items.length} financial accounts`} tone="blue"/>
      <FinanceMetric label="Collected" value={money(view.collected, view.currency)} detail="Completed payment transactions" tone="green"/>
      <FinanceMetric label="Outstanding" value={money(view.outstanding, view.currency)} detail="Pending and partial balances" tone="amber"/>
      <FinanceMetric label="Eligible" value={view.eligible.toString()} detail={options.requirePaidForAdvancement ? "Paid accounts may advance" : "Paid gate disabled in Settings"} tone="violet"/>
    </section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search account, payment, student, or enrollment..." contextLabel="Finance boundary" contextValue="Current truth only">
      <label className="finance-status-filter"><span>Status</span><select value={status} onChange={event => setStatus(event.target.value)}>{statuses.map(item => <option key={item}>{item}</option>)}</select></label>
    </DataTableToolbar>
    <PaginatedDataRegion items={view.items} resetKey={`${query}-${status}`} className="management-paginated-region" empty={<DataTableEmptyState icon={<Icon name="finance" size={24}/>} title="No finance accounts" description="A financial account appears after Student Enrollment is saved."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table finance-payment-table" headerClassName="horizontal-management-head" rowSelector=":scope > .horizontal-management-row" columns={columns}>
      {pageItems.map(account => <article className="horizontal-management-row" key={account.id}>
        <Cell label="Account"><strong className="management-code-value">{account.financialAccountCode}</strong></Cell>
        <Cell label="Student"><span className="finance-student"><strong>{account.studentName}</strong><small>{account.studentCode}</small></span></Cell>
        <Cell label="Enrollment"><span><strong>{account.enrollmentCode}</strong><small>{account.department} / Year {account.yearLevel} / {account.shift}</small></span></Cell>
        <Cell label="Period"><span><strong>{account.academicYear}</strong><small>{account.semester}</small></span></Cell>
        <Cell label="Fees"><span><strong>{money(account.totalDue, account.currency)}</strong><small>Tuition {money(account.tuitionFee, account.currency)} · Other {money(account.otherFee, account.currency)}</small></span></Cell>
        <Cell label="Paid"><strong>{money(account.totalPaid, account.currency)}</strong></Cell>
        <Cell label="Balance"><strong>{money(account.balance, account.currency)}</strong></Cell>
        <Cell label="Status"><span className={`table-status finance-state-${account.status.toLowerCase()}`}>{account.status}</span></Cell>
        <Cell label="Due"><time>{formatDate(account.dueOn)}</time></Cell>
        <Cell label="Actions" className="management-action-cell"><button type="button" className="button secondary finance-manage-button" onClick={() => setSelectedId(account.id)}>Manage</button></Cell>
      </article>)}
    </DataTable>}</PaginatedDataRegion>
    {selected && <FinanceAccountModal
      account={selected}
      options={options}
      onClose={() => setSelectedId("")}
      onUpdated={updated => setAccounts(current => current?.map(account => account.id === updated.id ? updated : account))}
    />}
  </div>;
}

function FinanceAccountModal({ account: initialAccount, options, onClose, onUpdated }: { account: FinancialAccount; options: FinanceOptions; onClose: () => void; onUpdated: (account: FinancialAccount) => void }) {
  const [account, setAccount] = useState(initialAccount);
  const [paymentDraft, setPaymentDraft] = useState<PaymentDraft>(() => emptyPayment(initialAccount.balance));
  const [adjustmentAmount, setAdjustmentAmount] = useState(initialAccount.adjustmentAmount.toString());
  const [adjustmentReason, setAdjustmentReason] = useState(initialAccount.adjustmentReason);
  const [editing, setEditing] = useState<FinancialPayment>();
  const [editDraft, setEditDraft] = useState<PaymentDraft>();
  const [showQr, setShowQr] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");

  async function apply(action: () => Promise<FinancialAccount>) {
    setBusy(true);
    setMessage("");
    try {
      const updated = await action();
      setAccount(updated);
      onUpdated(updated);
      setPaymentDraft(emptyPayment(updated.balance));
      setAdjustmentAmount(updated.adjustmentAmount.toString());
      setAdjustmentReason(updated.adjustmentReason);
      setEditing(undefined);
      setEditDraft(undefined);
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : "Could not apply the finance change.");
    } finally {
      setBusy(false);
    }
  }

  async function recordPayment(event: React.FormEvent) {
    event.preventDefault();
    await apply(() => financeApi.recordPayment(account.id, paymentDraft));
  }

  async function updatePayment(event: React.FormEvent) {
    event.preventDefault();
    if (!editing || !editDraft) return;
    await apply(() => financeApi.updatePayment(account.id, editing.id, editDraft));
  }

  async function changePaymentStatus(payment: FinancialPayment, status: Exclude<PaymentStatus, "Completed">) {
    if (!confirm(`${status === "Refunded" ? "Refund" : "Cancel"} ${payment.paymentCode}? The current balance will be recalculated and History will keep the event.`)) return;
    await apply(() => financeApi.setPaymentStatus(account.id, payment.id, status));
  }

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><section className="modal finance-account-modal" role="dialog" aria-modal="true" aria-label={`Finance account for ${account.studentName}`}>
    <div className="modal-head"><div><span className="eyebrow">Financial account</span><h2>{account.studentName}</h2><p>{account.financialAccountCode} · {account.enrollmentCode} · {account.academicYear} · {account.semester}</p></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close"><Icon name="close"/></button></div>
    <div className="finance-account-scroll">
      {message && <div className="management-rule-error" role="alert"><Icon name="finance" size={16}/><div><strong>Could not apply change</strong><span>{message}</span></div><button type="button" onClick={() => setMessage("")}>Dismiss</button></div>}
      <section className="finance-account-summary" aria-label="Financial account balance">
        <FinanceAmount label="Tuition fee" value={account.tuitionFee} currency={account.currency}/>
        <FinanceAmount label="Other fee" value={account.otherFee} currency={account.currency}/>
        <FinanceAmount label="Adjustment" value={account.adjustmentAmount} currency={account.currency}/>
        <FinanceAmount label="Total due" value={account.totalDue} currency={account.currency}/>
        <FinanceAmount label="Paid" value={account.totalPaid} currency={account.currency}/>
        <FinanceAmount label="Balance" value={account.balance} currency={account.currency} strong/>
      </section>

      <section className="finance-current-state"><div><span>Payment status</span><strong className={`finance-state-${account.status.toLowerCase()}`}>{account.status}</strong></div><div><span>Enrollment eligibility</span><strong>{options.requirePaidForAdvancement ? account.status === "Paid" ? "Eligible" : "Stay in current enrollment" : "Payment gate disabled"}</strong></div><div><span>Payment methods</span><strong>{options.paymentMethods.join(", ")}</strong></div></section>

      {account.status !== "Cancelled" && account.balance > 0 && <form className="finance-operation-card" onSubmit={recordPayment}>
        <header><div><strong>Record payment</strong><span>Capture actual money received. The balance and status are calculated by Finance.</span></div><button type="button" className="button secondary finance-qr-button" onClick={() => setShowQr(value => !value)}>{showQr ? "Hide QR" : "Generate QR"}</button></header>
        {showQr && <div className="finance-inline-qr"><QRCodeSVG value={account.qrPayload} size={150} level="M" marginSize={2}/><span>Student scan pays the current balance of {money(account.balance, account.currency)}.</span></div>}
        <div className="finance-form-grid">
          <label><span>Amount</span><input type="number" min="0.01" step="0.01" value={paymentDraft.amount} onChange={event => setPaymentDraft({ ...paymentDraft, amount: event.target.value })} required/></label>
          <label><span>Payment method</span><select value={paymentDraft.method} onChange={event => setPaymentDraft({ ...paymentDraft, method: event.target.value })} required><option value="">Select payment method</option>{options.paymentMethods.map(method => <option key={method}>{method}</option>)}</select></label>
          <label><span>Transaction reference</span><input value={paymentDraft.transactionReference} onChange={event => setPaymentDraft({ ...paymentDraft, transactionReference: event.target.value })} placeholder="Optional bank or receipt reference"/></label>
          <label><span>Paid at</span><input type="datetime-local" value={paymentDraft.paidAtUtc} onChange={event => setPaymentDraft({ ...paymentDraft, paidAtUtc: event.target.value })}/></label>
        </div>
        <footer><small>{options.allowPartialPayments ? "Partial payments are allowed." : "Payment must clear the full balance."} {options.allowOverpayment ? "Overpayment is allowed." : "Overpayment is blocked."}</small><button className="button primary" disabled={busy}>{busy ? "Saving..." : "Record payment"}</button></footer>
      </form>}

      <form className="finance-operation-card" onSubmit={event => { event.preventDefault(); void apply(() => financeApi.adjust(account.id, Number(adjustmentAmount), adjustmentReason)); }}>
        <header><div><strong>Discount / adjustment</strong><span>Use a negative amount for a discount or a positive amount for an extra charge.</span></div></header>
        <div className="finance-form-grid finance-adjustment-grid"><label><span>Adjustment amount</span><input type="number" step="0.01" value={adjustmentAmount} onChange={event => setAdjustmentAmount(event.target.value)} required/></label><label><span>Reason</span><input value={adjustmentReason} onChange={event => setAdjustmentReason(event.target.value)} placeholder="Required when amount is not zero"/></label></div>
        <footer><small>Maximum absolute adjustment: {money(options.maximumAdjustmentAmount, account.currency)}.</small><button className="button secondary" disabled={busy}>{busy ? "Saving..." : "Apply adjustment"}</button></footer>
      </form>

      <section className="finance-transactions">
        <header><div><strong>Payments</strong><span>Current payment transactions. Corrections and status changes are written to History.</span></div><b>{account.payments.length}</b></header>
        {editing && editDraft && <form className="finance-payment-edit" onSubmit={updatePayment}><strong>Edit {editing.paymentCode}</strong><div className="finance-form-grid"><label><span>Amount</span><input type="number" min="0.01" step="0.01" value={editDraft.amount} onChange={event => setEditDraft({ ...editDraft, amount: event.target.value })} required/></label><label><span>Payment method</span><select value={editDraft.method} onChange={event => setEditDraft({ ...editDraft, method: event.target.value })} required><option value="">Select payment method</option>{options.paymentMethods.map(method => <option key={method}>{method}</option>)}</select></label><label><span>Transaction reference</span><input value={editDraft.transactionReference} onChange={event => setEditDraft({ ...editDraft, transactionReference: event.target.value })}/></label><label><span>Paid at</span><input type="datetime-local" value={editDraft.paidAtUtc} onChange={event => setEditDraft({ ...editDraft, paidAtUtc: event.target.value })}/></label></div><footer><button type="button" className="button secondary" onClick={() => { setEditing(undefined); setEditDraft(undefined); }}>Cancel edit</button><button className="button primary" disabled={busy}>{busy ? "Saving..." : "Save correction"}</button></footer></form>}
        <div className="finance-transaction-list">{account.payments.map(payment => <article key={payment.id}><div><span>Payment ID</span><strong>{payment.paymentCode}</strong></div><div><span>Amount</span><strong>{money(payment.amount, account.currency)}</strong></div><div><span>Method</span><strong>{payment.method}</strong></div><div><span>Status</span><strong className={`finance-payment-state-${payment.status.toLowerCase()}`}>{payment.status}</strong></div><div><span>Paid at</span><strong>{formatDateTime(payment.paidAtUtc)}</strong></div><div><span>Reference</span><strong>{payment.transactionReference || "—"}</strong></div><footer>{payment.status === "Completed" ? <><button type="button" onClick={() => { setEditing(payment); setEditDraft(draftFromPayment(payment)); }}>Edit</button><button type="button" onClick={() => void changePaymentStatus(payment, "Cancelled")}>Cancel</button><button type="button" onClick={() => void changePaymentStatus(payment, "Refunded")}>Refund</button></> : <span>Read-only</span>}</footer></article>)}</div>
        {!account.payments.length && <div className="empty-state"><strong>No payments recorded</strong><span>This account remains {account.status.toLowerCase()} until Finance receives money or applies an adjustment.</span></div>}
      </section>
    </div>
    <div className="modal-actions finance-modal-actions">{account.status !== "Cancelled" && <button type="button" className="button danger" disabled={busy} onClick={() => { if (confirm("Cancel this financial account? Completed payments must be cancelled or refunded first, and History will keep the event.")) void apply(() => financeApi.cancel(account.id)); }}>Cancel account</button>}<button type="button" className="button secondary" onClick={onClose}>Done</button></div>
  </section></div>;
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}

function FinanceMetric({ label, value, detail, tone }: { label: string; value: string; detail: string; tone: string }) {
  return <article className={`panel finance-metric finance-tone-${tone}`}><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>;
}

function FinanceAmount({ label, value, currency, strong = false }: { label: string; value: number; currency: string; strong?: boolean }) {
  return <div className={strong ? "is-strong" : ""}><span>{label}</span><strong>{money(value, currency)}</strong></div>;
}

function emptyPayment(balance: number): PaymentDraft {
  return { amount: balance > 0 ? balance.toFixed(2) : "", method: "", transactionReference: "", paidAtUtc: localDateTime(new Date()) };
}

function draftFromPayment(payment: FinancialPayment): PaymentDraft {
  return { amount: payment.amount.toFixed(2), method: payment.method, transactionReference: payment.transactionReference, paidAtUtc: localDateTime(new Date(payment.paidAtUtc)) };
}

function localDateTime(date: Date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

function money(value: number, currency: string) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency, maximumFractionDigits: currency === "KHR" ? 0 : 2 }).format(value);
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date);
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString();
}
