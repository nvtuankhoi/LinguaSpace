export interface ConversationDto {
  id: number;
  otherUserId: string;
  otherUserDisplayName: string | null;
  otherUserAvatarUrl: string | null;
  lastMessage: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
}

export interface DirectMessageDto {
  id: number;
  conversationId: number;
  senderId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
  isDeleted: boolean;
  editedAt: string | null;
}

export interface ConversationsUnreadCountDto {
  unreadConversations: number;
  totalUnread: number;
}

export interface SendDmRequest {
  recipientId: string;
  content: string;
}

export interface UpdateMessageRequest {
  content: string;
}
