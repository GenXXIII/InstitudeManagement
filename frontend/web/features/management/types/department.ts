export type DepartmentValues = Record<string, string> & {
  departmentCode: string;
  name: string;
  headTeacherId: string;
  head: string;
  status: string;
  createAt: string;
};

export type DepartmentItem = { id: string; values: DepartmentValues };
