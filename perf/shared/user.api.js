import { DELETE, GET, PATCH, POST, PUT } from './utils.api.js';

// Get student profile
export const GetProfile = () => {
  return GET('/api/students/me');
};

// Update student profile
export const UpdateProfile = (studentCode, fullName) => {
  const data = {
    studentCode,
    fullName,
  };
  return PUT('/api/students/me', data);
};

// Get student enrollments
export const GetMyEnrollments = () => {
  return GET('/api/students/enrollments');
};

// Enroll in a course
export const EnrollInCourse = (courseCode) => {
  const data = {
    courseCode,
  };
  return POST('/api/students/enrollments', data);
};

// Withdraw from a course
export const WithdrawFromCourse = (courseCode) => {
  return DELETE(`/api/students/enrollments/${courseCode}`);
};

// Legacy compatibility - alias for withdraw
export const LeaveCourse = (courseCode) => {
  return WithdrawFromCourse(courseCode);
};