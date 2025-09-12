import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';
import { SharedArray } from 'k6/data';
import { GetMyEnrollments, EnrollInCourse, WithdrawFromCourse } from './shared/user.api.js';
import { GetAllCourses } from './shared/course.api.js';
import { setAuthToken } from './shared/utils.api.js';

/* ===== Metrics (tối giản, có phân loại) ===== */
const enrollAttempts = new Counter('enroll_attempts');
const enrollSuccess  = new Counter('enroll_success');
const enrollDupRate  = new Rate('enroll_duplicate_rate'); // 409 hoặc business duplicate
const infraFailRate  = new Rate('infra_failure_rate');    // 5xx/timeout

const withdrawAttempts = new Counter('withdraw_attempts');
const withdrawSuccess  = new Counter('withdraw_success');

/* ===== Tokens (8k, mỗi dòng 1 token) ===== */
const TOKENS = new SharedArray('tokens8k', () => {
  const lines = open('data/token.txt').trim().split(/\r?\n/);
  if (lines.length < 8000) throw new Error(`Need 8000 tokens, got ${lines.length}`);
  return lines.slice(0, 8000);
});

/* ===== K6 options ===== */
export const options = {
  stages: [
    { duration: '1m', target: 4000 }, // ramp up
    { duration: '5m', target: 4000 }, // sustain
    { duration: '1m', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<5000'],
    enroll_duplicate_rate: ['rate>=0'], // chỉ để hiển thị (không fail)
    infra_failure_rate:  ['rate<0.02'], // hạ tầng fail phải <2%
  },
  // Cần body để parse danh sách course/enrollments
  gracefulRampDown: '30s',
};

/* ===== Helpers ===== */
function myToken() { return TOKENS[__VU - 1]; }

/** Lấy status nếu wrapper trả raw; nếu không thì suy luận. */
function getStatus(res) {
  if (!res) return -1;
  if (typeof res.status === 'number') return res.status;
  if (res === true) return 200;
  if (res === false) return 400;
  return -1;
}

/** enroll check: phân biệt success / duplicate (409) / infra fail */
function classifyEnroll(res) {
  const st = getStatus(res);
  if (st >= 200 && st < 300) { enrollSuccess.add(1); return 'ok'; }
  if (st === 409)           { enrollDupRate.add(1);  return 'dup'; }
  if (st >= 500 || st === -1 || res?.timedOut) { infraFailRate.add(1); return 'infra'; }
  return 'other'; // 4xx khác (nghiệp vụ khác) – không tính infra fail
}

/** chọn ngẫu nhiên k phần tử (không sort cả mảng để nhẹ) */
function pickRandomN(arr, k) {
  if (!arr?.length) return [];
  const n = Math.min(k, arr.length);
  const out = new Array(n);
  for (let i = 0; i < n; i++) {
    out[i] = arr[(Math.random() * arr.length) | 0];
  }
  return out;
}

/* ===== Test flow ===== */
export default function () {
  const token = myToken();
  if (!token) return;
  setAuthToken(token); 

  // 1) Get courses
  const courses = GetAllCourses();
  if (!check(courses, { 'get courses ok': (r) => Array.isArray(r) })) return;

  // 2) Enroll 2–4 courses (mỗi course retry tối đa 1 lần nếu fail)
  const toEnroll = pickRandomN(courses, (Math.random() * 3 | 0) + 2); // 2..4
  for (const c of toEnroll) {
    enrollAttempts.add(1);
    let result = classifyEnroll(EnrollInCourse(c.courseCode));
    if (result === 'infra') {
      // retry 1 lần nếu infra fail
      sleep(0.2 + Math.random() * 0.5);
      result = classifyEnroll(EnrollInCourse(c.courseCode));
    }
    // sleep ngắn giữa các enroll
    sleep(0.1 + Math.random() * 0.2);
  }

  // 4) Think-time giữa iterations
  sleep(0.5 + Math.random()); // 0.5–1.5s
}
