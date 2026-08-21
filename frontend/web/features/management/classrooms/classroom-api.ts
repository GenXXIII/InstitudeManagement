import { managementResourceClient } from "../management-client";
import type { ClassroomItem } from "../types/classroom";

export const classroomApi = managementResourceClient<ClassroomItem>("classrooms");
