import type { DevicePlatform } from './enums';

export interface AuthResponseDto {
  accessToken: string;
  expiresIn: number;
  userId: string;
  email: string;
}

export interface TokenResponseDto {
  accessToken: string;
  expiresIn: number;
}

export interface CurrentUserDto {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
  avatarUrl: string | null;
  isEmailConfirmed: boolean;
}

export interface ActiveSessionDto {
  id: number;
  deviceInfo: string | null;
  ipAddress: string | null;
  createdAt: string;
  lastActiveAt: string;
}

export interface RegisterResult {
  userId: string;
  email: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface GoogleOAuthRequest {
  idToken: string;
}

export interface DeviceTokenRequest {
  fcmToken: string;
  platform: DevicePlatform;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ChangeEmailRequest {
  newEmail: string;
  password: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface VerifyEmailRequest {
  token: string;
}
