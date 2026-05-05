# LinguaSpace API — Bruno Collection

[Bruno](https://www.usebruno.com/) là API client miễn phí, lưu collection dưới dạng file text (commit được vào git).

## Cài đặt Bruno

Tải tại: https://www.usebruno.com/downloads

## Mở collection

1. Mở Bruno → **Open Collection** → chọn thư mục `bruno/`
2. Chọn environment **Local** (góc trên phải)

## Luồng test cơ bản

### 1. Chạy app
```bash
dotnet run --project src\AppHost
```
Xem port của Web API trong Aspire dashboard (`http://localhost:15888`), cập nhật `baseUrl` trong **Local** environment.

### 2. Auth flow
| Bước | Request | Ghi chú |
|------|---------|---------|
| 1 | `Auth/01_Register` | Tạo tài khoản mới |
| 2 | `Auth/02_Login` | Lấy `accessToken` (tự lưu vào env var) |
| 3 | Bất kỳ request nào | Dùng `{{accessToken}}` Bearer token |

> Script post-response tự động lưu `accessToken` và `userId` vào environment sau khi login.

### 3. Test các feature
- **Users**: Update profile → Add language → Search users → Send friend request
- **Rooms**: Create room → Join room (lấy LiveKit token)
- **Feed**: Create post → Explore → Add comment
- **Messages**: Send DM → Get history

## Environment variables

| Variable | Mô tả |
|----------|-------|
| `baseUrl` | URL của Web API (vd: `https://localhost:7001`) |
| `accessToken` | JWT access token (tự set sau Login) |
| `userId` | User ID của tài khoản đang test (tự set sau Login) |
| `roomId` | Room ID (tự set sau Create Room) |
| `postId` | Post ID (tự set sau Create Post) |

## Lưu ý

- **Refresh token**: Lưu trong HttpOnly cookie — Bruno tự gửi cookie khi call `/api/Auth/refresh`
- **HTTPS**: App dùng self-signed cert trong dev → Bruno cần bật "Disable SSL Verification" (Settings > Preferences)
