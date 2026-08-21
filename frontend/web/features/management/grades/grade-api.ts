import { managementResourceClient } from "../management-client";
import type { GradeItem } from "../types/grade";

export const gradeApi = managementResourceClient<GradeItem>("grades");
