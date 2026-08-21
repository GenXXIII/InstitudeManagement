import { Icon } from "@/components/icon";
import type { References } from "../management-types";

export function ManagementOverview({ references, onSelect, selected }: { references: References; onSelect: (id: string) => void; selected: string }) {
  return <section className="department-overview-grid">{references.departments.filter(department => !selected || department.id === selected).map(department => {
    const teachers = references.teachers.filter(x => x.values.departmentId === department.id);
    const students = references.students.filter(x => x.values.departmentId === department.id);
    const courses = references.courses.filter(x => x.values.departmentId === department.id);
    const rooms = references.classrooms.filter(x => x.values.departmentId === department.id);
    return <article className={`department-overview-card panel ${selected === department.id ? "selected" : ""}`} key={department.id}><div className="department-code-cell"><strong>{department.values.code}</strong><span>Department</span></div><div className="department-overview-head"><div><span>Academic department</span><h2>{department.values.name}</h2></div></div><div className="hod-line"><span>Head of department</span><strong>{department.values.head || "Not appointed"}</strong></div><div className="relationship-counts"><div><strong>{students.length}</strong><span>Students</span></div><div><strong>{teachers.length}</strong><span>Teachers</span></div><div><strong>{courses.length}</strong><span>Courses</span></div><div><strong>{rooms.length}</strong><span>Rooms</span></div></div><span className={`table-status ${department.values.status.toLowerCase()}`}>{department.values.status}</span><button className="button secondary" onClick={() => onSelect(department.id)}>Open department <Icon name="arrow" size={14}/></button></article>;
  })}</section>;
}
