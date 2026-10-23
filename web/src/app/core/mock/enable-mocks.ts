import { environment } from '../../../environments/environment';
import { handlers } from './mock-handlers';

let started = false;

/**
 * Starts the MSW service worker when mocks are enabled (environment.useMock),
 * i.e. in development. No-op in production.
 *
 * Handlers are added per feature (see ./handlers) so the mock layer grows
 * alongside the real UI.
 */
export async function enableApiMocks(): Promise<void> {
  if (!environment.useMock || started || typeof window === 'undefined') {
    return;
  }

  const { setupWorker } = await import('msw/browser');
  const worker = setupWorker(...handlers);
  await worker.start({
    onUnhandledRequest: 'bypass',
    quiet: false,
  });
  started = true;
  // eslint-disable-next-line no-console
  console.info('[mocks] MSW enabled');
}
