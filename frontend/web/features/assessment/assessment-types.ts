export type GradeAssessmentValues = {
  gradeCode: string; studentId: string; student: string; courseId: string; course: string; departmentId: string; department: string;
  attendanceScore: string; attendanceMaximum: string; attendancePresent: string; attendanceSessions: string;
  assignmentScore: string; assignmentMaximum: string; midtermScore: string; midtermMaximum: string; finalExamScore: string; finalExamMaximum: string;
  score: string; grade: string; academicYear: string; term: string; submittedByTeacherId: string; submittedByTeacher: string;
  reviewStatus: "Pending" | "Approved" | "Rejected" | "ResubmitRequested" | "ResubmitAuthorized"; reviewNote: string; submissionVersion: string; submittedAtUtc: string; reviewedAtUtc: string; createAt: string;
};

export type GradeAssessment = { id: string; values: GradeAssessmentValues };
