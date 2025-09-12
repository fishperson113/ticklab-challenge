import { GET, POST, PUT } from './utils.api.js';

// Get all courses
export const GetAllCourses = () => {
  return GET('/api/courses');
};

// Get course by code
export const GetCourse = (courseCode) => {
  return GET(`/api/courses/${courseCode}`);
};

// Get courses by subject
export const GetCoursesBySubject = (subjectCode) => {
  return GET(`/api/courses/subject/${subjectCode}`);
};

// Create a new course
export const CreateCourse = (courseData) => {
  return POST('/api/courses', courseData);
};

// Update an existing course
export const UpdateCourse = (courseCode, courseData) => {
  return PUT(`/api/courses/${courseCode}`, courseData);
};