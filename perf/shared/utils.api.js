
import Http from './http.api.js';

// Token management
let authToken = null;

export const setAuthToken = (token) => {
  authToken = token;
  // Set the Authorization header for all future requests
  if (token) {
    Http.interceptors.request = (config) => {
      config.headers = config.headers || {};
      config.headers.Authorization = `Bearer ${token}`;
      return config;
    };
  }
};

export const getAuthToken = () => authToken;

export const GET = (url, params) => {
  const config = { params };
  if (authToken) {
    config.headers = { Authorization: `Bearer ${authToken}` };
  }
  return Http.get(url, config).then((response) => response.data);
};

export const POST = (url, data, config = {}) => {
  if (authToken) {
    config.headers = { ...config.headers, Authorization: `Bearer ${authToken}` };
  }
  return Http.post(url, data, config).then((response) => response.data);
};

export const PUT = (url, data, config = {}) => {
  if (authToken) {
    config.headers = { ...config.headers, Authorization: `Bearer ${authToken}` };
  }
  return Http.put(url, data, config).then((response) => response.data);
};

export const DELETE = (url, params) => {
  const config = { params };
  if (authToken) {
    config.headers = { Authorization: `Bearer ${authToken}` };
  }
  return Http.delete(url, config).then((response) => response.data);
};

export const PATCH = (url, data, config = {}) => {
  if (authToken) {
    config.headers = { ...config.headers, Authorization: `Bearer ${authToken}` };
  }
  return Http.patch(url, data, config).then((response) => response.data);
};
