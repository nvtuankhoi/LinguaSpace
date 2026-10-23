import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import { UploadedFileResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class MediaApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** POST /Feed/posts/media — upload up to 4 images/videos; returns their public URLs. */
  uploadPostMedia(files: File[]) {
    const form = new FormData();
    for (const file of files) {
      form.append('files', file, file.name);
    }
    return this.http.post<UploadedFileResponse[]>(`${this.base}/Feed/posts/media`, form, {
      withCredentials: true,
    });
  }

  /** POST /Users/me/avatar/upload — upload a single avatar image; returns its public URL. */
  uploadAvatar(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<UploadedFileResponse>(`${this.base}/Users/me/avatar/upload`, form, {
      withCredentials: true,
    });
  }
}
