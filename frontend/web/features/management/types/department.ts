export type DepartmentValues = Record<string, string> & {
  code: string;
  name: string;
  headTeacherId: string;
  head: string;
  status: string;
};

export type DepartmentItem = { id: string; values: DepartmentValues };
