import { request } from "@/lib/http";
import type { BulkFinanceDeclarationResult, DeclarationDraft, FinancialAccount, FinanceOptions, PaymentDraft, PaymentStatus } from "./finance-types";

const accountsRoute = "/api/finance/accounts";

function paymentBody(draft: PaymentDraft) {
  return {
    amount: Number(draft.amount),
    method: draft.method,
    transactionReference: draft.transactionReference,
    paidAtUtc: draft.paidAtUtc ? new Date(draft.paidAtUtc).toISOString() : null,
  };
}

export const financeApi = {
  get: (search = "", status = "All") => request<FinancialAccount[]>(`/api/finance?search=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}`),
  getOptions: () => request<FinanceOptions>("/api/finance/options"),
  declareAll: () => request<BulkFinanceDeclarationResult>("/api/finance/declarations", { method: "PUT" }),
  declare: (accountId: string, draft: DeclarationDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/declaration`, { method: "PUT", body: JSON.stringify({ ...draft, amount: Number(draft.amount), expiresAtUtc: new Date(draft.expiresAtUtc).toISOString() }) }),
  extendExpiry: (accountId: string, days: number, reason: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/expiry-extension`, { method: "PUT", body: JSON.stringify({ days, reason }) }),
  regenerateQr: (accountId: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/qr`, { method: "PUT" }),
  recordPayment: (accountId: string, draft: PaymentDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments`, { method: "POST", body: JSON.stringify(paymentBody(draft)) }),
  updatePayment: (accountId: string, paymentId: string, draft: PaymentDraft) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments/${paymentId}`, { method: "PUT", body: JSON.stringify(paymentBody(draft)) }),
  setPaymentStatus: (accountId: string, paymentId: string, status: Exclude<PaymentStatus, "Completed">) => request<FinancialAccount>(`${accountsRoute}/${accountId}/payments/${paymentId}/status`, { method: "PUT", body: JSON.stringify({ status }) }),
  adjust: (accountId: string, amount: number, reason: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/adjustment`, { method: "PUT", body: JSON.stringify({ amount, reason }) }),
  cancel: (accountId: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/cancel`, { method: "PUT" }),
  closePayment: (accountId: string) => request<FinancialAccount>(`${accountsRoute}/${accountId}/close-payment`, { method: "PUT" }),
};
