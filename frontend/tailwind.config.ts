import type { Config } from 'tailwindcss';

export default {
  content: ['./src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Tomado del logo corporativo, para que la interfaz y la marca coincidan en lugar de usar un
        // azul genérico al lado.
        brand: {
          50: '#eef4fb',
          100: '#d6e5f5',
          200: '#aecbea',
          300: '#7fabdb',
          500: '#2f74bc',
          600: '#1b5faa',
          700: '#154b88',
          800: '#103a6a',
          900: '#0c2b4f',
        },
      },
    },
  },
  plugins: [],
} satisfies Config;
