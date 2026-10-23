# BACKLOG — Frontend còn thiếu so với Backend

Cập nhật: 2026-07-16 · Branch: `feat/edit-delete-flows`

Tham chiếu chuẩn: `design/api-contract.md` (Angular phải mirror backend).
Legend: **S** = nhỏ (<~1h), **M** = vừa, **L** = lớn. Endpoint prefix `/api`.

---

## ✅ Đã hoàn thành (initiative này)

- **Direct Message realtime** (`55aa1ee`): realtime + đúng thứ tự thời gian (fix PresenceHub `FindAsync`→`FirstOrDefaultAsync`).
- **Edit/Delete flows** (`6946f8e`): sửa/xóa post, comment, DM; xóa room message; fix `.gitignore` rule `Icon` đang che source `IconComponent`.
- **Realtime** (`7ffe373`): participant list room cập nhật live (join/leave); following-feed hiện post mới live. Backend đã rebuild + deploy.
- **Languages CRUD + Post search** (`46d7a3d`): thêm/sửa level/xóa language ở profile; search có tab People/Posts.
- **Moderation admin console** (`dff3ee6`): console `/app/admin` (role `Administrator`) — list/filter reports, resolve/dismiss, ban user; `adminGuard` + nav entry. Track thêm `app.routes`/`auth.guard`/`shell`.
- **Quick wins** (`aabfb2e`): hủy friend request (tab Sent), tab Blocked + unblock, host mute + invite-by-search, trang post detail `/app/post/:id` (permalink ở timestamp). Fix latent bug: outgoing requests giờ filter đúng.
- **Participant mute (đầy đủ)** (`810635b` + `f375650`): expose `isMuted` trong `RoomParticipantDto` + host toggle (Mute/Unmute + badge Muted). Backend enforce (user bị mute không gửi chat được) + broadcast `ParticipantMuted` realtime để mọi client (kể cả user bị mute) sync state live.
- **Edit/delete realtime sync (DM + room)** (`037543f`): edit/delete DM và delete room message giờ sync live cho participant/tab khác (broadcast `DirectMessageEdited`/`DirectMessageDeleted` qua PresenceHub + `MessageDeleted` qua RoomHub). Feed post/comment edit/delete sync lúc đó còn thiếu — đã làm ở `3d474a2` (backend) + `508e3d5` (FE).
- **Live media presence** (`17f5a71`): xem ai đang trong voice/video call live — seed qua `GET /media/participants` khi mở room + consume `UserJoinedMedia`/`UserLeftMedia` realtime (badge "In call" trong manage modal + đếm "N in call" ở av-bar). Backend broadcast sẵn qua LiveKit webhook nên FE-only.
- **Feed post-group broadcasting (backend)** (`3d474a2`): cơ chế realtime cho feed — viewer xem post giờ nhận live comment/reaction + edit/delete. Mở rộng PresenceHub bằng group `post-{id}` (`JoinPostGroup`/`LeavePostGroup`) + `NotifyPostGroupAsync`; broadcast `NewComment`, `NewReaction(likeCount)`, `CommentEdited`/`Deleted`, `PostEdited`/`Deleted`. Reuse connection PresenceHub (không thêm hub).
- **Feed live (FE)** (`508e3d5`): trang post detail giờ live — comment mới/edit/delete, reaction count (post + comment), post edit, post delete (tự về feed). `PostDetailComponent` join/leave post group + apply event cấp post; `PostCard` (expanded) apply event cấp comment. Feed list giữ nguyên (chỉ NewPost live). Hoàn tất story realtime cho feed.
- **Media session mgmt** (`3ee9655`): host "End call" (nút danger trong AV bar) — `DELETE /Rooms/{id}/media` terminate LiveKit room, ngắt kết nối tất cả. Wire thêm `GET .../media/status` để reconcile in-call set sau end-call. Handle `RoomEvent.Disconnected` (host end-call / mất mạng / server kick) để reset UI AV thay vì kẹt "Live" stale. Đóng nốt item C media-session-mgmt (chỉ còn email verify-token + FCM).
- **Feed list live** (`f664db7`): đưa realtime lên cả feed list (following + explore), không chỉ trang detail. Mỗi post loaded join post group; `PostEdited`/`PostDeleted`/`NewReaction(Post)`/`NewComment`/`CommentDeleted` patch thẳng `items` trong FeedStore. Hoàn thiện story realtime feed end-to-end (list + detail).
- **Email verification UI** (`d8d407e`): wire `POST /Auth/verify-email` — nhập token thủ công ở Settings (input + Verify, refresh `isEmailConfirmed`) + trang `/verify-email` consume link từ email (auto-verify). authGuard giữ `?returnUrl` để click link lúc chưa login round-trip token qua login (guard open-redirect). Đóng item C email-verify (chỉ còn FCM).
- **Reaction remove broadcast** (`aff2b40`, backend): un-react giờ push `NewReaction`(likeCount giảm) qua post-group, để viewer khác (và actor) thấy count giảm live thay vì kẹt stale tới reload. `RemoveReactionCommand` inject `INotificationService` + broadcast inline sau save (mirror pattern `DeletePost` cho delete, payload y hệt `NewReaction` của add). FE không đổi — đã apply `NewReaction` tuyệt đối. Đóng nốt gap cuối của story reaction realtime.
- **Search/profile feed live** (`6226b00`): đưa realtime lên cả search (tab Posts) + public profile — mỗi post loaded join post group + patch signal cục bộ (`PostEdited`/`PostDeleted`/`NewReaction(Post)`/`NewComment`/`CommentDeleted`) qua helper chung `live-post-patch`. Các surface này giờ không kẹt stale tới reload. Hoàn thiện consistency realtime feed end-to-end (feed list + detail + search + profile).
- **XP history (profile activity)** (`365467d`): trang profile giờ hiện "Recent activity" — breakdown XP theo ngày + từng transaction (reason/amount/time) từ `GET /Gamification/me/xp/history`. Data đã load sẵn qua `GamificationStore.loadProgress` nhưng chưa render; thêm section + toggle Week/Month (dùng `store.loadXpHistory`), ngày/transaction newest-first (backend trả ascending). Backend lưu reason dạng câu sẵn ("Joined voice room", "Voice session (12 min)") nên render verbatim. Đóng gap latent: surface dữ liệu backend expose mà FE fetch nhưng chưa show.
- **My rooms view** (`e0a5e7e`): trang rooms list thêm toggle **Discover / My rooms** — "My rooms" gọi `GET /Rooms/mine` (endpoint backend có sẵn trả `PaginatedResult<RoomSummaryDto>`, FE chưa wire). Thêm `RoomsApi.getMine` + `RoomStore.loadMyRooms` (mirror `loadRooms`), toggle segmented control reuse `.filter-tab` (0 CSS mới), reset type filter khi đổi source, empty-state copy riêng per source. Phát hiện qua scan api-contract vs FE. Repo note: `rooms-list.component.{ts,html,scss}` trước đó **untracked** (chưa từng commit dù component đang dùng) — commit này đưa cả component vào VCS ở trạng thái hiện tại (cùng lúc với feature).
- **Who reacted to a post** (`bdff4e8`): like-count của post giờ là nút bấm riêng (tách khỏi nút react) — click mở popover list ai đã react (avatar + tên + emoji reaction loại đó) qua `GET /Feed/posts/{postId}/reactions` (endpoint backend có sẵn trả `PaginatedResult<ReactionDetailDto>`, model FE có sẵn nhưng chưa wire). Thêm `FeedApi.getReactions` + `PostCard.toggleReactors` (refetch mỗi lần mở cho fresh). Popover reuse pattern `.reaction-picker` (absolute, `picker-pop` keyframe, `--ls` tokens) ~40 dòng SCSS, dưới budget 8kB. Hoạt động trên mọi surface PostCard render (feed/explore/search/profile/detail).
- **Resend verification email sender** (`281dabe`, backend): `ResendEmailVerificationCommand` (POST /Auth/resend-verification) trước đây sinh token rồi vứt — nút "Resend verification email" ở Settings là no-op câm. Giờ capture token + `GetEmailAsync` + gửi link verify y hệt `SendVerificationEmailHandler` (registration). FE button (`auth.api.resendVerification`) + trang `/verify-email` đã wire sẵn từ trước → giờ end-to-end. Email infra (`IEmailService`/`ConsoleEmailService`) đã có; ~5 dòng, no migration/config. Đóng backend-only gap sạch nhất.
- **VocabCard posts** (`5add71a` backend + `bc97433` FE): backend support `PostType.VocabCard` + JSON metadata từ đầu nhưng FE chỉ tạo Text → giờ tạo + render end-to-end. Composer: toggle Text/Vocab card + meaning/pronunciation/example (front = `content` reuse để searchable). Component mới `vocab-card` (flip-card 3D, click/Enter-Space, reduced-motion guard, `--ls` tokens). PostCard branch trên `postType` → cover mọi surface (feed/explore/search/profile/detail). Backend: 3 field nullable vào `PostMetadataDto` (no migration, JSON column). Limit: back/pronunciation/example chưa edit được (`UpdatePostRequest` không có metadata).
- **Media upload (post media + avatar)** (`8f2696b` backend + `bf64ccb` FE): upload endpoint + storage thật — generalize `IStorageService` thành `UploadAsync(category, userId, stream, fileName, contentType) → UploadedFile`; `LocalStorageService` persist `wwwroot/uploads/{cat}/{userId}/{guid}.{ext}` (serve qua `UseFileServer`), `DeleteAsync` thật. Hai endpoint: `POST /Feed/posts/media` (≤4 file; ảnh ≤5MB / video ≤50MB; `RequestSizeLimit` 60MB) + `POST /Users/me/avatar/upload` (1 ảnh ≤5MB → trả URL, FE đẩy vào `UpdateProfile.avatarUrl`, không thêm command). Composer: picker nhiều file + preview/remove + gửi `mediaUrls`. PostCard: gallery (`<img>`/`<video controls>`, grid 1 full / 2-4 hai cột) — **thêm `MediaItems` vào `PostSummaryDto`** + project ở cả 4 list query (GetFeed/GetExplore/GetUserPosts/SearchPosts) để media hiện ở feed/explore/search/profile, không chỉ detail. Profile: avatar file picker → preview live qua `avatarPreview`. MSW mock placeholder. Không migration (`PostMediaItem` unchanged; infer img/video từ ext client-side). Limit: `wwwroot/uploads` local ephemeral (Phase 2 Azure Blob, DI unchanged), media-type không persist (infer từ ext), `/uploads/*` public-readable (capability-URL via GUID), không resize/thumbnail server-side, avatar persistence vẫn qua `UpdateProfile` (deferred, không race với Save).

> **Note (2026-07-13):** đã scan api-contract vs FE hệ thống **2 lần** — **các gap FE-only sạch đã cạn hẳn** (mọi ứng viên — badges own-profile, post-edit, avatar — đều đã wire, chỉ là false positive). Backend-only sạch nhất (resend verification sender, `281dabe`) + post-type cao-value nhất (**VocabCard**, `5add71a`+`bc97433`) đều đã đóng. Còn lại cần **backend feature mới + quyết định contract** (notification preferences, clear-conversation, DM search — endpoint backend chưa có; **Poll** cần vote endpoint + state; **media-upload** ĐÃ xong (`8f2696b`+`bf64ccb`)), hoặc **external config/SDK** (FCM cần FCM SDK, Google OAuth cần `GoogleClientId` + Google Identity SDK), hoặc **effort lớn video** (screen-share/active-speaker UI). Không còn gap S/M sẵn sàng làm ngay.

---

## ✅ A. Moderation admin console — DONE (`dff3ee6`)

Backend đầy đủ (`src/Web/Endpoints/Moderation.cs`, role `Administrator`). Đã có route `/app/admin` + `adminGuard` + console UI đầy đủ.

- [x] **Role guard** + admin route `/app/admin` (`adminGuard` check `roles.includes('Administrator')`).
- [x] **`moderation.api.ts`**: `getReports` / `getReport` / `resolveReport` / `banUser` / `unbanUser`.
- [x] **Console UI** (`features/admin/moderation.component`): filter tabs (Pending/UnderReview/Resolved/Dismissed/All), report cards (target chip, status badge, reason, meta grid), resolve/dismiss, ban user, pagination.
- [x] **Nav link** trong user-menu, chỉ hiện admin (`shell.isAdmin`).
- [x] **Models**: dùng `ReportDto`/`ReportStatus`/`ReportAction`/`ReportTargetType` có sẵn.
- Còn lại: `unbanUser` có ở api+store nhưng chưa có UI (backend không expose danh sách user-banned, nên không biết trạng thái ban từ report).

---

## ✅ B. Quick wins — DONE (`aabfb2e`)

- [x] **Host controls**: `mute` (nút per-participant trong host modal) + `invite` (invite-by-search trong modal, ẩn user đã trong room). *(Kick đã có từ trước.)*
- [x] **Hủy friend request**: tab Requests giờ chia Incoming/Sent; Sent có nút Cancel.
- [x] **Trang Block list**: tab Blocked trong Network + nút Unblock.
- [x] **Post detail page**: `/app/post/:id` (dùng `getPost`), reuse `PostCardComponent` với `expanded`, permalink ở timestamp.

---

## 🟠 C. Thiếu hoàn toàn (BE có, FE chưa có API lẫn UI)

- [x] **Email verify-token** (S): `POST /Auth/verify-email` ĐÃ wire (`d8d407e`) — nhập token ở Settings + trang `/verify-email` consume link.
- [ ] **FCM push** (M): `POST /Auth/device-token` — chưa wire (cần FCM SDK + registration).
- [ ] **Media session mgmt** (M): `GET /media/participants` ĐÃ wire (seed in-call set, `17f5a71`); `GET .../media/status` + `DELETE .../media` (host end-call) ĐÃ wire (`3ee9655`).
- [x] **Feed live comments/reactions (FE)** — ĐÃ xong: trang post detail (`508e3d5`) + feed list (`f664db7`). Cả list lẫn detail đều live.

---

## 🔵 D. Realtime chưa consume (còn lại)

- [x] **RoomHub `UserJoinedMedia` / `UserLeftMedia`** (S): participant media join/leave — ĐÃ consume (`17f5a71`) *(room membership join/leave cũng ĐÃ consume rồi).*
- [x] **Feed `NewComment` / `NewReaction`** — backend (`3d474a2`) + FE consume (`508e3d5`) đều xong.

---

## ⚪ E. Ngoài tầm / cần config ngoài

- [ ] **Google OAuth thật** (M): `POST /Auth/oauth/google` có, FE chỉ mock button → cần `GoogleClientId` + Google Identity SDK.
- [ ] **LiveKit webhook** — server-only (không cần FE).
- [x] **VocabCard post type** (M): composer + flip-card render ĐÃ xong (`5add71a`+`bc97433`). Backend support `PostType.VocabCard` + JSON metadata từ đầu; FE giờ tạo + render end-to-end.
- [ ] **Poll post type** (M): backend enum có `Poll` + metadata JSON, nhưng cần vote endpoint + lưu vote state (contract: option shape + vote mutation) trước khi làm UI tạo/vote.
- [x] **Media-upload post type** (L): ĐÃ xong (`8f2696b`+`bf64ccb`) — upload endpoint thật (`POST /Feed/posts/media`, `POST /Users/me/avatar/upload`) + `IStorageService` generalize (`UploadAsync(category,...)`) + `LocalStorageService` persist thật `wwwroot/uploads/...`. Composer gallery + PostCard render + avatar upload UI; thêm `MediaItems` vào `PostSummaryDto`. Limit: Phase 2 Azure Blob (DI unchanged) + (optional) `MediaType` column nếu URL mất ext.

---

## Gợi ý thứ tự tiếp theo

1. **A — Moderation console** (lớn nhất, đóng gap admin hoàn chỉnh).
2. **B — nhóm quick wins** (invite/mute, cancel friend request, block list, post detail) — FE-mostly, ROI cao.
3. **C/D — feed live-comments** (cần backend) + media session mgmt.
4. **E — Google OAuth / post types** (cần config/contract ngoài).
