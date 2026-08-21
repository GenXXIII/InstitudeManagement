import { managementResourceClient } from "../management-client";
import type { DepartmentItem } from "../types/department";

export const departmentApi = managementResourceClient<DepartmentItem>("departments");
