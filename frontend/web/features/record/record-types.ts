export type OperationalRecord = { id: string; module: string; subject: string; identifier: string; status: string; summary: string; lastActivityAt?: string | null; activities: Record<string, string>[]; classSessionRecordCode: string };
export type ClassSessionAttendanceUpdate = { studentId: string; status: string; checkedInAt: string };
