// Real-backend dev mode (`ng serve -c realdev`): MSW is off and all requests are
// proxied to the running .NET API via proxy.conf.json. Same-origin relative URLs,
// so the browser never hits CORS. Mirrors environment.development otherwise.
export const environment = {
  production: false,
  useMock: false,
  apiBaseUrl: '/api',
  hubs: {
    room: '/hubs/room',
    presence: '/hubs/presence',
  },
  liveKitUrl: '',
} as const;
