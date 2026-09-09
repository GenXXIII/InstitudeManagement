"use client";

import { useMemo } from "react";
import { DataTable, DataTableToolbar, PaginatedDataRegion, type DataTableColumn } from "@/components/data-table";
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
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder={`Search ${resource}...`} searchAriaLabel={`Search ${resource}`} contextLabel="Enrollment scope" contextValue={`${selectedDepartment}${year ? ` - Year ${year}` : " - All years"}`}/>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Enrollment relationship protected</strong><span>{actionError}</span></div><button type="button" onClick={() => setActionError("")}>Dismiss</button></section>}
    <PaginatedDataRegion items={sortedItems} resetKey={`${resource}-enrollment-${departmentId}-${year}-${query}`} className="management-paginated-region">{pageItems => <>
      <DataTable as="section" className={`panel horizontal-management-table enrollment-service-horizontal enrollment-${resource}`} headerClassName="horizontal-management-head" rowSelector=":scope > .horizontal-management-row" columns={enrollmentTableColumns(resource, details.columns)}>
        {pageItems.map(item => {
          const editable = isEditableEnrollment(resource) && item.values.periodState !== "Retained";
          return <EnrollmentRow resource={resource} item={item} onEdit={editable ? () => setEditing(item) : undefined} onRemove={editable ? () => { void remove(item); } : undefined} key={item.rowKey}/>;
        })}
      </DataTable>
    </>}</PaginatedDataRegion>
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

function enrollmentTableColumns(resource: EnrollmentResource, labels: string[]): DataTableColumn[] {
  return labels.map(label => ({
    key: label,
    label,
    align: resource === "departments" && (label === "Students" || label === "Timetables")
      ? "center"
      : resource === "timetable" && label === "Classroom"
        ? "center"
        : undefined,
  }));
}
