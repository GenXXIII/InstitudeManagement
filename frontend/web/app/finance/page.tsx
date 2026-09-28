import { Suspense } from "react";
import { FinanceWorkspace } from "@/features/finance/finance-workspace";

export default function FinancePage() {
  return <Suspense fallback={null}><FinanceWorkspace/></Suspense>;
}
