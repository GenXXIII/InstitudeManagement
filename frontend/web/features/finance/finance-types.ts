export type FinanceStatus = "Pending" | "Partial" | "Paid" | "Cancelled" | "Refunded";
export type PaymentStatus = "Completed" | "Cancelled" | "Refunded";

export type FinancialPayment = {
  id: string;
  paymentCode: string;
  amount: number;
  method: string;
  status: PaymentStatus;
  transactionReference: string;
  paidAtUtc: string;
  createAt: string;
};

export type FinancialAccount = {
  id: string;
  financialAccountCode: string;
  paymentCode: string;
  studentId: string;
  studentCode: string;
  publicId: string;
  studentName: string;
  department: string;
  enrollmentCode: string;
  yearLevel: number;
  shift: string;
  academicYear: string;
  semester: string;
  tuitionFee: number;
  otherFee: number;
  adjustmentAmount: number;
  adjustmentReason: string;
  totalDue: number;
  totalPaid: number;
  balance: number;
  amountDue: number;
  currency: string;
  dueOn: string;
  status: FinanceStatus;
  confirmationMethod: string;
  paidAtUtc: string | null;
  reminderSentAtUtc: string | null;
  reminderReadAtUtc: string | null;
  timetableStatus: "Ready" | "Waiting";
  periodState: "Current" | "Retained";
  qrPayload: string;
  payments: FinancialPayment[];
  createAt: string;
};

export type FinanceOptions = {
  paymentMethods: string[];
  allowPartialPayments: boolean;
  allowOverpayment: boolean;
  maximumAdjustmentAmount: number;
  requirePaidForAdvancement: boolean;
};

export type PaymentDraft = {
  amount: string;
  method: string;
  transactionReference: string;
  paidAtUtc: string;
};
