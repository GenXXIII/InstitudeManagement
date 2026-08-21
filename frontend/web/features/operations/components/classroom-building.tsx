import { Icon } from "@/components/icon";
import type { ClassroomOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function ClassroomBuilding({ rows }: { rows: ClassroomOperation[] }) {
  return <div className="classroom-building"><div className="building-roof"><Icon name="building" size={18}/><div><strong>Main Building</strong><span>Five-floor live classroom overview</span></div></div>{[5, 4, 3, 2, 1].map(floor => <section className={`building-floor floor-${floor}`} key={floor}><header><b>{floor}</b><span>Floor {floor}</span></header>{floor === 5 ? <div className="meeting-hall"><Icon name="users" size={17}/><div><strong>Meeting Hall</strong><span>Top-floor meetings and institute events</span></div><i>Floor 5</i></div> : <div className="floor-rooms">{rows.filter(row => row.floor === floor).map(room => <article key={room.id}><div><strong>{room.room}</strong><span>{room.capacity} seats · Device {room.device}</span></div><b className={`table-status ${statusClass(room.status)}`}>{room.status}</b></article>)}</div>}</section>)}</div>;
}
