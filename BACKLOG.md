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

## 🟡 B. API có sẵn, thiếu UI (quick wins)

- [ ] **Host controls** (M): `invite` (`POST /Rooms/{id}/invite/{userId}`), `mute` (`POST /Rooms/{id}/mute/{userId}`) — `rooms.api` đã có method, `room-detail` chưa có nút. *(Lưu ý: `kick` đã có UI ở host modal.)*
- [ ] **Hủy friend request** (S): `DELETE /Users/friend-requests/{id}` — api có, chưa có nút hủy lời mời đang chờ.
- [ ] **Trang Block list** (S): `GET /Users/me/blocked` — api có, chưa có trang quản lý user đã block.
- [ ] **Post detail page** (M): `GET /Feed/posts/{id}` (`getPost`) — api có, chưa có trang riêng (đang xem trong feed).

---

## 🟠 C. Thiếu hoàn toàn (BE có, FE chưa có API lẫn UI)

- [ ] **Email verify-token** (S): `POST /Auth/verify-email` — có "resend verification" nhưng chưa có UI nhập token / consume link verify.
- [ ] **FCM push** (M): `POST /Auth/device-token` — chưa wire (cần FCM SDK + registration).
- [ ] **Media session mgmt** (M): `GET /Rooms/{roomId}/media/participants`, `GET .../media/status`, `DELETE .../media` — room đang dùng LiveKit client trực tiếp, 3 endpoint này chưa gọi.
- [ ] **Feed live comments/reactions** (L): khi đang xem 1 post, comment/reaction mới của người khác hiện live. Cần **backend post-group broadcasting** (`NewComment`/`NewReaction` hiện chỉ author-targeted). *(Đã làm được: following-feed new posts live.)*

---

## 🔵 D. Realtime chưa consume (còn lại)

- [ ] **RoomHub `UserJoinedMedia` / `UserLeftMedia`** (S): participant media join/leave — chưa subscribe *(room membership join/leave ĐÃ consume rồi).*
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
