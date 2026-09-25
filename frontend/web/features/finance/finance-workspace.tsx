"use client";

import Image from "next/image";
import { useCallback, useEffect, useMemo, useState } from "react";
import QRCode from "qrcode";
import { DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import { compareFinanceAccounts, FinanceTable } from "./finance-table";
import type { DeclarationDraft, FinanceClosureReadiness, FinancialAccount, FinancialPayment, FinanceOptions, PaymentDraft, PaymentStatus } from "./finance-types";

const statuses = ["All", "Pending", "Partial", "Paid", "Closed", "Cancelled", "Refunded"];
type PaymentQrPreview = { kind: "Bakong" | "Mock"; payload: string; publicId: string; expiresAtUtc: string };

export function FinanceWorkspace() {
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
      const [nextAccounts, nextOptions, nextClosure] = await Promise.all([financeApi.get(query, status), financeApi.getOptions(), financeApi.getClosureReadiness()]);
      setAccounts(nextAccounts);
      setOptions(nextOptions);
      setClosure(nextClosure);
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
    if (!window.confirm("Issue payment notices to all active students in the current academic period? Existing unpaid notices will use the current configured price and receive a fresh expiry countdown.")) return;
    setBulkDeclaring(true);
    setBulkNotice(undefined);
    try {
      const result = await financeApi.declareAll();
      await load();
      setBulkNotice({
        message: result.declaredCount > 0
          ? `Payment notices issued to ${result.declaredCount} ${result.declaredCount === 1 ? "student" : "students"}. The expiry countdown started now and ends ${formatDateTime(result.expiresAtUtc)}.`
          : "Every active student in the current academic period already has a payment notice.",
        error: false,
      });
    } catch (reason) {
      setBulkNotice({ message: reason instanceof Error ? reason.message : "Could not issue payment notices.", error: true });
    } finally {
      setBulkDeclaring(false);
    }
  }

  async function closeAllPayments() {
    if (!closure?.canCloseAll || !window.confirm(`Finalize all ${closure.totalAccounts} current student payments? Every account will become final and read-only together.`)) return;
    setClosingAll(true);
    setBulkNotice(undefined);
    try {
      const result = await financeApi.closeAllPayments();
      await load();
      setBulkNotice({ message: `${result.closedCount} student payment${result.closedCount === 1 ? "" : "s"} finalized together. They stay current until Semester Results are released and the semester ends.`, error: false });
    } catch (reason) {
      setBulkNotice({ message: reason instanceof Error ? reason.message : "Could not finalize semester payments.", error: true });
    } finally {
      setClosingAll(false);
    }
  }

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!accounts || !options || !closure) return <LoadingPage/>;
  const selected = accounts.find(account => account.id === selectedId);

  return <div className="viewport-data-page management-viewport-page finance-viewport-page">
    <PageHeading eyebrow="Current financial state" title="Finance" description="Finance owns enrollment-linked fees, received payments, balances, adjustments, and financial eligibility. Audit events stay in History." actions={<div className="management-actions finance-heading-actions"><button type="button" className="button secondary" disabled={bulkDeclaring || closingAll} onClick={() => void declareAll()}>{bulkDeclaring ? "Issuing…" : "Issue Payment Notices"}</button><button type="button" className="button primary" disabled={!closure.canCloseAll || closingAll || bulkDeclaring} onClick={() => void closeAllPayments()}>{closingAll ? "Finalizing…" : closure.openPaidAccounts === 0 && closure.totalAccounts > 0 && closure.paidAccounts === closure.totalAccounts ? "Semester Payments Finalized" : "Finalize Semester Payments"}</button></div>}/>
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
    <PaginatedDataRegion items={view.items} resetKey={`${query}-${status}`} className="management-paginated-region" empty={<DataTableEmptyState icon={<Icon name="finance" size={24}/>} title="No active finance accounts" description="Payments archive only after all payments are finalized, Semester Results are released, and the semester ends."/>}>{pageItems => <FinanceTable accounts={pageItems} onSelect={account => setSelectedId(account.id)}/>}</PaginatedDataRegion>
    {selected && <FinanceAccountModal
      account={selected}
      options={options}
      onClose={() => setSelectedId("")}
      onUpdated={updated => { if (updated.closedAtUtc && updated.periodState === "Retained") { setAccounts(current => current?.filter(account => account.id !== updated.id)); setSelectedId(""); } else setAccounts(current => current?.map(account => account.id === updated.id ? updated : account)); }}
    />}
  </div>;
}

function FinanceAccountModal({ account: initialAccount, options, onClose, onUpdated }: { account: FinancialAccount; options: FinanceOptions; onClose: () => void; onUpdated: (account: FinancialAccount) => void }) {
  const [account, setAccount] = useState(initialAccount);
  const [declarationDraft, setDeclarationDraft] = useState<DeclarationDraft>(() => declarationFromAccount(initialAccount, options.paymentDueDays));
  const [paymentDraft, setPaymentDraft] = useState<PaymentDraft>(() => emptyPayment(initialAccount.balance));
  const [adjustmentAmount, setAdjustmentAmount] = useState(initialAccount.adjustmentAmount.toString());
  const [adjustmentReason, setAdjustmentReason] = useState(initialAccount.adjustmentReason);
  const [extensionDays, setExtensionDays] = useState("");
  const [extensionReason, setExtensionReason] = useState("");
  const [editing, setEditing] = useState<FinancialPayment>();
  const [editDraft, setEditDraft] = useState<PaymentDraft>();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [qrPreview, setQrPreview] = useState<PaymentQrPreview>();

  async function apply(action: () => Promise<FinancialAccount>) {
    setBusy(true);
    setMessage("");
    try {
      const updated = await action();
      setAccount(updated);
      onUpdated(updated);
      setDeclarationDraft(declarationFromAccount(updated, options.paymentDueDays));
      setPaymentDraft(emptyPayment(updated.balance));
      setAdjustmentAmount(updated.adjustmentAmount.toString());
      setAdjustmentReason(updated.adjustmentReason);
      setEditing(undefined);
      setEditDraft(undefined);
      setQrPreview(undefined);
      return updated;
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : "Could not apply the finance change.");
      return undefined;
    } finally {
      setBusy(false);
    }
  }

  async function recordPayment(event: React.FormEvent) {
    event.preventDefault();
    await apply(() => financeApi.recordPayment(account.id, paymentDraft));
  }

  async function saveDeclaration(event: React.FormEvent) {
    event.preventDefault();
    await apply(() => financeApi.declare(account.id, declarationDraft));
  }

  async function extendExpiry(event: React.FormEvent) {
    event.preventDefault();
    if (await apply(() => financeApi.extendExpiry(account.id, Number(extensionDays), extensionReason))) {
      setExtensionDays("");
      setExtensionReason("");
    }
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

  async function generateBakongQr() {
    const updated = await apply(() => financeApi.regenerateQr(account.id));
    if (updated?.qrPayload) setQrPreview({ kind: "Bakong", payload: updated.qrPayload, publicId: updated.publicId, expiresAtUtc: updated.qrExpiresAtUtc ?? "" });
  }

  async function generateMockQr() {
    setBusy(true);
    setMessage("");
    try {
      const generated = await financeApi.generateMockQr(account.id);
      setQrPreview({ kind: "Mock", payload: generated.qrPayload, publicId: generated.publicId, expiresAtUtc: generated.expiresAtUtc });
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : "Could not generate the mock payment QR.");
    } finally {
      setBusy(false);
    }
  }

  const readOnly = account.status === "Paid" || Boolean(account.closedAtUtc);

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><section className="modal finance-account-modal" role="dialog" aria-modal="true" aria-label={`Finance account for ${account.studentName}`}>
    <div className="modal-head"><div><span className="eyebrow">{readOnly ? "Financial account · View only" : "Financial account"}</span><h2>{account.studentName}</h2><p>{account.financialAccountCode} · {account.enrollmentCode} · {account.academicYear} · {account.semester}</p></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close"><Icon name="close"/></button></div>
    <div className="finance-account-scroll">
      {message && <div className="management-rule-error" role="alert"><Icon name="finance" size={16}/><div><strong>Could not apply change</strong><span>{message}</span></div><button type="button" onClick={() => setMessage("")}>Dismiss</button></div>}
      {!readOnly && <form className="finance-operation-card finance-declaration-card" onSubmit={saveDeclaration}>
        <header><div><strong>{account.isDeclared ? "Student payment declaration" : "Declare student payment"}</strong><span>Prices and the initial expiry countdown come from Finance Settings. Existing expiry dates never renew automatically.</span></div></header>
        <div className="finance-semester-plan"><Icon name="calendar" size={18}/><div><strong>{account.semester} payment</strong><span>One declaration covers only this semester.</span></div></div>
        <div className="finance-form-grid">
          <label><span>Card title</span><input value={declarationDraft.title} minLength={3} maxLength={160} onChange={event => setDeclarationDraft({ ...declarationDraft, title: event.target.value })} placeholder="Semester tuition payment" required/></label>
          <label><span>Amount to pay</span><input type="number" min="0.01" step="0.01" value={declarationDraft.amount} onChange={event => setDeclarationDraft({ ...declarationDraft, amount: event.target.value })} required/></label>
          <label><span>Due date</span><input type="date" value={declarationDraft.dueOn} readOnly={account.isDeclared} onChange={event => setDeclarationDraft({ ...declarationDraft, dueOn: event.target.value })} required/></label>
          <label><span>Payment expires</span><input type="datetime-local" value={declarationDraft.expiresAtUtc} readOnly={account.isDeclared} onChange={event => setDeclarationDraft({ ...declarationDraft, expiresAtUtc: event.target.value })} required/></label>
        </div>
        <footer><small>{account.isDeclared && account.declaredAtUtc ? `Created ${formatDateTime(account.declaredAtUtc)} · Changes are recorded in History.` : `Suggested from Settings: ${money(account.tuitionFee + account.otherFee, account.currency)}.`}</small><button className="button primary" disabled={busy}>{busy ? "Saving..." : account.isDeclared ? "Save declaration" : "Declare payment"}</button></footer>
      </form>}

      {account.isDeclared && !account.isExpired && account.status !== "Paid" && account.status !== "Cancelled" && <form className="finance-operation-card finance-expiry-extension" onSubmit={extendExpiry}>
        <header><div><strong>Additional expiry days</strong><span>Add days only when the student has an approved reason. This action is recorded in History.</span></div></header>
        <div className="finance-form-grid finance-extension-grid"><label><span>Additional days</span><input type="number" min="1" max="365" value={extensionDays} onChange={event => setExtensionDays(event.target.value)} required/></label><label><span>Reason</span><input minLength={3} maxLength={500} value={extensionReason} onChange={event => setExtensionReason(event.target.value)} placeholder="Student request and approved reason" required/></label></div>
        <footer><small>Days cannot be added after the payment expires.</small><button className="button secondary" disabled={busy}>{busy ? "Saving..." : "Add expiry days"}</button></footer>
      </form>}
      {account.isDeclared && account.isExpired && account.status !== "Paid" && account.status !== "Cancelled" && <div className="finance-expiry-locked"><Icon name="finance" size={16}/><span>This payment has expired. Additional days can no longer be assigned; the daily punishment continues until payment is recorded.</span></div>}

      <section className="finance-account-summary" aria-label="Financial account balance">
        <FinanceAmount label={account.isDeclared ? "Declared amount" : "Suggested amount"} value={account.declaredAmount ?? account.tuitionFee + account.otherFee} currency={account.currency}/>
        <FinanceAmount label="Adjustment" value={account.adjustmentAmount} currency={account.currency}/>
        <FinanceAmount label={`Late punishment (${account.latePenaltyDays}d)`} value={account.latePenaltyAmount} currency={account.currency}/>
        <FinanceAmount label="Total due" value={account.totalDue} currency={account.currency}/>
        <FinanceAmount label="Paid" value={account.totalPaid} currency={account.currency}/>
        <FinanceAmount label="Balance" value={account.balance} currency={account.currency} strong/>
      </section>

      <section className="finance-current-state"><div><span>Payment status</span><strong className={`finance-state-${account.closedAtUtc ? "closed" : account.isExpired ? "expired" : account.status.toLowerCase()}`}>{account.closedAtUtc ? "Paid · Closed" : account.isExpired ? "Expired" : account.isDeclared ? account.status : "Draft"}</strong></div><div><span>Enrollment eligibility</span><strong>{options.requirePaidForAdvancement ? account.closedAtUtc ? account.periodState === "Current" ? "Waiting for result publication and semester end" : "Enrollment increased" : account.status === "Paid" ? "Waiting for every student to pay" : "Stay in current enrollment" : "Payment gate disabled"}</strong></div><div><span>Payment lifecycle</span><strong>{account.closedAtUtc ? `Read-only · closed ${formatDateTime(account.closedAtUtc)}` : account.status === "Paid" ? "Ready for bulk closure" : "Open"}</strong></div></section>
      {account.canClosePayment && <section className="finance-close-payment is-ready"><div><Icon name="finance" size={18}/><span><strong>Paid and ready</strong><small>This account will be finalized together with every current student payment from the Finalize Semester Payments action at the top of this page.</small></span></div></section>}
      {account.closedAtUtc && <section className="finance-close-payment is-closed"><div><Icon name="finance" size={18}/><span><strong>Payment finalized · read-only</strong><small>{account.periodState === "Current" ? "This account stays in Finance until all Semester Results are released and the semester ends." : "Payment finalization, Semester Result release, and semester end are complete; this account belongs in Finance History."}</small></span></div></section>}

      {account.isDeclared && !readOnly && account.status !== "Cancelled" && account.balance > 0 && <section className="finance-operation-card finance-qr-card">
        <header><div><strong>Student payment QR</strong><span>Generate a QR for this student only. Bakong QR requires real bank confirmation; Mock QR is accepted only by the student signed in with the matching Public ID.</span></div></header>
        <div className="finance-qr-identity"><span>Student Public ID</span><strong>{account.publicId || "Not assigned"}</strong></div>
        <div className="finance-qr-actions"><button type="button" className="button secondary" disabled={busy || !options.bakongEnabled || !options.bakongConfigured} onClick={() => void generateBakongQr()}>{busy ? "Generating…" : "Generate Bakong QR"}</button><button type="button" className="button primary" disabled={busy || !options.mockPaymentEnabled || !account.publicId} onClick={() => void generateMockQr()}>{busy ? "Generating…" : "Generate Mock QR"}</button></div>
        {!options.bakongEnabled || !options.bakongConfigured ? <small>Bakong Pay requires the Bakong receiver and API token configuration.</small> : null}
        {!options.mockPaymentEnabled ? <small>Enable Mock Scan QR Pay in Finance Settings to issue test QRs.</small> : null}
        {qrPreview && <PaymentQrCode preview={qrPreview} amount={account.balance} currency={account.currency}/>}
      </section>}

      {account.isDeclared && !readOnly && account.status !== "Cancelled" && account.balance > 0 && <form className="finance-operation-card" onSubmit={recordPayment}>
        <header><div><strong>Record payment</strong><span>Capture actual money received. The balance and status are calculated by Finance.</span></div></header>
        <div className="finance-form-grid">
          <label><span>Amount</span><input type="number" min="0.01" step="0.01" value={paymentDraft.amount} onChange={event => setPaymentDraft({ ...paymentDraft, amount: event.target.value })} required/></label>
          <label><span>Payment method</span><select value={paymentDraft.method} onChange={event => setPaymentDraft({ ...paymentDraft, method: event.target.value })} required><option value="">Select payment method</option>{options.paymentMethods.map(method => <option key={method}>{method}</option>)}</select></label>
          <label><span>Transaction reference</span><input value={paymentDraft.transactionReference} onChange={event => setPaymentDraft({ ...paymentDraft, transactionReference: event.target.value })} placeholder="Optional bank or receipt reference"/></label>
          <label><span>Paid at</span><input type="datetime-local" value={paymentDraft.paidAtUtc} onChange={event => setPaymentDraft({ ...paymentDraft, paidAtUtc: event.target.value })}/></label>
        </div>
        <footer><small>{options.allowPartialPayments ? "Partial payments are allowed." : "Payment must clear the full balance."} {options.allowOverpayment ? "Overpayment is allowed." : "Overpayment is blocked."}</small><button className="button primary" disabled={busy}>{busy ? "Saving..." : "Record payment"}</button></footer>
      </form>}

      {account.isDeclared && !readOnly && <form className="finance-operation-card" onSubmit={event => { event.preventDefault(); void apply(() => financeApi.adjust(account.id, Number(adjustmentAmount), adjustmentReason)); }}>
        <header><div><strong>Discount / adjustment</strong><span>Use a negative amount for a discount or a positive amount for an extra charge.</span></div></header>
        <div className="finance-form-grid finance-adjustment-grid"><label><span>Adjustment amount</span><input type="number" step="0.01" value={adjustmentAmount} onChange={event => setAdjustmentAmount(event.target.value)} required/></label><label><span>Reason</span><input value={adjustmentReason} onChange={event => setAdjustmentReason(event.target.value)} placeholder="Required when amount is not zero"/></label></div>
        <footer><small>Maximum absolute adjustment: {money(options.maximumAdjustmentAmount, account.currency)}.</small><button className="button secondary" disabled={busy}>{busy ? "Saving..." : "Apply adjustment"}</button></footer>
      </form>}

      <section className="finance-transactions">
        <header><div><strong>Payments</strong><span>Current payment transactions. Corrections and status changes are written to History.</span></div><b>{account.payments.length}</b></header>
        {editing && editDraft && <form className="finance-payment-edit" onSubmit={updatePayment}><strong>Edit {editing.paymentCode}</strong><div className="finance-form-grid"><label><span>Amount</span><input type="number" min="0.01" step="0.01" value={editDraft.amount} onChange={event => setEditDraft({ ...editDraft, amount: event.target.value })} required/></label><label><span>Payment method</span><select value={editDraft.method} onChange={event => setEditDraft({ ...editDraft, method: event.target.value })} required><option value="">Select payment method</option>{options.paymentMethods.map(method => <option key={method}>{method}</option>)}</select></label><label><span>Transaction reference</span><input value={editDraft.transactionReference} onChange={event => setEditDraft({ ...editDraft, transactionReference: event.target.value })}/></label><label><span>Paid at</span><input type="datetime-local" value={editDraft.paidAtUtc} onChange={event => setEditDraft({ ...editDraft, paidAtUtc: event.target.value })}/></label></div><footer><button type="button" className="button secondary" onClick={() => { setEditing(undefined); setEditDraft(undefined); }}>Cancel edit</button><button className="button primary" disabled={busy}>{busy ? "Saving..." : "Save correction"}</button></footer></form>}
        <div className="finance-transaction-list">{account.payments.map(payment => <article key={payment.id}><div><span>Payment ID</span><strong>{payment.paymentCode}</strong></div><div><span>Amount</span><strong>{money(payment.amount, account.currency)}</strong></div><div><span>Method</span><strong>{payment.method}</strong></div><div><span>Status</span><strong className={`finance-payment-state-${payment.status.toLowerCase()}`}>{payment.status}</strong></div><div><span>Paid at</span><strong>{formatDateTime(payment.paidAtUtc)}</strong></div><div><span>Reference</span><strong>{payment.transactionReference || "—"}</strong></div><footer>{payment.status === "Completed" && !readOnly ? <><button type="button" onClick={() => { setEditing(payment); setEditDraft(draftFromPayment(payment)); }}>Edit</button><button type="button" onClick={() => void changePaymentStatus(payment, "Cancelled")}>Cancel</button><button type="button" onClick={() => void changePaymentStatus(payment, "Refunded")}>Refund</button></> : <span>Read-only</span>}</footer></article>)}</div>
        {!account.payments.length && <div className="empty-state"><strong>No payments recorded</strong><span>This account remains {account.status.toLowerCase()} until Finance receives money or applies an adjustment.</span></div>}
      </section>
    </div>
    <div className="modal-actions finance-modal-actions">{!account.closedAtUtc && account.status !== "Cancelled" && account.status !== "Paid" && <button type="button" className="button danger" disabled={busy} onClick={() => { if (confirm("Cancel this financial account? Completed payments must be cancelled or refunded first, and History will keep the event.")) void apply(() => financeApi.cancel(account.id)); }}>Cancel account</button>}<button type="button" className="button secondary" onClick={onClose}>Done</button></div>
  </section></div>;
}

function PaymentQrCode({ preview, amount, currency }: { preview: PaymentQrPreview; amount: number; currency: string }) {
  const [imageUrl, setImageUrl] = useState("");
  useEffect(() => {
    let active = true;
    void QRCode.toDataURL(preview.payload, { width: 240, margin: 2, errorCorrectionLevel: "M" })
      .then(value => { if (active) setImageUrl(value); });
    return () => { active = false; };
  }, [preview.payload]);

  return <div className="finance-generated-qr">
    <div>{imageUrl ? <Image src={imageUrl} width={220} height={220} unoptimized alt={`${preview.kind} payment QR for ${preview.publicId}`}/> : <span>Preparing QR…</span>}</div>
    <section><span>{preview.kind === "Mock" ? "Mock Scan QR Pay" : "Bakong Pay"}</span><strong>{money(amount, currency)}</strong><small>Public ID: {preview.publicId}</small><small>Expires: {formatDateTime(preview.expiresAtUtc)}</small></section>
  </div>;
}

function FinanceMetric({ label, value, tone }: { label: string; value: string; tone: string }) {
  return <article className={`panel finance-metric finance-tone-${tone}`}><span>{label}</span><strong>{value}</strong></article>;
}

function FinanceAmount({ label, value, currency, strong = false }: { label: string; value: number | string; currency?: string; strong?: boolean }) {
  return <div className={strong ? "is-strong" : ""}><span>{label}</span><strong>{typeof value === "number" && currency ? money(value, currency) : value}</strong></div>;
}

function declarationFromAccount(account: FinancialAccount, paymentDueDays: number): DeclarationDraft {
  const deadline = declarationDeadline(paymentDueDays);
  const expiry = account.expiresAtUtc ? new Date(account.expiresAtUtc) : deadline.expiry;
  return {
    title: account.title || `${account.semester} payment`,
    paymentPlan: "Semester",
    amount: (account.declaredAmount ?? account.tuitionFee + account.otherFee).toFixed(2),
    dueOn: account.isDeclared ? account.dueOn : deadline.dueOn,
    expiresAtUtc: localDateTime(expiry),
  };
}

function declarationDeadline(paymentDueDays: number) {
  const due = new Date();
  due.setDate(due.getDate() + paymentDueDays);
  const expiry = new Date(due);
  expiry.setHours(23, 59, 59, 999);
  return { dueOn: localDate(due), expiry };
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

function localDate(date: Date) {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}

function money(value: number, currency: string) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency, maximumFractionDigits: currency === "KHR" ? 0 : 2 }).format(value);
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString();
}
