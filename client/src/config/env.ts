export const ENV = {
  API_URL: import.meta.env.VITE_API_URL || 'https://localhost:5000',
} as const;

Object.freeze(ENV);