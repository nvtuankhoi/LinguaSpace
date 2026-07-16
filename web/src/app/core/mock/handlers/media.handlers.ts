import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';

const BASE = environment.apiBaseUrl;

// Mock mode can't serve real uploaded files, so the handlers return stable placeholder
// URLs (images via picsum, a short sample clip for video) so the upload → render flow
// works end-to-end against the mock layer. Real files are served by the backend in realdev.
const SAMPLE_VIDEO_URL =
  'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4';

export const mediaHandlers = [
  http.post(`${BASE}/Feed/posts/media`, async ({ request }) => {
    const form = await request.formData();
    const files = form.getAll('files') as File[];
    const stamp = Date.now();

    const items = files.map((file, i) => {
      const isVideo = file.type.startsWith('video/');
      return {
        url: isVideo ? SAMPLE_VIDEO_URL : `https://picsum.photos/seed/${stamp}-${i}/600/600`,
        contentType: file.type || (isVideo ? 'video/mp4' : 'image/jpeg'),
      };
    });

    return HttpResponse.json(items);
  }),

  http.post(`${BASE}/Users/me/avatar/upload`, async () => {
    return HttpResponse.json({
      url: `https://picsum.photos/seed/avatar-${Date.now()}/200/200`,
      contentType: 'image/jpeg',
    });
  }),
];
