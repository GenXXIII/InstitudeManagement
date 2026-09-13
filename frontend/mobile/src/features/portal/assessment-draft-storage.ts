import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

export type AssessmentDraft = {
  assignment: string;
  midterm: string;
  finalExam: string;
};

const emptyDraft: AssessmentDraft = { assignment: '', midterm: '', finalExam: '' };

export async function readAssessmentDraft(teacherId: string, courseId: string, studentId: string) {
  const stored = await read(draftKey(teacherId, courseId, studentId));
  if (!stored) return null;
  try {
    const draft = JSON.parse(stored) as Partial<AssessmentDraft>;
    return {
      assignment: draft.assignment ?? '',
      midterm: draft.midterm ?? '',
      finalExam: draft.finalExam ?? '',
    } satisfies AssessmentDraft;
  } catch {
    return null;
  }
}

export async function writeAssessmentDraft(teacherId: string, courseId: string, studentId: string, draft: AssessmentDraft) {
  const key = draftKey(teacherId, courseId, studentId);
  if (Object.values(draft).every(value => !value.trim())) {
    await remove(key);
    return;
  }
  await write(key, JSON.stringify(draft));
}

export async function clearAssessmentDraft(teacherId: string, courseId: string, studentId: string) {
  await remove(draftKey(teacherId, courseId, studentId));
}

export function blankAssessmentDraft() {
  return { ...emptyDraft };
}

function draftKey(teacherId: string, courseId: string, studentId: string) {
  return ['ink', 'assessment-draft', teacherId, courseId, studentId].map(part => part.replace(/[^a-zA-Z0-9._-]/g, '_')).join('.');
}

async function read(key: string) {
  if (Platform.OS === 'web') return typeof window === 'undefined' ? null : window.localStorage.getItem(key);
  return SecureStore.getItemAsync(key);
}

async function write(key: string, value: string) {
  if (Platform.OS === 'web') {
    if (typeof window !== 'undefined') window.localStorage.setItem(key, value);
    return;
  }
  await SecureStore.setItemAsync(key, value);
}

async function remove(key: string) {
  if (Platform.OS === 'web') {
    if (typeof window !== 'undefined') window.localStorage.removeItem(key);
    return;
  }
  await SecureStore.deleteItemAsync(key);
}
