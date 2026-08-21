import { managementResourceClient } from "../management-client";
import type { StudentItem } from "../types/student";

export const studentApi = managementResourceClient<StudentItem>("students");
