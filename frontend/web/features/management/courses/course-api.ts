import { managementResourceClient } from "../management-client";
import type { CourseItem } from "../types/course";

export const courseApi = managementResourceClient<CourseItem>("courses");
