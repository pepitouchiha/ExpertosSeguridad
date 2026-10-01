import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Genera un bundle de servidor autocontenido para que la imagen final no lleve node_modules.
  output: 'standalone',
};

export default nextConfig;
