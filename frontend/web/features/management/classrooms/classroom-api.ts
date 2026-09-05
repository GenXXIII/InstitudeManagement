import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { ClassroomItem } from "@/features/management/classrooms/classroom-types";

export const classroomApi = catalogResourceClient<ClassroomItem>("classrooms");
