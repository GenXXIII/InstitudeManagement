import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { TeacherItem } from "@/features/management/teachers/teacher-types";

export const teacherApi = catalogResourceClient<TeacherItem>("teachers");
