// Development environment — Mock-first: all API calls hit the MSW layer (no backend needed).
export const environment = {
  production: false,
  useMock: true,
  apiBaseUrl: '/api',
  hubs: {
    room: '/hubs/room',
    presence: '/hubs/presence',
  },
  liveKitUrl: '',
} as const;
