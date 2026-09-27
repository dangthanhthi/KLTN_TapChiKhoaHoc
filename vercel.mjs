const apiOrigin = (process.env.HUIT_API_ORIGIN || '').trim().replace(/\/$/, '');

if (apiOrigin && !/^https:\/\/[^/]+$/i.test(apiOrigin)) {
  throw new Error('HUIT_API_ORIGIN must be an HTTPS origin without a path.');
}

export const config = {
  version: 2,
  framework: null,
  buildCommand: 'node scripts/build-web.mjs',
  outputDirectory: 'dist',
  rewrites: [
    ...(apiOrigin ? [
      { source: '/api/:path*', destination: `${apiOrigin}/api/:path*` },
      { source: '/uploads/avatars/:path*', destination: `${apiOrigin}/uploads/avatars/:path*` }
    ] : []),
    { source: '/', destination: '/UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html' }
  ]
};
