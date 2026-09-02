import { Icon } from "@/components/icon";
import { workflowCode } from "@/lib/workflow-code";
import type { ClassroomOperation } from "../operations-types";

export function ClassroomBuilding({ rows }: { rows: ClassroomOperation[] }) {
  return <div className="classroom-building"><div className="building-roof"><Icon name="building" size={18}/><div><strong>Institute learning spaces</strong><span>Timetable controls Available and Running; maintenance states stay fixed</span></div></div>{[5, 4, 3, 2, 1].map(floor => { const floorRooms = rows.filter(row => row.floor === floor); return <section className={`building-floor floor-${floor}`} key={floor}><header><b>{floor}</b><span>Floor {floor}</span></header><div className="floor-rooms">{floorRooms.length ? floorRooms.map(room => { const state = classroomState(room.status); const teacherMissing = state === "available" && (room.teacherAttendance === "Absent" || room.teacherAttendance === "Permission"); return <article className={`operation-${state}-classroom ${teacherMissing ? "teacher-missing-classroom" : ""}`} key={room.id}><div><strong>{workflowCode(room.room, "classroom", "enrollment")}</strong><small>{room.roomType} · {room.course}</small><span className="classroom-live-detail">{room.statusDetail}</span></div><b className={`table-status operation-classroom-status ${state}`}>{classroomStateLabel(state)}</b></article>; }) : <span className="floor-empty">No enrolled learning spaces</span>}</div></section>; })}</div>;
}

function classroomState(status: string) {
  if (status === "In Study") return "running";
  if (status === "Maintenance") return "maintenance";
  if (status === "Unavailable") return "unavailable";
  return "available";
}

function classroomStateLabel(state: ReturnType<typeof classroomState>) {
  return state === "running" ? "Running" : state[0].toUpperCase() + state.slice(1);
}
