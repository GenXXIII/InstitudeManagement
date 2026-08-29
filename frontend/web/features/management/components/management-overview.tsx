import Link from "next/link";
import { Icon } from "@/components/icon";
import type { References } from "../management-types";

const lifecycle = [
  { icon: "users", source: "Students", management: "students", next: "Student Enrollment", enrollment: "students", rule: "Identity, department, year, and shift" },
  { icon: "teacher", source: "Teachers", management: "teachers", next: "Teacher Assign", enrollment: "teachers", rule: "Identity and home department" },
  { icon: "book", source: "Courses", management: "courses", next: "Course Assign", enrollment: "courses", rule: "Course code, name, credits, and level" },
  { icon: "room", source: "Classrooms", management: "classrooms", next: "Classroom Assign", enrollment: "classrooms", rule: "Room code, building, type, and capacity" },
  { icon: "calendar", source: "Schedule", management: "timetable", next: "Timetable Enrollment", enrollment: "timetable", rule: "Code, day, time, and create date" },
  { icon: "building", source: "Departments", management: "departments", next: "Department Assign", enrollment: "departments", rule: "Department code, name, and leadership" },
] as const;

export function ManagementOverview({ references, onSelect, selected, year }: { references: References; onSelect: (id: string) => void; selected: string; year: string }) {
  const departments = references.departments.filter(department => !selected || department.id === selected);
  const metrics = [
    { icon: "users", label: "Student master records", value: references.students.length, module: "students", detail: year ? `Year ${year}` : "All year levels" },
    { icon: "teacher", label: "Teacher master records", value: references.teachers.length, module: "teachers", detail: "Ready for assignment" },
    { icon: "book", label: "Course master records", value: references.courses.length, module: "courses", detail: "Includes course level" },
    { icon: "room", label: "Classroom master records", value: references.classrooms.length, module: "classrooms", detail: "Rooms and capacity" },
    { icon: "calendar", label: "Schedule templates", value: references.timetable.length, module: "timetable", detail: "Code, day, and time" },
  ] as const;

  return <div className="management-control-overview">
    <section className="enrollment-overview-metrics" aria-label="Management source data">
      {metrics.map(metric => <Link className="panel enrollment-overview-metric" href={scopedHref(`/management/${metric.module}`, selected, year)} key={metric.module}>
        <span className="complete"><Icon name={metric.icon} size={17}/></span>
        <div><small>{metric.label}</small><strong>{metric.value.toLocaleString()}</strong><p>{metric.detail}</p></div>
        <Icon name="arrow" size={14}/>
      </Link>)}
    </section>

    <section className="panel management-lifecycle-map">
      <header><div><span>Easy maintenance flow</span><h2>Create master data, then assign it in Enrollment</h2><p>Management stores reusable institute data. Enrollment owns semester relationships, and Operation only reads completed enrollment.</p></div></header>
      <div className="management-lifecycle-list">
        {lifecycle.map(row => <div className="management-lifecycle-row" key={row.source}>
          <span><Icon name={row.icon} size={16}/></span>
          <Link href={scopedHref(`/management/${row.management}`, selected, year)}><small>Management source</small><strong>{row.source}</strong></Link>
          <Icon name="arrow" size={13}/>
          <Link href={scopedHref(`/enrollment/${row.enrollment}`, selected, year)}><small>Semester relationship</small><strong>{row.next}</strong></Link>
          <span className="management-lifecycle-rule">{row.rule}</span>
        </div>)}
      </div>
    </section>

    <section className="panel management-department-coverage">
      <header><div><span>Source data coverage</span><h2>Departments and their master records</h2></div><Link className="button secondary" href={scopedHref("/management/departments", selected, year)}>Manage departments <Icon name="arrow" size={14}/></Link></header>
      <div className="management-department-table">
        <div className="management-department-head"><span>Department code</span><span>Department</span><span>Head</span><span>Students</span><span>Teachers</span><span>Courses</span><span>Open</span></div>
        {departments.map(department => {
          const students = references.students.filter(student => student.values.departmentId === department.id).length;
          const teachers = references.teachers.filter(teacher => teacher.values.departmentId === department.id).length;
          const courses = references.courses.filter(course => course.values.departmentId === department.id).length;
          return <article className="management-department-row" key={department.id}>
            <strong>{department.values.departmentCode}</strong><span>{department.values.name}</span><span>{department.values.head || "Not appointed"}</span><b>{students}</b><b>{teachers}</b><b>{courses}</b><button type="button" onClick={() => onSelect(department.id)}>View <Icon name="arrow" size={12}/></button>
          </article>;
        })}
        {!departments.length && <div className="empty-state"><strong>No departments in this scope</strong><span>Add a DepartmentCode before linking other management records.</span></div>}
      </div>
    </section>
  </div>;
}

function scopedHref(pathname: string, departmentId: string, year: string) {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  return `${pathname}${params.size ? `?${params}` : ""}`;
}
