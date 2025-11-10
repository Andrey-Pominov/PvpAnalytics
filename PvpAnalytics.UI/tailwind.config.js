/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        background: '#070d1f',
        accent: {
          DEFAULT: '#4076ff',
          muted: '#20355c',
        },
        surface: '#101933',
        text: {
          DEFAULT: '#f5f7ff',
          muted: '#94a3c8',
        },
      },
      fontFamily: {
        sans: ['Inter', 'Segoe UI', 'system-ui', '-apple-system', 'BlinkMacSystemFont', '"Helvetica Neue"', 'sans-serif'],
      },
      boxShadow: {
        card: '0 16px 40px rgba(16, 25, 51, 0.35)',
      },
    },
  },
  plugins: [],
}

