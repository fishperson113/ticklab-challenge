import { POST, setAuthToken } from './utils.api.js';

// Re-export the token setter for convenience
export { setAuthToken };

// Enhanced Login endpoint that stores the token (k6 synchronous version)
export const Login = (email, password, twoFactorCode = null, twoFactorRecoveryCode = null) => {
  const data = {
    email,
    password,
    twoFactorCode: twoFactorCode || "string", // Default to "string" as shown in curl
    twoFactorRecoveryCode: twoFactorRecoveryCode || "string" // Default to "string" as shown in curl
  };
  
  const response = POST('/login', data);
  
  // If login successful and we have an access token, store it
  if (response && response.accessToken) {
    setAuthToken(response.accessToken);
  }
  
  return response;
};

// Register endpoint
export const Register = (email, password, studentCode, fullName) => {
  const data = {
    email,
    password,
    studentCode,
    fullName,
  };
  return POST('/register', data);
};

// Refresh token endpoint
export const RefreshToken = () => {
  return POST('/refresh');
};
