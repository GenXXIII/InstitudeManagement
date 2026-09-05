import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { StudentItem } from "@/features/management/students/student-types";

export const studentApi = catalogResourceClient<StudentItem>("students");
