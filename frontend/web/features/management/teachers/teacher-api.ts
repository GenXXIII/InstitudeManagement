import { managementResourceClient } from "../management-client";
import type { TeacherItem } from "../types/teacher";

export const teacherApi = managementResourceClient<TeacherItem>("teachers");
