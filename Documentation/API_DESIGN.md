# ChatApp — REST API & SignalR Real-Time Protocol Specification

**Document Version:** 1.0.0  
**Phase:** 1 — Product Discovery & Architecture  
**Base REST URL:** `https://api.chatapp.com/api/v1` (Local: `https://localhost:7001/api/v1`)  
**SignalR Hub URL:** `https://api.chatapp.com/hubs/chat` (Local: `https://localhost:7001/hubs/chat`)  
**Specification Standard:** OpenAPI 3.1 / RFC 7807 Problem Details  

---

## 1. API Standards & Response Conventions

### 1.1 Standard JSON Response Envelopes

#### Success Response (`ApiResponse<T>`)
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "timestamp": "2026-11-18T21:05:00.123Z"
}
```

#### Paged Response (`PagedResponse<T>`)
```json
{
  "success": true,
  "data": [ ... ],
  "pagination": {
    "pageIndex": 1,
    "pageSize": 30,
    "totalCount": 142,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false,
    "nextCursor": "2026-11-18T20:55:00.000Z"
  },
  "timestamp": "2026-11-18T21:05:00.123Z"
}
```

#### Standard Error Response (RFC 7807 `ProblemDetails`)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/v1/auth/register",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00",
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."],
    "Password": ["Password must contain at least one uppercase letter."]
  }
}
```

---

## 2. REST API Endpoints Catalogue

### 2.1 Authentication Module (`/api/v1/auth`)

#### 1. Register User
- **Method:** `POST /api/v1/auth/register`
- **Auth:** Anonymous
- **Request Body:**
```json
{
  "fullName": "Abdullah Ahmed",
  "userName": "abdullah",
  "email": "abdullah@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```
- **Response `201 Created`:**
```json
{
  "success": true,
  "data": {
    "userId": "d3b07384-d113-4611-9c88-e24c5b369c01",
    "userName": "abdullah",
    "email": "abdullah@example.com",
    "fullName": "Abdullah Ahmed",
    "accessToken": "eyJhbGciOiJIUzI1Ni...",
    "refreshToken": "4a189f78-2c35-4309-98...",
    "expiresAt": "2026-11-18T21:20:00Z"
  }
}
```
- **Status Codes:** `201 Created`, `400 Bad Request` (Validation), `409 Conflict` (Duplicate email/username).

#### 2. User Login
- **Method:** `POST /api/v1/auth/login`
- **Auth:** Anonymous
- **Request Body:**
```json
{
  "userNameOrEmail": "abdullah",
  "password": "SecurePassword123!"
}
```
- **Response `200 OK`:** Same token payload as Register.
- **Status Codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`.

#### 3. Refresh Access Token
- **Method:** `POST /api/v1/auth/refresh`
- **Auth:** Anonymous
- **Request Body:**
```json
{
  "refreshToken": "4a189f78-2c35-4309-98..."
}
```
- **Response `200 OK`:** Rotated Access Token & New Refresh Token.
- **Status Codes:** `200 OK`, `401 Unauthorized` (Invalid, expired, or reused token).

#### 4. Logout (Revoke Token)
- **Method:** `POST /api/v1/auth/logout`
- **Auth:** Bearer Token
- **Request Body:**
```json
{
  "refreshToken": "4a189f78-2c35-4309-98..."
}
```
- **Response `204 No Content`**

---

### 2.2 Users Module (`/api/v1/users`)

#### 1. Get Current User Profile
- **Method:** `GET /api/v1/users/me`
- **Auth:** Bearer Token
- **Response `200 OK`:**
```json
{
  "success": true,
  "data": {
    "id": "d3b07384-d113-4611-9c88-e24c5b369c01",
    "userName": "abdullah",
    "email": "abdullah@example.com",
    "fullName": "Abdullah Ahmed",
    "bio": "Available for chat",
    "avatarUrl": "https://storage.chatapp.com/avatars/user1.webp",
    "isOnline": true,
    "lastSeenAt": "2026-11-18T21:00:00Z"
  }
}
```

#### 2. Update Profile Details
- **Method:** `PUT /api/v1/users/me`
- **Auth:** Bearer Token
- **Request Body:**
```json
{
  "fullName": "Abdullah A. Ahmed",
  "bio": "Coding with .NET 10"
}
```
- **Response `200 OK`**

#### 3. Search Users
- **Method:** `GET /api/v1/users/search?q={query}&pageIndex=1&pageSize=20`
- **Auth:** Bearer Token
- **Response `200 OK`:** `PagedResponse<UserProfileDto>`

---

### 2.3 Conversations Module (`/api/v1/conversations`)

#### 1. List User Conversations
- **Method:** `GET /api/v1/conversations?pageIndex=1&pageSize=30`
- **Auth:** Bearer Token
- **Response `200 OK`:**
```json
{
  "success": true,
  "data": [
    {
      "id": "7f8b3c21-1234-4567-89ab-cdef01234567",
      "type": 1,
      "title": "Sarah Connor",
      "avatarUrl": "https://storage.chatapp.com/avatars/sarah.webp",
      "lastMessage": {
        "id": "99887766-5544-3322-1100-aabbccddeeff",
        "senderId": "e5f6g7h8-...",
        "content": "See you at the meeting!",
        "messageType": 1,
        "sentAt": "2026-11-18T20:58:12Z",
        "status": 3
      },
      "unreadCount": 2,
      "isPinned": true,
      "isArchived": false,
      "isMuted": false
    }
  ]
}
```

#### 2. Start Direct 1-on-1 Conversation
- **Method:** `POST /api/v1/conversations/direct`
- **Auth:** Bearer Token
- **Request Body:**
```json
{
  "recipientUserId": "e5f6g7h8-1111-2222-3333-444455556666"
}
```
- **Response `200 OK` or `201 Created`:** Conversation model (returns existing if already created).

#### 3. Create Group Conversation
- **Method:** `POST /api/v1/conversations/group`
- **Auth:** Bearer Token
- **Request Body:**
```json
{
  "title": ".NET Architecture Core",
  "memberIds": [
    "e5f6g7h8-1111-2222-3333-444455556666",
    "a1b2c3d4-5555-6666-7777-888899990000"
  ]
}
```
- **Response `201 Created`**

#### 4. Pin / Archive Conversation
- **Method:** `PUT /api/v1/conversations/{id}/pin` (Body: `{"isPinned": true}`)
- **Method:** `PUT /api/v1/conversations/{id}/archive` (Body: `{"isArchived": true}`)
- **Response `200 OK`**

---

### 2.4 Messages Module (`/api/v1/conversations/{id}/messages`)

#### 1. Retrieve Messages (Cursor Pagination)
- **Method:** `GET /api/v1/conversations/{id}/messages?beforeCursor={timestamp}&pageSize=40`
- **Auth:** Bearer Token
- **Response `200 OK`:** Paged list of `MessageDto` items ordered newest to oldest.

#### 2. Send Message via REST Fallback
- **Method:** `POST /api/v1/conversations/{id}/messages`
- **Auth:** Bearer Token
- **Request Body:**
```json
{
  "clientMessageId": "b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6",
  "content": "Hello from offline fallback",
  "messageType": 1,
  "replyToMessageId": null
}
```
- **Response `201 Created`**

#### 3. Edit Message
- **Method:** `PUT /api/v1/messages/{id}`
- **Auth:** Bearer Token
- **Request Body:** `{"newContent": "Updated text message"}`
- **Response `200 OK`**

#### 4. Delete Message
- **Method:** `DELETE /api/v1/messages/{id}?forEveryone=true`
- **Auth:** Bearer Token
- **Response `204 No Content`**

#### 5. Add / Remove Reaction
- **Method:** `POST /api/v1/messages/{id}/reactions` (Body: `{"emoji": "👍"}`)
- **Method:** `DELETE /api/v1/messages/{id}/reactions`
- **Response `200 OK`**

---

### 2.5 Media & Custom Stickers Module (`/api/v1/media` & `/api/v1/stickers`)

#### 1. Upload Media
- **Method:** `POST /api/v1/media/upload`
- **Content-Type:** `multipart/form-data`
- **Form Fields:** `file` (binary), `mediaType` (1: Image, 2: Video, 3: Audio, 4: Sticker)
- **Response `201 Created`:**
```json
{
  "success": true,
  "data": {
    "mediaId": "6c7d8e9f-...",
    "fileUrl": "https://storage.chatapp.com/media/202611/img_098.webp",
    "thumbnailUrl": "https://storage.chatapp.com/media/202611/thumb_098.webp",
    "contentType": "image/webp",
    "fileSizeBytes": 45210,
    "width": 1080,
    "height": 1080
  }
}
```

#### 2. Create Sticker Pack
- **Method:** `POST /api/v1/stickers/packs`
- **Request Body:** `{"name": "Office Memes", "isPublic": true, "coverImageUrl": "..."}`
- **Response `201 Created`**

#### 3. Add Custom Sticker to Pack
- **Method:** `POST /api/v1/stickers/packs/{id}/stickers`
- **Request Body:** `{"imageUrl": "https://storage.chatapp.com/stickers/stk_01.webp", "width": 512, "height": 512}`
- **Response `201 Created`**

---

## 3. SignalR Real-Time Protocol Specification

### 3.1 Hub Configuration
- **Hub Endpoint:** `/hubs/chat`
- **Authentication Handshake:**
  The SignalR client transmits the JWT access token in the query string `?access_token={jwt}` when establishing the WebSocket handshake.

### 3.2 Client-to-Server Methods (Invocations from Mobile App)

| Method Name | Parameters | Description |
|---|---|---|
| `JoinConversation` | `Guid conversationId` | Explicitly binds connection to conversation group stream |
| `LeaveConversation` | `Guid conversationId` | Unbinds connection from active conversation |
| `SendMessage` | `SendMessageHubDto dto` | Ingests message, saves to database, broadcasts to peers |
| `SendTypingIndicator` | `Guid conversationId, bool isTyping` | Broadcasts live typing pulse to peers in conversation |
| `MarkAsDelivered` | `Guid messageId` | Records packet reception on device; triggers double gray tick |
| `MarkAsRead` | `Guid conversationId, Guid messageId` | Records message view on device; triggers double blue tick |

#### `SendMessageHubDto` Payload
```csharp
public record SendMessageHubDto(
    Guid ConversationId,
    Guid ClientMessageId,
    string? Content,
    int MessageType,
    Guid? ReplyToMessageId,
    Guid? MediaAttachmentId
);
```

---

### 3.3 Server-to-Client Events (Broadcasts to Mobile App)

| Event Name | Argument Type | Description |
|---|---|---|
| `OnReceiveMessage` | `MessageDto` | Dispatches new incoming message to active participants |
| `OnMessageDelivered` | `MessageDeliveryEventDto` | Notifies sender of message delivery timestamp |
| `OnMessageRead` | `MessageReadEventDto` | Notifies sender that recipient opened conversation |
| `OnUserTyping` | `UserTypingEventDto` | Live typing indicator broadcast |
| `OnUserPresenceChanged`| `UserPresenceEventDto` | Broadcasts user online/offline status change |
| `OnMessageEdited` | `MessageEditedEventDto` | Broadcasts updated content and timestamp |
| `OnMessageDeleted` | `MessageDeletedEventDto` | Broadcasts soft-delete tombstone |
| `OnReactionUpdated` | `ReactionUpdatedEventDto` | Broadcasts added or removed emoji reaction |

#### Example Payloads

##### `OnReceiveMessage`
```json
{
  "id": "e4c5b369-1234-4567-89ab-cdef01234567",
  "conversationId": "7f8b3c21-1234-4567-89ab-cdef01234567",
  "senderId": "d3b07384-d113-4611-9c88-e24c5b369c01",
  "senderName": "Abdullah Ahmed",
  "senderAvatarUrl": "https://storage.chatapp.com/avatars/user1.webp",
  "content": "Are we ready for .NET 10 deployment?",
  "messageType": 1,
  "clientMessageId": "b1b2b3b4-c1c2-d1d2-e1e2-f1f2f3f4f5f6",
  "sentAt": "2026-11-18T21:10:00Z",
  "status": 1,
  "replyTo": null,
  "media": null
}
```

##### `OnUserTyping`
```json
{
  "conversationId": "7f8b3c21-1234-4567-89ab-cdef01234567",
  "userId": "d3b07384-d113-4611-9c88-e24c5b369c01",
  "userName": "Abdullah",
  "isTyping": true
}
```
