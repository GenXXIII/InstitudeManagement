"use client";

import { useMemo } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import type { EnrollmentResource } from "./common/enrollment-types";
import { EnrollmentRow } from "./components/enrollment-row";
import { EnrollmentEditor } from "./enrollment-editor";
import {
  buildEnrollmentDisplayItems,
  enrollmentCopy,
  isEditableEnrollment,
  sortEnrollmentItems,
} from "./enrollment-workspace-model";
import { useEnrollmentWorkspace } from "./use-enrollment-workspace";

export function EnrollmentWorkspace({ resource }: { resource: EnrollmentResource }) {
  const {
    actionError,
    candidates,
    classrooms,
    courses,
    departmentId,
    departments,
    editing,
    error,
    items,
    load,
    query,
    ready,
    remove,
    setActionError,
    setEditing,
    setError,
    setQuery,
    teachers,
    year,
  } = useEnrollmentWorkspace(resource);

  const displayItems = useMemo(() => buildEnrollmentDisplayItems(items, resource), [items, resource]);
  const sortedItems = useMemo(() => sortEnrollmentItems(displayItems, resource), [displayItems, resource]);
  const pagination = useDataPagination(sortedItems, `${resource}-enrollment-${departmentId}-${year}-${query}`);
  const details = enrollmentCopy[resource];
  const selectedDepartment = departments.find(department => department.id === departmentId)?.values.name ?? "All departments";

  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page management-viewport-page enrollment-viewport-page">
    <PageHeading
      eyebrow="Academic enrollment service"
      title={details.title}
      description={details.description}
      actions={resource === "students" || resource === "timetable" ? <button type="button" className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>{resource === "timetable" ? "Add timetable" : "Add student enrollment"}</button> : undefined}
    />
    <section className="management-toolbar panel management-toolbar-global">
      <label className="management-search module-search-field"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${resource}...`} aria-label={`Search ${resource}`}/></label>
      <div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` - Year ${year}` : " - All years"}</strong></div>
    </section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Enrollment relationship protected</strong><span>{actionError}</span></div><button type="button" onClick={() => setActionError("")}>Dismiss</button></section>}
    <section className="management-paginated-region">
      <section className={`panel horizontal-management-table enrollment-service-horizontal enrollment-${resource}`}>
        <div className="horizontal-management-head">{details.columns.map(column => <span key={column}>{column}</span>)}</div>
        {pagination.pageItems.map(item => <EnrollmentRow resource={resource} item={item} onEdit={isEditableEnrollment(resource) ? () => setEditing(item) : undefined} onRemove={isEditableEnrollment(resource) ? () => { void remove(item); } : undefined} key={item.rowKey}/>)}
      </section>
      <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={sortedItems.length} onPage={pagination.setPage}/>
    </section>
    {editing !== undefined && (resource === "students" || resource === "timetable") && <EnrollmentEditor
      resource={resource}
      item={editing}
      candidates={candidates}
      departments={departments}
      teachers={teachers}
      courses={courses}
      classrooms={classrooms}
      scopeDepartmentId={departmentId}
      scopeYear={year}
      onClose={() => setEditing(undefined)}
      onSaved={() => { setEditing(undefined); void load(); }}
    />}
  </div>;
}
