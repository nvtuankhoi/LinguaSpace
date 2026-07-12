# BACKLOG — Frontend còn thiếu so với Backend

Cập nhật: 2026-07-12 · Branch: `feat/edit-delete-flows`

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
- **Edit/delete realtime sync (DM + room)** (`037543f`): edit/delete DM và delete room message giờ sync live cho participant/tab khác (broadcast `DirectMessageEdited`/`DirectMessageDeleted` qua PresenceHub + `MessageDeleted` qua RoomHub). Feed post/comment edit/delete sync còn thiếu — cần "post-group broadcasting" (mục C).
- **Live media presence** (`17f5a71`): xem ai đang trong voice/video call live — seed qua `GET /media/participants` khi mở room + consume `UserJoinedMedia`/`UserLeftMedia` realtime (badge "In call" trong manage modal + đếm "N in call" ở av-bar). Backend broadcast sẵn qua LiveKit webhook nên FE-only.

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

- [ ] **Email verify-token** (S): `POST /Auth/verify-email` — có "resend verification" nhưng chưa có UI nhập token / consume link verify.
- [ ] **FCM push** (M): `POST /Auth/device-token` — chưa wire (cần FCM SDK + registration).
- [ ] **Media session mgmt** (M): `GET /media/participants` ĐÃ wire (seed in-call set, `17f5a71`); còn `GET .../media/status` + `DELETE .../media` (host end-call) chưa gọi.
- [ ] **Feed live comments/reactions** (L): khi đang xem 1 post, comment/reaction mới của người khác hiện live. Cần **backend post-group broadcasting** (`NewComment`/`NewReaction` hiện chỉ author-targeted). *(Đã làm được: following-feed new posts live.)*

---

## 🔵 D. Realtime chưa consume (còn lại)

- [x] **RoomHub `UserJoinedMedia` / `UserLeftMedia`** (S): participant media join/leave — ĐÃ consume (`17f5a71`) *(room membership join/leave cũng ĐÃ consume rồi).*
- [ ] **Feed `NewComment` / `NewReaction`** cho viewer — xem mục C (cần backend post-group).

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
