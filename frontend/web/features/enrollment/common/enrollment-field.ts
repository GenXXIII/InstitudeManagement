export type EnrollmentFieldOption = {
  id: string;
  label: string;
};

export type EnrollmentField = {
  key: string;
  label: string;
  type?: "select" | "number" | "text" | "time";
  options?: EnrollmentFieldOption[];
  required?: boolean;
};

export function yearOptions(): EnrollmentFieldOption[] {
  return ["1", "2", "3", "4"].map(id => ({ id, label: `Year ${id}` }));
}
