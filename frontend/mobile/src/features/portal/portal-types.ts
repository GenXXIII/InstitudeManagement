import type { MobileRole } from '@/features/auth/auth-context';

export type CatalogItem<TValues extends Record<string, string>> = { id: string; values: TValues };

export type TeacherValues = Record<string, string> & { photoDataUrl: string; teacherCode: string; publicId: string; name: string; email: string; departmentId: string; department: string; status: string };
export type StudentValues = Record<string, string> & { photoDataUrl: string; studentCode: string; publicId: string; name: string; email: string; departmentId: string; department: string; year: string; shift: string; status: string; academicYear: string; semester: string; periodState: string };
export type ScheduleValues = Record<string, string> & { timetableCode: string; enrollmentCode: string; courseId: string; courseCode: string; course: string; teacherId: string; teacherCode: string; teacher: string; classroomId: string; classroom: string; building: string; departmentId: string; department: string; yearLevel: string; shift: string; dayOfWeek: string; startsAt: string; endsAt: string; status: string; academicYear: string; semester: string };
export type AttendanceValues = Record<string, string> & { attendanceCode: string; studentId: string; student: string; studentCode: string; date: string; checkedInAt: string; status: string; method: string; academicYear: string; term: string };
export type GradeValues = Record<string, string> & { gradeCode: string; studentId: string; student: string; courseId: string; course: string; attendanceScore: string; attendanceMaximum: string; attendancePresent: string; attendanceSessions: string; assignmentScore: string; assignmentMaximum: string; midtermScore: string; midtermMaximum: string; finalExamScore: string; finalExamMaximum: string; score: string; grade: string; academicYear: string; term: string; submittedByTeacherId: string; submittedByTeacher: string; reviewStatus: "SubmissionRequested" | "SubmissionAuthorized" | "Submitted" | "Pending" | "Approved" | "Rejected" | "ResubmitRequested" | "ResubmitAuthorized"; reviewNote: string; submissionVersion: string; submittedAtUtc: string; reviewedAtUtc: string };
export type GradeWeights = { attendance: number; assignment: number; midterm: number; finalExam: number };
export type Announcement = { id: string; announcementCode: string; type: 'General' | 'Attendance' | 'Emergency' | 'Result' | 'Finance'; title: string; message: string; isRead: boolean; createAt: string; source: 'announcement' | 'finance'; sourceId: string };
export type StudentPayment = { id: string; paymentCode: string; financialAccountCode: string; studentId: string; studentName: string; academicYear: string; semester: string; title: string; paymentPlan: 'Semester'; declaredAmount: number; declaredAtUtc: string; expiresAtUtc: string; isDeclared: boolean; isExpired: boolean; qrGeneratedAtUtc: string | null; qrExpiresAtUtc: string | null; isQrExpired: boolean; latePenaltyDays: number; latePenaltyAmount: number; totalDue: number; totalPaid: number; balance: number; amountDue: number; currency: string; dueOn: string; status: 'Pending' | 'Partial' | 'Paid' | 'Cancelled' | 'Refunded'; confirmationMethod: string; paidAtUtc: string | null; reminderSentAtUtc: string | null; reminderReadAtUtc: string | null; timetableStatus: 'Ready' | 'Waiting'; qrProvider: 'Bakong KHQR'; qrPayload: string };
export type BankPaymentOption = { name: 'ABA' | 'ACLEDA'; accountName: string; accountCode: string };
export type StudentFinanceOptions = { paymentProviders: BankPaymentOption[]; bakongEnabled: boolean; bakongConfigured: boolean; bakongEnvironment: 'SIT' | 'Production'; dynamicQrBank: string; dynamicQrAccountName: string; dynamicQrAccountCode: string };
export type ClassSessionStartItem = { id: string; scheduleEntryId: string; teacherId: string; sessionDate: string; startedAtUtc: string };
export type ClassPermissionRequestItem = { id: string; studentId: string; studentName: string; studentPublicId: string; teacherId: string | null; teacherName: string; sessionDate: string; reason: string; status: "Pending" | "Approved" | "Rejected"; requestedAtUtc: string; reviewedAtUtc: string | null };
export type PublishedCourseResult = { courseId: string; courseCode: string; name: string; score: number; grade: string };
export type PublishedSemesterResult = { studentId: string; studentCode: string; resultCode: string; fullName: string; departmentId: string; department: string; year: number; shift: string; academicYear: string; semester: string; presentCount: number; absentCount: number; permissionCount: number; attendanceScore: number; attendanceMaximum: number; attendanceGrade: string; grades: PublishedCourseResult[]; expectedCourseCount: number; totalCourses: number; totalScore: number; average: number; overallGrade: string; totalGrade: string; publicationStatus: "Published"; isPublished: true; publishedAtUtc: string };

export type TeacherItem = CatalogItem<TeacherValues>;
export type StudentItem = CatalogItem<StudentValues>;
export type ScheduleItem = CatalogItem<ScheduleValues>;
export type AttendanceItem = CatalogItem<AttendanceValues>;
export type GradeItem = CatalogItem<GradeValues>;
export type PortalProfile = TeacherItem | StudentItem;

export type PortalData = {
  role: MobileRole;
  profile: PortalProfile | null;
  schedule: ScheduleItem[];
  students: StudentItem[];
  attendance: AttendanceItem[];
  grades: GradeItem[];
  publishedResults: PublishedSemesterResult[];
  gradeWeights: GradeWeights;
  announcements: Announcement[];
  payments: StudentPayment[];
  financeOptions: StudentFinanceOptions;
  startedScheduleIds: string[];
  permissionRequests: ClassPermissionRequestItem[];
};
