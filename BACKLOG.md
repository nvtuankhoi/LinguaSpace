# BACKLOG — Frontend còn thiếu so với Backend

Cập nhật: 2026-07-13 · Branch: `feat/edit-delete-flows`

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
- [ ] **VocabCard / Poll / media-upload post types** (M/L): composer chỉ tạo `Text` post. Cần xác nhận `design/api-contract.md` có postType poll/vocab/media hay không trước khi làm UI tạo.

---

## Gợi ý thứ tự tiếp theo

1. **A — Moderation console** (lớn nhất, đóng gap admin hoàn chỉnh).
2. **B — nhóm quick wins** (invite/mute, cancel friend request, block list, post detail) — FE-mostly, ROI cao.
3. **C/D — feed live-comments** (cần backend) + media session mgmt.
4. **E — Google OAuth / post types** (cần config/contract ngoài).
