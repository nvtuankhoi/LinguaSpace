import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { enableApiMocks } from './app/core/mock/enable-mocks';

// Start the MSW mock layer (dev only) before bootstrapping so the first
// requests are already intercepted.
enableApiMocks()
  .then(() => bootstrapApplication(App, appConfig))
  .catch((err) => console.error(err));
