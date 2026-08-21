import { Icon } from "@/components/icon";
import type { ClassroomOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function ClassroomBuilding({ rows }: { rows: ClassroomOperation[] }) {
  return <div className="classroom-building"><div className="building-roof"><Icon name="building" size={18}/><div><strong>Institute learning spaces</strong><span>Classrooms and meeting rooms available for teaching</span></div></div>{[5, 4, 3, 2, 1].map(floor => { const floorRooms = rows.filter(row => row.floor === floor); return <section className={`building-floor floor-${floor}`} key={floor}><header><b>{floor}</b><span>Floor {floor}</span></header><div className="floor-rooms">{floorRooms.length ? floorRooms.map(room => <article className={room.roomType === "Meeting Room" ? "meeting-learning-room" : ""} key={room.id}><div><strong>{room.room}</strong><small>{room.roomType}</small><span>{room.capacity} seats · Device {room.device}</span></div><b className={`table-status ${statusClass(room.status)}`}>{room.status}</b></article>) : <span className="floor-empty">No learning spaces</span>}</div></section>; })}</div>;
}
