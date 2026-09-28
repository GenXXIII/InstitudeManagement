import { request } from "@/lib/http";
import type { BulkFinanceClosureResult, BulkFinanceDeclarationResult, DeclarationDraft, FinanceClosureReadiness, FinancialAccount, FinanceOptions, MockPaymentQr, PaymentDraft, PaymentStatus } from "./finance-types";

const accountsRoute = "/api/finance/accounts";

function scopeQuery(departmentId = "", year = "") {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  return params;
}

function paymentBody(draft: PaymentDraft) {
  return {
    amount: Number(draft.amount),
    method: draft.method,
    transactionReference: draft.transactionReference,
    paidAtUtc: draft.paidAtUtc ? new Date(draft.paidAtUtc).toISOString() : null,
  };
}

export const financeApi = {
  get: (search = "", status = "All", departmentId = "", year = "") => {
    const params = scopeQuery(departmentId, year);
    params.set("search", search);
    params.set("status", status);
    return request<FinancialAccount[]>(`/api/finance?${params}`);
  },
  getOptions: () => request<FinanceOptions>("/api/finance/options"),
  declareAll: (departmentId = "", year = "") => request<BulkFinanceDeclarationResult>(`/api/finance/declarations?${scopeQuery(departmentId, year)}`, { method: "PUT" }),
  getClosureReadiness: (departmentId = "", year = "") => request<FinanceClosureReadiness>(`/api/finance/close-readiness?${scopeQuery(departmentId, year)}`),
  closeAllPayments: (departmentId = "", year = "") => request<BulkFinanceClosureResult>(`/api/finance/close-payments?${scopeQuery(departmentId, year)}`, { method: "PUT" }),
  declare: (accountId: string, draft: DeclarationDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/declaration`, { method: "PUT", body: JSON.stringify({ ...draft, amount: Number(draft.amount), expiresAtUtc: new Date(draft.expiresAtUtc).toISOString() }) }),
  extendExpiry: (accountId: string, days: number, reason: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/expiry-extension`, { method: "PUT", body: JSON.stringify({ days, reason }) }),
  regenerateQr: (accountId: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/qr`, { method: "PUT" }),
  generateMockQr: (accountId: string) => request<MockPaymentQr>(`${accountsRoute}/${accountId}/mock-qr`, { method: "PUT" }),
  recordPayment: (accountId: string, draft: PaymentDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments`, { method: "POST", body: JSON.stringify(paymentBody(draft)) }),
  updatePayment: (accountId: string, paymentId: string, draft: PaymentDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments/${paymentId}`, { method: "PUT", body: JSON.stringify(paymentBody(draft)) }),
  setPaymentStatus: (accountId: string, paymentId: string, status: Exclude<PaymentStatus, "Completed">) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments/${paymentId}/status`, { method: "PUT", body: JSON.stringify({ status }) }),
  adjust: (accountId: string, amount: number, reason: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/adjustment`, { method: "PUT", body: JSON.stringify({ amount, reason }) }),
  cancel: (accountId: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/cancel`, { method: "PUT" }),
};
