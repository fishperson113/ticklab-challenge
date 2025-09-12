import { SharedArray } from 'k6/data';
import papaparse from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

// Load student accounts from CSV file using SharedArray for memory efficiency
export const testAccounts = new SharedArray('student_accounts', function () {
  // Read CSV file content
  const csvData = open('../data/student.csv');
  
  // Parse CSV data
  const parsedData = papaparse.parse(csvData, {
    header: true,        // First row contains headers
    skipEmptyLines: true // Skip empty lines
  });  
  // Transform CSV data to our expected format
  return parsedData.data.map((row, index) => ({
    studentCode: row.StudentCode,
    email: row.Email,
    password: row.Password,
    index: index // Add index for easy reference
  }));
});

// Utility function to get account by VU number
export function getAccountForVU(vuNumber) {
  const index = (vuNumber - 1) % testAccounts.length;
  return testAccounts[index];
}

// Utility function to get account by index
export function getAccountByIndex(index) {
  return testAccounts[index % testAccounts.length];
}

// Utility function to get random account
export function getRandomAccount() {
  const randomIndex = Math.floor(Math.random() * testAccounts.length);
  return testAccounts[randomIndex];
}

// Get total number of accounts
export function getTotalAccounts() {
  return testAccounts.length;
}

// Get accounts info for logging
export function getAccountsInfo() {
  return {
    totalAccounts: testAccounts.length,
    sampleAccounts: testAccounts.slice(0, 5), // First 5 accounts as sample
    accountsRange: {
      first: testAccounts[0],
      last: testAccounts[testAccounts.length - 1]
    }
  };
}
