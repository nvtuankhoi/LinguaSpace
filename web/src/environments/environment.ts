// Production environment. In dev this file is replaced by environment.development.ts
// (see angular.json > build > configurations > development > fileReplacements).
// TODO(deploy): set apiBaseUrl + hubs to the real production origin before release.
export const environment = {
  production: true,
  /** When true, HTTP requests are intercepted by the MSW mock layer (design/api-contract.md). */
  useMock: false,
  apiBaseUrl: 'https://localhost:7241/api',
  hubs: {
    room: 'https://localhost:7241/hubs/room',
    presence: 'https://localhost:7241/hubs/presence',
  },
  /** Resolved per-room via POST /api/Rooms/{id}/media-token (MediaTokenDto.liveKitUrl). */
  liveKitUrl: '',
} as const;
