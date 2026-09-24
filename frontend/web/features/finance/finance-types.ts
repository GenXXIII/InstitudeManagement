export type FinanceStatus = "Pending" | "Partial" | "Paid" | "Cancelled" | "Refunded";
export type PaymentStatus = "Completed" | "Cancelled" | "Refunded";
export type PaymentPlan = "Semester" | "Year";

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
  title: string;
  paymentPlan: PaymentPlan;
  declaredAmount: number | null;
  declaredAtUtc: string | null;
  expiresAtUtc: string | null;
  isDeclared: boolean;
  isExpired: boolean;
  qrGeneratedAtUtc: string | null;
  qrExpiresAtUtc: string | null;
  isQrExpired: boolean;
  tuitionFee: number;
  otherFee: number;
  adjustmentAmount: number;
  adjustmentReason: string;
  latePenaltyDays: number;
  latePenaltyAmount: number;
  totalDue: number;
  totalPaid: number;
  balance: number;
  amountDue: number;
  currency: string;
  dueOn: string;
  status: FinanceStatus;
  confirmationMethod: string;
  paidAtUtc: string | null;
  closedAtUtc: string | null;
  canClosePayment: boolean;
  reminderSentAtUtc: string | null;
  reminderReadAtUtc: string | null;
  timetableStatus: "Ready" | "Waiting";
  periodState: "Current" | "Retained";
  qrProvider: "Bakong KHQR";
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
  paymentDueDays: number;
  semesterPrice: number;
  otherFee: number;
  paymentProviders: BankPaymentOption[];
  bakongEnabled: boolean;
  bakongConfigured: boolean;
  bakongEnvironment: "SIT" | "Production";
  dynamicQrBank: string;
  dynamicQrAccountName: string;
  dynamicQrAccountCode: string;
};

export type BankPaymentOption = {
  name: "ABA" | "ACLEDA";
  accountName: string;
  accountCode: string;
};

export type BulkFinanceDeclarationResult = {
  declaredCount: number;
  announcedAtUtc: string;
  dueOn: string;
  expiresAtUtc: string;
};

export type PaymentDraft = {
  amount: string;
  method: string;
  transactionReference: string;
  paidAtUtc: string;
};

export type DeclarationDraft = {
  title: string;
  paymentPlan: PaymentPlan;
  amount: string;
  dueOn: string;
  expiresAtUtc: string;
};
