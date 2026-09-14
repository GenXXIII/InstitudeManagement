"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ManagementDataCell } from "@/components/management-data-cell";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { financeApi } from "./finance-api";
import type { StudentPayment } from "./finance-types";

const columns = ["Payment code", "Student", "Enrollment", "Period", "Amount", "Payment", "Timetable", "Due", "Paid at", "Actions"];

export function FinanceWorkspace() {
  const [payments, setPayments] = useState<StudentPayment[]>();
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [selected, setSelected] = useState<StudentPayment>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try { setPayments(await financeApi.get(query, status)); setError(false); }
    catch { setError(true); }
  }, [query, status]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  const view = useMemo(() => {
    const items = payments ?? [];
    return {
      items,
      expected: items.reduce((total, payment) => total + payment.amountDue, 0),
      collected: items.filter(payment => payment.status === "Paid").reduce((total, payment) => total + payment.amountDue, 0),
      pending: items.filter(payment => payment.status === "Pending").length,
      timetableWaiting: items.filter(payment => payment.timetableStatus === "Waiting").length,
      currency: items[0]?.currency ?? "USD",
    };
  }, [payments]);

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!payments) return <LoadingPage/>;

  return <div className="viewport-data-page management-viewport-page finance-viewport-page">
    <PageHeading eyebrow="Semester payment control" title="Finance" description="Generate student payment QR codes and monitor simulated payments. Paid students may advance automatically; pending students are held and receive a payment reminder."/>
    <section className="finance-metrics" aria-label="Finance summary">
      <FinanceMetric label="Expected" value={money(view.expected, view.currency)} detail={`${view.items.length} semester payments`} tone="blue"/>
      <FinanceMetric label="Collected" value={money(view.collected, view.currency)} detail="Confirmed by students" tone="green"/>
      <FinanceMetric label="Pending" value={view.pending.toString()} detail="Held at semester transition" tone="amber"/>
      <FinanceMetric label="Timetable waiting" value={view.timetableWaiting.toString()} detail="No matched timetable enrollment" tone="violet"/>
    </section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search payment, student, or enrollment..." contextLabel="Confirmation policy" contextValue="Student QR scan only">
      <label className="finance-status-filter"><span>Status</span><select value={status} onChange={event => setStatus(event.target.value)}><option>All</option><option>Pending</option><option>Paid</option></select></label>
    </DataTableToolbar>
    <PaginatedDataRegion items={view.items} resetKey={`${query}-${status}`} className="management-paginated-region" empty={<DataTableEmptyState icon={<Icon name="finance" size={24}/>} title="No finance records" description="Student payments appear after Student Enrollment is saved."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table finance-payment-table" headerClassName="horizontal-management-head" rowSelector=":scope > .horizontal-management-row" columns={columns}>
      {pageItems.map(payment => <article className="horizontal-management-row" key={payment.id}>
        <Cell label="Payment code"><strong className="management-code-value">{payment.paymentCode}</strong></Cell>
        <Cell label="Student"><span className="finance-student"><strong>{payment.studentName}</strong><small>{payment.studentCode}</small></span></Cell>
        <Cell label="Enrollment"><span><strong>{payment.enrollmentCode}</strong><small>{payment.department} / Year {payment.yearLevel} / {payment.shift}</small></span></Cell>
        <Cell label="Period"><span><strong>{payment.academicYear}</strong><small>{payment.semester}</small></span></Cell>
        <Cell label="Amount"><strong>{money(payment.amountDue, payment.currency)}</strong></Cell>
        <Cell label="Payment"><span className={`table-status ${payment.status.toLowerCase()}`}>{payment.status}</span></Cell>
        <Cell label="Timetable"><span className={`table-status ${payment.timetableStatus === "Ready" ? "paid" : "pending"}`}>{payment.timetableStatus}</span></Cell>
        <Cell label="Due"><time>{formatDate(payment.dueOn)}</time></Cell>
        <Cell label="Paid at"><span>{payment.paidAtUtc ? <><strong>{formatDate(payment.paidAtUtc)}</strong><small>{payment.confirmationMethod}</small></> : "—"}</span></Cell>
        <Cell label="Actions" className="management-action-cell">{payment.status === "Pending" ? <button type="button" className="button secondary finance-qr-button" onClick={() => setSelected(payment)}>Generate QR code</button> : <span className="table-status paid">Confirmed</span>}</Cell>
      </article>)}
    </DataTable>}</PaginatedDataRegion>
    {selected && <FinanceQrModal payment={selected} onClose={() => setSelected(undefined)}/>} 
  </div>;
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}

function FinanceMetric({ label, value, detail, tone }: { label: string; value: string; detail: string; tone: string }) {
  return <article className={`panel finance-metric finance-tone-${tone}`}><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>;
}

function FinanceQrModal({ payment, onClose }: { payment: StudentPayment; onClose: () => void }) {
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><section className="modal finance-qr-modal" role="dialog" aria-modal="true" aria-label={`Payment QR for ${payment.studentName}`}>
    <div className="modal-head"><div><span className="eyebrow">Simulated payment QR</span><h2>{payment.studentName}</h2><p>{payment.paymentCode} · {payment.academicYear} · {payment.semester}</p></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close"><Icon name="close"/></button></div>
    <div className="finance-qr-code"><QRCodeSVG value={payment.qrPayload} size={230} level="M" marginSize={2}/></div>
    <div className="finance-qr-summary"><span>Amount</span><strong>{money(payment.amountDue, payment.currency)}</strong><small>The student scans this code from the Student Finance tab. The administrator cannot confirm it.</small></div>
    <div className="modal-actions"><button type="button" className="button primary" onClick={onClose}>Done</button></div>
  </section></div>;
}

function money(value: number, currency: string) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency, maximumFractionDigits: currency === "KHR" ? 0 : 2 }).format(value);
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date);
}
