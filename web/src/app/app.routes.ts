import { Routes } from '@angular/router';

import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent),
    title: 'LinguaSpace — practise languages together',
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
    title: 'Log in — LinguaSpace',
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent),
    title: 'Create account — LinguaSpace',
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/reset-password.component').then((m) => m.ResetPasswordComponent),
    title: 'Reset password — LinguaSpace',
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', redirectTo: 'feed', pathMatch: 'full' },
      {
        path: 'feed',
        title: 'Feed — LinguaSpace',
        loadComponent: () => import('./features/feed/feed.component').then((m) => m.FeedComponent),
      },
      {
        path: 'post/:id',
        title: 'Post — LinguaSpace',
        loadComponent: () => import('./features/feed/post-detail.component').then((m) => m.PostDetailComponent),
      },
      {
        path: 'rooms',
        title: 'Rooms — LinguaSpace',
        loadComponent: () => import('./features/rooms/rooms-list.component').then((m) => m.RoomsListComponent),
      },
      {
        path: 'rooms/:id',
        title: 'Room — LinguaSpace',
        loadComponent: () => import('./features/rooms/room-detail.component').then((m) => m.RoomDetailComponent),
      },
      {
        path: 'messages',
        title: 'Messages — LinguaSpace',
        loadComponent: () => import('./features/messages/messages-list.component').then((m) => m.MessagesListComponent),
      },
      {
        path: 'messages/:id',
        title: 'Conversation — LinguaSpace',
        loadComponent: () => import('./features/messages/message-thread.component').then((m) => m.MessageThreadComponent),
      },
      {
        path: 'notifications',
        title: 'Notifications — LinguaSpace',
        loadComponent: () => import('./features/notifications/notifications.component').then((m) => m.NotificationsComponent),
      },
      {
        path: 'leaderboard',
        title: 'Leaderboard — LinguaSpace',
        loadComponent: () => import('./features/gamification/leaderboard.component').then((m) => m.LeaderboardComponent),
      },
      {
        path: 'friends',
        title: 'Network — LinguaSpace',
        loadComponent: () => import('./features/friends/friends.component').then((m) => m.FriendsComponent),
      },
      {
        path: 'search',
        title: 'Search — LinguaSpace',
        loadComponent: () => import('./features/search/search.component').then((m) => m.SearchComponent),
      },
      {
        path: 'profile/:userId',
        title: 'Learner Profile — LinguaSpace',
        loadComponent: () => import('./features/profile/public-profile.component').then((m) => m.PublicProfileComponent),
      },
      {
        path: 'settings',
        title: 'Settings — LinguaSpace',
        loadComponent: () => import('./features/settings/settings.component').then((m) => m.SettingsComponent),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        title: 'Moderation — LinguaSpace',
        loadComponent: () => import('./features/admin/moderation.component').then((m) => m.ModerationComponent),
      },
      {
        path: 'profile',
        data: { label: 'Profile' },
        title: 'Profile — LinguaSpace',
        loadComponent: () => import('./features/profile/profile.component').then((m) => m.ProfileComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
