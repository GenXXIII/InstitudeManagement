import { catalogResourceClient } from "@/lib/catalog-resource-client";
import type { CourseItem } from "@/features/management/courses/course-types";

export const courseApi = catalogResourceClient<CourseItem>("courses");
