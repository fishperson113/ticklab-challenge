import { HTTP_ERRORS } from './http-errors.js';
import { environment } from '../environments/environments.js';
import http from 'k6/http';

const backendUrl =
  (environment && environment.BACKEND_URL) ||
  __ENV.BACKEND_URL ||
  'http://localhost:8080';
console.log('Backend URL:', backendUrl);

const defaultHeaders = {
  'Content-Type': 'application/json',
  Accept: 'application/json',
};

let isRefreshing = false;
let refreshSubscribers = [];

function onRefreshed() {
  refreshSubscribers.forEach((cb) => cb());
  refreshSubscribers = [];
}
function addRefreshSubscriber(cb) {
  refreshSubscribers.push(cb);
}

function thenResolve(value) {
  return { then: (onFulfilled) => (onFulfilled ? onFulfilled(value) : value) };
}
function thenReject(error) {
  return {
    then: (_onFulfilled, onRejected) =>
      onRejected ? onRejected(error) : error,
  };
}
function thenable(executor) {
  return {
    then: (onFulfilled, onRejected) => {
      try {
        executor(
          (v) => (onFulfilled ? onFulfilled(v) : v),
          (e) => (onRejected ? onRejected(e) : e)
        );
      } catch (e) {
        if (onRejected) onRejected(e);
      }
    },
  };
}

function isAbs(url) {
  return /^https?:\/\//i.test(url);
}
function withParams(url, params) {
  if (!params) return url;
  const q = Object.entries(params)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
    .join('&');
  return q ? url + (url.includes('?') ? '&' : '?') + q : url;
}
function parseData(res) {
  const ct = (res.headers['Content-Type'] || res.headers['content-type'] || '')
    .toLowerCase();
  if (ct.includes('application/json')) {
    try { return res.json(); } catch (_) {}
  }
  return res.body;
}

function coreRequest(method, url, data, config = {}) {
  const fullUrl = isAbs(url) ? url : backendUrl + url;
  const opts = {
    headers: { ...defaultHeaders, ...(config.headers || {}) },
    timeout: (config.timeout || '60s'),
  };
  const finalUrl = withParams(fullUrl, config.params);

  let res;
  if (method === 'GET') res = http.get(finalUrl, opts);
  else if (method === 'DELETE') res = http.del(finalUrl, null, opts);
  else {
    const body =
      data == null || typeof data === 'string' ? data || null : JSON.stringify(data);
    if (!opts.headers['Content-Type'])
      opts.headers['Content-Type'] = 'application/json';
    if (method === 'POST') res = http.post(finalUrl, body, opts);
    else if (method === 'PUT') res = http.put(finalUrl, body, opts);
    else res = http.patch(finalUrl, body, opts);
  }

  const axiosLikeResponse = {
    status: res.status,
    data: parseData(res),
    headers: res.headers,
    config,
    request: { method, url: finalUrl },
  };

  if (res.status >= 200 && res.status < 300) return thenResolve(axiosLikeResponse);

  const error = {
    config: { method, url, ...config, _retry: config._retry || false },
    response: axiosLikeResponse,
  };

  const shouldRefresh =
    (res.status === 401 || res.status === 403) &&
    !error.config._retry &&
    !error.config.url.includes('/refresh') &&
    !error.config.url.includes('/login') &&
    !error.config.url.includes('/register');

  if (shouldRefresh) {
    error.config._retry = true;
    return thenable((resolve, reject) => {
      addRefreshSubscriber(() => {
        Http(error.config).then(resolve).then(null, reject); 
      });

      if (!isRefreshing) {
        isRefreshing = true;
        Http.get(`${backendUrl}/refresh`)
          .then(() => {
            isRefreshing = false;
            onRefreshed();
          })
          .then(null, (_refreshErr) => {
            isRefreshing = false;
            reject({
              status: 401,
              message: HTTP_ERRORS[401] || 'Unauthorized',
            });
          });
      }
    });
  }

  const errorMessage =
    axiosLikeResponse.data?.message ||
    HTTP_ERRORS[res.status] ||
    'Something went wrong. Please try again later';
  return thenReject({
    status: res.status || 500,
    message: errorMessage,
  });
}

function Http(input) {
  if (typeof input === 'string') {
    return Http.get(input);
  }
  const { method = 'GET', url, data, ...config } = input || {};
  return coreRequest(method.toUpperCase(), url, data, config);
}

Http.get = (url, config = {}) => coreRequest('GET', url, null, config);
Http.delete = (url, config = {}) => coreRequest('DELETE', url, null, config);
Http.post = (url, data, config = {}) => coreRequest('POST', url, data, config);
Http.put = (url, data, config = {}) => coreRequest('PUT', url, data, config);
Http.patch = (url, data, config = {}) => coreRequest('PATCH', url, data, config);

Http.interceptors = {
  response: {
    use: (_onFulfilled, _onRejected) => {},
  },
};

export default Http;
