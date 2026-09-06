import { Suspense } from "react";
import { LoadingPage } from "@/components/page-primitives";
import { GlobalSearchResultsPage } from "@/features/search/global-search-results-page";

export default function SearchPage() {
  return <Suspense fallback={<LoadingPage/>}><GlobalSearchResultsPage/></Suspense>;
}
