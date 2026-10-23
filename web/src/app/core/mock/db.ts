import type {
  LanguageLevel,
  LanguageType,
  NotificationType,
  PostMetadataDto,
  PostType,
  ReactionType,
} from '../models';

export interface MockUser {
  userId: string;
  email: string;
  password: string;
  displayName: string;
  avatarUrl: string | null;
  bio: string | null;
  timezone: string | null;
  isOnline: boolean;
  lastSeenAt: string | null;
  languages: { id: number; languageCode: string; type: LanguageType; level: LanguageLevel | null }[];
  followerCount: number;
  followingCount: number;
  friendCount: number;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  lastActivityAt: string | null;
  badges: {
    badgeId: number;
    code: string;
    name: string;
    description: string | null;
    iconUrl: string | null;
    earnedAt: string;
  }[];
}

export interface MockComment {
  id: number;
  postId: number;
  authorId: string;
  content: string;
  parentCommentId: number | null;
  createdAt: string;
  reactions: { userId: string; type: ReactionType }[];
}

export interface MockPost {
  id: number;
  authorId: string;
  content: string;
  postType: PostType;
  languageCode: string | null;
  metadata: PostMetadataDto | null;
  tags: string[];
  createdAt: string;
  comments: MockComment[];
  reactions: { userId: string; type: ReactionType }[];
  mediaItems: { id: number; url: string; sortOrder: number }[];
}

// Relative timestamps seeded at module load (runtime, so Date is fine here).
const NOW = Date.now();
const hours = (h: number): string => new Date(NOW - h * 3_600_000).toISOString();
const mins = (m: number): string => new Date(NOW - m * 60_000).toISOString();

/**
 * Mock session, backed by localStorage so it survives page reloads — just as a
 * real HttpOnly refresh-token cookie would. The access token itself stays
 * in-memory (see TokenService); only the persistent "you are signed in" flag is
 * stored here, so a reload can restore the session via /Auth/me + /Auth/refresh.
 */
const SESSION_KEY = 'ls_mock_session';
const readSession = (): string | null => {
  try {
    return localStorage.getItem(SESSION_KEY);
  } catch {
    return null;
  }
};
const writeSession = (value: string | null): void => {
  try {
    if (value === null) {
      localStorage.removeItem(SESSION_KEY);
    } else {
      localStorage.setItem(SESSION_KEY, value);
    }
  } catch {
    /* storage unavailable (private mode) — degrade to in-memory */
  }
};

export const session = {
  get userId(): string | null {
    return readSession();
  },
  set userId(value: string | null) {
    writeSession(value);
  },
};

export interface MockParticipant {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  role: 'Host' | 'Speaker' | 'Listener';
  joinedAt: string;
}

export interface MockRoomMessage {
  id: number;
  roomId: number;
  senderId: string;
  senderDisplayName: string;
  content: string;
  type: 'Text' | 'System';
  sentAt: string;
  isDeleted: boolean;
}

export interface MockRoom {
  id: number;
  title: string;
  description: string | null;
  languageCode: string;
  maxParticipants: number;
  status: 'Active' | 'Closed';
  roomType: 'TextOnly' | 'VoiceOnly' | 'VideoEnabled';
  hostId: string;
  created: string;
  participants: MockParticipant[];
  messages: MockRoomMessage[];
}

export interface MockDm {
  id: number;
  conversationId: number;
  senderId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
  isDeleted: boolean;
  editedAt: string | null;
}

export interface MockConversation {
  id: number;
  /** The two participants, unordered. The "other" user is derived from session.userId. */
  participantIds: [string, string];
  messages: MockDm[];
}

export interface MockNotification {
  id: number;
  type: NotificationType;
  payload: Record<string, unknown> | null;
  isRead: boolean;
  createdAt: string;
}

const roomsSeed: MockRoom[] = [
  {
    id: 1, title: 'Japanese B1 practice circle', description: 'Low-pressure speaking. Pausing is fine.',
    languageCode: 'ja', maxParticipants: 8, status: 'Active', roomType: 'VoiceOnly', hostId: 'u-3', created: hours(50),
    participants: [
      { userId: 'u-3', displayName: 'Sora', avatarUrl: null, role: 'Host', joinedAt: hours(2) },
      { userId: 'u-2', displayName: 'Marco', avatarUrl: null, role: 'Speaker', joinedAt: hours(1) },
      { userId: 'u-1', displayName: 'Aoi', avatarUrl: null, role: 'Listener', joinedAt: mins(40) },
    ],
    messages: [
      { id: 101, roomId: 1, senderId: 'u-3', senderDisplayName: 'Sora', content: 'Welcome in. We take turns — no pressure to be perfect.', type: 'Text', sentAt: hours(2), isDeleted: false },
      { id: 102, roomId: 1, senderId: 'u-2', senderDisplayName: 'Marco', content: 'ありがとうございます！ちょっと緊張しています。', type: 'Text', sentAt: mins(90), isDeleted: false },
    ],
  },
  {
    id: 2, title: 'Spanish warm-up', description: 'Quick intros in Spanish, then a free chat.',
    languageCode: 'es', maxParticipants: 6, status: 'Active', roomType: 'VideoEnabled', hostId: 'u-1', created: hours(20),
    participants: [
      { userId: 'u-1', displayName: 'Aoi', avatarUrl: null, role: 'Host', joinedAt: hours(1) },
      { userId: 'u-4', displayName: 'Lena', avatarUrl: null, role: 'Speaker', joinedAt: mins(30) },
    ],
    messages: [
      { id: 201, roomId: 2, senderId: 'u-1', senderDisplayName: 'Aoi', content: '¡Hola a todos! Empecemos con una presentación corta.', type: 'Text', sentAt: hours(1), isDeleted: false },
    ],
  },
  {
    id: 3, title: 'German reading club', description: 'We read a short text together and unpack it.',
    languageCode: 'de', maxParticipants: 12, status: 'Active', roomType: 'TextOnly', hostId: 'u-4', created: hours(72),
    participants: [
      { userId: 'u-4', displayName: 'Lena', avatarUrl: null, role: 'Host', joinedAt: hours(3) },
      { userId: 'u-5', displayName: 'Jin', avatarUrl: null, role: 'Listener', joinedAt: hours(2) },
    ],
    messages: [
      { id: 301, roomId: 3, senderId: 'u-4', senderDisplayName: 'Lena', content: 'Heute lesen wir eine kurze Geschichte. Seite 1 zuerst.', type: 'Text', sentAt: hours(3), isDeleted: false },
    ],
  },
];

const conversationsSeed: MockConversation[] = [
  {
    id: 1,
    participantIds: ['u-1', 'u-2'],
    messages: [
      { id: 11, conversationId: 1, senderId: 'u-2', content: 'Aoi! あの、明日の会話の練習、まだ大丈夫？', sentAt: hours(6), isRead: true, isDeleted: false, editedAt: null },
      { id: 12, conversationId: 1, senderId: 'u-1', content: 'はい、もちろん！20時でどう？', sentAt: hours(5), isRead: true, isDeleted: false, editedAt: null },
      { id: 13, conversationId: 1, senderId: 'u-2', content: 'Perfect. I will bring the vocab list we made.', sentAt: mins(45), isRead: false, isDeleted: false, editedAt: null },
    ],
  },
  {
    id: 2,
    participantIds: ['u-1', 'u-4'],
    messages: [
      { id: 21, conversationId: 2, senderId: 'u-4', content: 'Danke for the vocab card today — I used "Ausnahme" three times 😄', sentAt: hours(3), isRead: true, isDeleted: false, editedAt: null },
      { id: 22, conversationId: 2, senderId: 'u-1', content: 'Haha, that is how it sticks. Every exception is a win.', sentAt: hours(2), isRead: true, isDeleted: false, editedAt: null },
    ],
  },
  {
    id: 3,
    participantIds: ['u-1', 'u-3'],
    messages: [
      { id: 31, conversationId: 3, senderId: 'u-3', content: 'I opened a B1–B2 English room tonight if you want to drop by.', sentAt: hours(8), isRead: true, isDeleted: false, editedAt: null },
      { id: 32, conversationId: 3, senderId: 'u-1', content: 'Maybe later — practising Spanish first tonight. Thank you!', sentAt: hours(7), isRead: true, isDeleted: false, editedAt: null },
    ],
  },
];

const notificationsSeed: MockNotification[] = [
  {
    id: 1, type: 'FriendRequest',
    payload: { requesterId: 'u-5', requesterDisplayName: 'Jin', requestId: 10 },
    isRead: false, createdAt: mins(15),
  },
  {
    id: 2, type: 'PostLike',
    payload: { postId: 4, likerId: 'u-3', likerDisplayName: 'Sora' },
    isRead: false, createdAt: mins(42),
  },
  {
    id: 3, type: 'PostComment',
    payload: { postId: 4, commentId: 41, commenterId: 'u-2', commenterDisplayName: 'Marco', commentPreview: 'Needed this today. Thank you.' },
    isRead: false, createdAt: hours(1),
  },
  {
    id: 4, type: 'NewFollower',
    payload: { followerId: 'u-4', followerDisplayName: 'Lena' },
    isRead: true, createdAt: hours(3),
  },
  {
    id: 5, type: 'RoomInvite',
    payload: { roomId: 1, roomTitle: 'Japanese B1 practice circle', inviterId: 'u-3', inviterDisplayName: 'Sora' },
    isRead: true, createdAt: hours(5),
  },
  {
    id: 6, type: 'FriendAccepted',
    payload: { acceptorId: 'u-2', acceptorDisplayName: 'Marco' },
    isRead: true, createdAt: hours(8),
  },
  {
    id: 7, type: 'BadgeEarned',
    payload: { badgeId: 1, badgeName: 'First conversation', badgeIconUrl: null },
    isRead: true, createdAt: hours(24),
  },
  {
    id: 8, type: 'SystemMessage',
    payload: { message: 'Welcome to LinguaSpace! Start by joining a room or following a learner.', actionUrl: '/app/rooms' },
    isRead: true, createdAt: hours(48),
  },
];

export const db: { users: MockUser[]; posts: MockPost[]; rooms: MockRoom[]; conversations: MockConversation[]; notifications: MockNotification[] } = {
  conversations: conversationsSeed,
  rooms: roomsSeed,
  notifications: notificationsSeed,
  users: [
    {
      userId: 'u-1', email: 'demo@lingua.space', password: 'Password1!', displayName: 'Aoi',
      avatarUrl: null, bio: 'Practising Spanish and Japanese. Slow is fine.', timezone: null,
      isOnline: true, lastSeenAt: null,
      languages: [
        { id: 1, languageCode: 'ja', type: 'Native', level: null },
        { id: 2, languageCode: 'es', type: 'Learning', level: 'B1' },
        { id: 3, languageCode: 'en', type: 'Learning', level: 'C1' },
      ],
      followerCount: 42, followingCount: 31, friendCount: 12,
      totalXp: 1240, currentStreak: 5, longestStreak: 12, lastActivityAt: hours(2),
      badges: [
        { badgeId: 1, code: 'first-conversation', name: 'First conversation', description: 'Joined your first practice room.', iconUrl: null, earnedAt: hours(120) },
        { badgeId: 2, code: 'week-streak', name: 'Seven-day streak', description: 'Practised seven days in a row.', iconUrl: null, earnedAt: hours(72) },
        { badgeId: 3, code: 'polyglot', name: 'Three languages', description: 'Added three languages to your profile.', iconUrl: null, earnedAt: hours(30) },
      ],
    },
    {
      userId: 'u-2', email: 'marco@example.com', password: 'x', displayName: 'Marco',
      avatarUrl: null, bio: 'Ciao! Learning Japanese, happy to help with Italian.', timezone: null,
      isOnline: true, lastSeenAt: null,
      languages: [
        { id: 4, languageCode: 'it', type: 'Native', level: null },
        { id: 5, languageCode: 'ja', type: 'Learning', level: 'A2' },
      ],
      followerCount: 88, followingCount: 54, friendCount: 20,
      totalXp: 2100, currentStreak: 9, longestStreak: 15, lastActivityAt: hours(1),
      badges: [
        { badgeId: 1, code: 'first-conversation', name: 'First conversation', description: 'Joined your first practice room.', iconUrl: null, earnedAt: hours(200) },
        { badgeId: 4, code: 'helper', name: 'Helped ten learners', description: 'Gave feedback that helped ten people.', iconUrl: null, earnedAt: hours(96) },
      ],
    },
    {
      userId: 'u-3', email: 'sora@example.com', password: 'x', displayName: 'Sora',
      avatarUrl: null, bio: 'English ↔ Japanese exchange.', timezone: null,
      isOnline: false, lastSeenAt: hours(3),
      languages: [
        { id: 6, languageCode: 'ja', type: 'Native', level: null },
        { id: 7, languageCode: 'en', type: 'Learning', level: 'B2' },
      ],
      followerCount: 133, followingCount: 90, friendCount: 41,
      totalXp: 3450, currentStreak: 22, longestStreak: 30, lastActivityAt: hours(5),
      badges: [
        { badgeId: 1, code: 'first-conversation', name: 'First conversation', description: 'Joined your first practice room.', iconUrl: null, earnedAt: hours(500) },
        { badgeId: 2, code: 'week-streak', name: 'Seven-day streak', description: 'Practised seven days in a row.', iconUrl: null, earnedAt: hours(300) },
        { badgeId: 4, code: 'helper', name: 'Helped ten learners', description: 'Gave feedback that helped ten people.', iconUrl: null, earnedAt: hours(200) },
        { badgeId: 3, code: 'polyglot', name: 'Three languages', description: 'Added three languages to your profile.', iconUrl: null, earnedAt: hours(120) },
        { badgeId: 5, code: 'early-bird', name: 'Early bird', description: 'Practised before 8am.', iconUrl: null, earnedAt: hours(60) },
      ],
    },
    {
      userId: 'u-4', email: 'lena@example.com', password: 'x', displayName: 'Lena',
      avatarUrl: null, bio: 'Deutsch lernen, one sentence a day.', timezone: null,
      isOnline: true, lastSeenAt: null,
      languages: [
        { id: 8, languageCode: 'es', type: 'Native', level: null },
        { id: 9, languageCode: 'de', type: 'Learning', level: 'B1' },
      ],
      followerCount: 57, followingCount: 22, friendCount: 9,
      totalXp: 980, currentStreak: 3, longestStreak: 5, lastActivityAt: mins(30),
      badges: [
        { badgeId: 1, code: 'first-conversation', name: 'First conversation', description: 'Joined your first practice room.', iconUrl: null, earnedAt: hours(80) },
      ],
    },
    {
      userId: 'u-5', email: 'jin@example.com', password: 'x', displayName: 'Jin',
      avatarUrl: null, bio: 'Korean native, studying French.', timezone: null,
      isOnline: false, lastSeenAt: hours(20),
      languages: [
        { id: 10, languageCode: 'ko', type: 'Native', level: null },
        { id: 11, languageCode: 'fr', type: 'Learning', level: 'A2' },
      ],
      followerCount: 71, followingCount: 40, friendCount: 15,
      totalXp: 1560, currentStreak: 0, longestStreak: 8, lastActivityAt: hours(48),
      badges: [
        { badgeId: 1, code: 'first-conversation', name: 'First conversation', description: 'Joined your first practice room.', iconUrl: null, earnedAt: hours(150) },
        { badgeId: 3, code: 'polyglot', name: 'Three languages', description: 'Added three languages to your profile.', iconUrl: null, earnedAt: hours(40) },
      ],
    },
  ],

  posts: [
    {
      id: 1, authorId: 'u-2', content: 'Just finished my first 10-minute conversation entirely in Japanese. I paused a lot, but I held it. Small win. 🌅',
      postType: 'Text', languageCode: 'ja', metadata: null, tags: ['journal', 'speaking'],
      createdAt: mins(24),
      comments: [
        { id: 11, postId: 1, authorId: 'u-1', content: 'That is huge — pausing is not failure, it is thinking. Congrats!', parentCommentId: null, createdAt: mins(18), reactions: [] },
        { id: 12, postId: 1, authorId: 'u-3', content: '10 minutes fully in Japanese is no joke. Well done.', parentCommentId: null, createdAt: mins(12), reactions: [] },
      ],
      reactions: [{ userId: 'u-1', type: 'Like' }, { userId: 'u-3', type: 'Love' }, { userId: 'u-4', type: 'Like' }],
      mediaItems: [],
    },
    {
      id: 2, authorId: 'u-4', content: 'Vocab of the day: "die Ausnahme" (the exception). Use it in a reply — I will correct gently.',
      postType: 'VocabCard', languageCode: 'de',
      metadata: null,
      tags: ['vocab', 'deutsch'],
      createdAt: hours(2),
      comments: [
        { id: 21, postId: 2, authorId: 'u-5', content: 'Jede Regel hat eine Ausnahme, oder?', parentCommentId: null, createdAt: hours(1), reactions: [{ userId: 'u-4', type: 'Like' }] },
      ],
      reactions: [{ userId: 'u-1', type: 'Like' }, { userId: 'u-5', type: 'Like' }],
      mediaItems: [],
    },
    {
      id: 3, authorId: 'u-3', content: 'Anyone up for a low-pressure English↔Japanese room tonight? Thinking 8 people max, B1–B2 level. No pressure to speak perfectly.',
      postType: 'Text', languageCode: 'en', metadata: null, tags: ['rooms'],
      createdAt: hours(5),
      comments: [],
      reactions: [{ userId: 'u-1', type: 'Love' }, { userId: 'u-2', type: 'Like' }, { userId: 'u-4', type: 'Like' }, { userId: 'u-5', type: 'Like' }],
      mediaItems: [],
    },
    {
      id: 4, authorId: 'u-1', content: 'Reminder to myself: consistency beats intensity. Five quiet minutes a day compounds. Posting here to stay accountable. 🌱',
      postType: 'Text', languageCode: 'en', metadata: null, tags: ['mindset'],
      createdAt: hours(9),
      comments: [
        { id: 41, postId: 4, authorId: 'u-2', content: 'Needed this today. Thank you.', parentCommentId: null, createdAt: hours(8), reactions: [] },
      ],
      reactions: [{ userId: 'u-3', type: 'Like' }],
      mediaItems: [],
    },
    {
      id: 5, authorId: 'u-5', content: 'Question for French learners: how do you keep "imparfait" and "passé composé" straight when speaking? I freeze every time.',
      postType: 'Text', languageCode: 'fr', metadata: null, tags: ['question', 'grammar'],
      createdAt: hours(26),
      comments: [
        { id: 51, postId: 5, authorId: 'u-1', content: 'I think of imparfait as the "weather" (ongoing background) and passé composé as the "event". Helped me.', parentCommentId: null, createdAt: hours(24), reactions: [{ userId: 'u-5', type: 'Like' }] },
      ],
      reactions: [{ userId: 'u-4', type: 'Like' }],
      mediaItems: [],
    },
    {
      id: 6, authorId: 'u-3', content: 'Reading tip: short stories beat novels early on. You finish something, and finishing is motivating.',
      postType: 'Text', languageCode: 'en', metadata: null, tags: ['reading'],
      createdAt: hours(30),
      comments: [],
      reactions: [{ userId: 'u-2', type: 'Like' }, { userId: 'u-1', type: 'Like' }],
      mediaItems: [],
    },
  ],
};

let nextPostId = 100;
let nextCommentId = 1000;
let nextRoomId = 100;
let nextMessageId = 5000;
let nextDmId = 6000;
let nextConversationId = 100;
let nextNotificationId = 100;
export const ids = {
  post: () => nextPostId++,
  comment: () => nextCommentId++,
  room: () => nextRoomId++,
  message: () => nextMessageId++,
  dm: () => nextDmId++,
  conversation: () => nextConversationId++,
  notification: () => nextNotificationId++,
};
