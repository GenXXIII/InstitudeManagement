import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { DepartmentItem } from "@/features/management/departments/department-types";

export const departmentApi = catalogResourceClient<DepartmentItem>("departments");
