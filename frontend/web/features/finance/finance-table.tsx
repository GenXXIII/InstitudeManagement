"use client";

import { DataTable } from "@/components/data-table";
import { ManagementDataCell } from "@/components/management-data-cell";
import type { FinancialAccount } from "./finance-types";

const activeColumns = ["Account", "Student", "Department", "Year", "Semester", "Shift", "Amount", "Paid", "Balance", "Status", "Due", "Actions"];
const historyColumns = ["Account", "Student", "Department", "Year", "Semester", "Shift", "Amount", "Paid", "Balance", "Status", "Due", "Closed at"];

export function FinanceTable({ accounts, history = false, onSelect }: { accounts: FinancialAccount[]; history?: boolean; onSelect?: (account: FinancialAccount) => void }) {
  return <DataTable as="section" className="panel horizontal-management-table finance-payment-table" headerClassName="horizontal-management-head" rowSelector=":scope > .horizontal-management-row" columns={history ? historyColumns : activeColumns}>
    {accounts.map(account => <article className="horizontal-management-row" key={account.id}>
      <Cell label="Account"><strong className="management-code-value">{account.financialAccountCode}</strong></Cell>
      <Cell label="Student"><strong>{account.studentName}</strong></Cell>
      <Cell label="Department"><strong>{account.department}</strong></Cell>
      <Cell label="Year"><strong>Year {account.yearLevel}</strong></Cell>
      <Cell label="Semester"><strong>{account.semester}</strong></Cell>
      <Cell label="Shift"><strong>{account.shift}</strong></Cell>
      <Cell label="Amount"><strong>{account.isDeclared ? money(account.totalDue, account.currency) : "—"}</strong></Cell>
      <Cell label="Paid"><strong>{money(account.totalPaid, account.currency)}</strong></Cell>
      <Cell label="Balance"><strong>{money(account.balance, account.currency)}</strong></Cell>
      <Cell label="Status"><span className={`table-status finance-state-${account.closedAtUtc ? "closed" : account.isExpired ? "expired" : account.status.toLowerCase()}`}>{account.closedAtUtc ? "Paid · Closed" : account.isExpired ? "Expired" : account.isDeclared ? account.status : "Draft"}</span></Cell>
      <Cell label="Due"><time>{account.isDeclared ? formatDate(account.dueOn) : "—"}</time></Cell>
      {history ? <Cell label="Closed at"><time>{account.closedAtUtc ? formatDate(account.closedAtUtc) : "—"}</time></Cell> : <Cell label="Actions" className="management-action-cell"><button type="button" className="button secondary finance-manage-button" onClick={() => onSelect?.(account)}>{account.closedAtUtc ? "View" : account.isDeclared ? "Modify" : "Declare"}</button></Cell>}
    </article>)}
  </DataTable>;
}

export function compareFinanceAccounts(left: FinancialAccount, right: FinancialAccount) {
  return left.yearLevel - right.yearLevel
    || semesterNumber(left.semester) - semesterNumber(right.semester)
    || shiftNumber(left.shift) - shiftNumber(right.shift)
    || left.studentName.localeCompare(right.studentName, undefined, { numeric: true, sensitivity: "base" });
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 99); }
function shiftNumber(value: string) { return ["morning", "afternoon", "evening", "weekend"].indexOf(value.toLowerCase()) === -1 ? 99 : ["morning", "afternoon", "evening", "weekend"].indexOf(value.toLowerCase()); }
function money(value: number, currency: string) { return new Intl.NumberFormat("en-US", { style: "currency", currency, maximumFractionDigits: currency === "KHR" ? 0 : 2 }).format(value); }
function formatDate(value: string) { const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date); }
