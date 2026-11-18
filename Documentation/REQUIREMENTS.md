# ChatApp — Requirements Specification

**Document Version:** 1.0.0  
**Phase:** 1 — Product Discovery & Architecture  
**Status:** Ready for Review & Approval  
**Target Platform:** .NET MAUI (Android & iOS) + ASP.NET Core Web API (.NET 10)  
**Database:** Microsoft SQL Server 2022+ / LocalDB with Entity Framework Core  

---

## 1. Executive Summary & Product Vision

**ChatApp** is a next-generation cross-platform messaging application engineered to blend the rock-solid reliability, speed, and clean communication of **WhatsApp** with the creative richness, personalization, and ecosystem features of **Telegram**. 

### 1.1 Core Value Proposition & Differentiators
1. **Fluid, High-Performance Native UI:** Built with .NET MAUI (.NET 10) leveraging compiled bindings, optimized virtualized message collections, and dual-direction localization (Arabic Cairo RTL & English Inter LTR).
2. **Integrated Sticker Studio & Marketplace:** Unlike traditional messengers that treat stickers as static downloads, ChatApp features an in-app sticker studio allowing users to crop, decorate, caption, and publish personal photo stickers into community or private packs.
3. **Multi-State Reliable Real-Time Messaging:** SignalR-powered stateful message lifecycle (`Sent` -> `Delivered` -> `Read`) combined with local client outbox queueing for resilient offline tolerance.
4. **Clean, Modular & Scalable Architecture:** Strictly decoupled Clean Architecture on the backend (Domain, Application, Infrastructure, API) avoiding bloated microservices while remaining cloud-scale ready.

---

## 2. Functional Requirements (FR)

### Module 1: Identity & Authentication (FR-AUTH)
- **FR-AUTH-01 (Registration):** New users can register with Full Name, unique Username, unique Email, and Password.
- **FR-AUTH-02 (Password Security):** Enforce secure password criteria (minimum 8 characters, uppercase, lowercase, digit, special character) hashed using ASP.NET Core Identity (PBKDF2/Argon2).
- **FR-AUTH-03 (Authentication):** Users authenticate via Email or Username with password. Upon success, receive short-lived JWT Access Token (15 mins) and long-lived Refresh Token (7 days).
- **FR-AUTH-04 (Token Refresh & Rotation):** Secure refresh token exchange with automatic rotation and token-family invalidation if reuse of a revoked token is detected.
- **FR-AUTH-05 (Revocation & Logout):** Immediate revocation of active refresh tokens upon explicit logout across devices.
- **FR-AUTH-06 (User Profile):** Retrieve and update the authenticated user's profile (`FullName`, `Bio`, `AvatarUrl`).
- **FR-AUTH-07 (Account Recovery):** Architectural baseline for password reset and email confirmation tokens via secure Identity token providers.

### Module 2: User Discovery & Contacts (FR-USER)
- **FR-USER-01 (Search):** Search registered users by exact/partial username or full display name with debounced, paginated queries.
- **FR-USER-02 (User Profile View):** View other users' public profile details (Avatar, Display Name, Username, Bio, Presence status subject to privacy rules).
- **FR-USER-03 (Privacy Settings):** Configurable visibility for `LastSeenAt` and `IsOnline` status (Everyone, Contacts Only, Nobody).

### Module 3: Conversations & Inbox Management (FR-CONV)
- **FR-CONV-01 (Direct Chats):** Initiate 1-on-1 private conversations between two users. Duplicate conversation prevention ensures exactly one direct thread exists per user pair.
- **FR-CONV-02 (Inbox Listing):** Display active conversations ordered by latest message timestamp, showing avatar, title, snippet of last message, unread message count badge, and pinned/archived indicator.
- **FR-CONV-03 (Conversation Actions):** Pin conversations to top of inbox and archive inactive conversations without deleting message history.
- **FR-CONV-04 (Group Conversations):** Create group conversations with custom Title, Avatar, Description, and designated initial Group Admin.
- **FR-CONV-05 (Group Membership):** Group Admins can add members, promote members to Admin, remove members, or transfer ownership.

### Module 4: Real-Time Messaging Engine (FR-MSG)
- **FR-MSG-01 (Message Delivery):** Instant bidirectional message dispatch over SignalR WebSocket connection with fallback to HTTP REST.
- **FR-MSG-02 (Message Persistence):** Messages are securely persisted in SQL Server before delivery acknowledgment is broadcast.
- **FR-MSG-03 (Message Types):** Support `Text`, `Image`, `Video`, `Audio` (voice notes), and `Sticker`.
- **FR-MSG-04 (Delivery Receipts):** Tri-state message status indicators:
  - Single gray tick: `Sent` (Persisted on server).
  - Double gray tick: `Delivered` (Delivered to recipient client app/hub).
  - Double blue tick: `Read` (Rendered in active chat view by recipient).
- **FR-MSG-05 (Typing Indicators):** Ephemeral typing events broadcast when a participant types in an active chat window, automatically clearing after 3 seconds of inactivity.
- **FR-MSG-06 (Replies & Quoting):** Quote previous messages in replies, maintaining parent message references.
- **FR-MSG-07 (Message Reactions):** React to messages with standard emoji reactions (👍, ❤️, 😂, 😮, 😢, 🙏).
- **FR-MSG-08 (Edit & Delete):**
  - Edit text messages within a 15-minute grace window (flagged as `Edited`).
  - Delete messages for self or for everyone (soft delete with tombstone `This message was deleted`).
- **FR-MSG-09 (Message Pagination):** Cursor-based pagination (`beforeMessageId` or timestamp) loading 30-50 messages per chunk to preserve memory.

### Module 5: Media & Storage (FR-MEDIA)
- **FR-MEDIA-01 (Upload Validation):** Strict MIME-type validation, magic-number inspection, and size restrictions (Images: 10MB, Videos: 50MB, Audio: 15MB).
- **FR-MEDIA-02 (Storage Abstraction):** Store media files in object storage (local structured file storage for dev, AWS S3/Azure Blob for production). Only storage URLs, metadata, and dimensions are recorded in SQL Server.
- **FR-MEDIA-03 (Thumbnails):** Automated low-resolution preview generation for images and video poster frames.
- **FR-MEDIA-04 (Voice Notes):** In-app audio recording, waveform visualization, and playback.

### Module 6: Custom Sticker Studio & Packs (FR-STICKER)
- **FR-STICKER-01 (Image Ingestion & Cropping):** Select personal photo from device gallery; crop to square or freeform bounds.
- **FR-STICKER-02 (Decorations & Captions):** Overlay text with custom colors, outline strokes, and sticker accessories.
- **FR-STICKER-03 (Packs Management):** Group custom stickers into `StickerPacks` with name, cover image, and author attribution.
- **FR-STICKER-04 (Pack Sharing & Installation):** Users can share pack links or add public sticker packs to their personal library.
- **FR-STICKER-05 (In-Chat Sticker Keyboard):** Dedicated sticker tray in chat with tabs for installed packs and recently used stickers.

---

## 3. Non-Functional Requirements (NFR)

### 3.1 Performance & Latency
- **NFR-PERF-01 (Real-Time Latency):** Sub-150ms roundtrip message transmission over standard 4G/5G connections when both clients are online.
- **NFR-PERF-02 (Database Queries):** P95 database query execution time under 50ms for message retrieval and inbox listing using covering indexes.
- **NFR-PERF-03 (UI Frame Rate):** 60 FPS scrolling performance on mobile message lists using virtualized UI controls with zero UI-thread blocking.
- **NFR-PERF-04 (Cold Startup):** Mobile application time-to-interactive under 2.0 seconds on mid-range Android/iOS devices.

### 3.2 Scalability & Availability
- **NFR-SCAL-01 (Stateless API):** API nodes are fully stateless; JWT-based auth ensures scale-out behind load balancers.
- **NFR-SCAL-02 (SignalR Scale-Out):** Hub designed for Redis Backplane / Azure SignalR Service scale-out for clustering multiple backend instances.
- **NFR-SCAL-03 (Storage Elasticity):** Media uploads decoupled from application servers via object storage architecture.

### 3.3 Security & Compliance
- **NFR-SEC-01 (Transport Security):** Mandatory TLS 1.3 for all HTTP REST and WebSocket connections.
- **NFR-SEC-02 (Credential Storage):** Mobile clients store JWT tokens, refresh tokens, and encryption secrets exclusively in OS-level hardware-backed keystores (`MAUI SecureStorage` / Android KeyStore / iOS Keychain).
- **NFR-SEC-03 (Token Security):** Refresh tokens stored as salted SHA-256 hashes in database; plain token string exists only on client.
- **NFR-SEC-04 (Rate Limiting):** API rate limiting applied to authentication endpoints (max 5 failed login attempts per IP per minute) to deter brute-force attacks.
- **NFR-SEC-05 (Information Leakage):** RFC 7807 Problem Details for all errors; zero stack trace or internal database exceptions exposed to clients.

### 3.4 Reliability, Offline Resilience & Data Integrity
- **NFR-REL-01 (Client Outbox Queue):** If mobile network drops, outgoing messages queue locally with a temporary `Pending` state and automatically flush with exponential backoff on reconnection.
- **NFR-REL-02 (Idempotency):** Every message payload includes a client-generated UUID (`ClientMessageId`). The backend uses this key to prevent duplicate message insertions during network retries.
- **NFR-REL-03 (Soft Deletes):** Messages, conversations, and attachments utilize soft-delete flags with EF Core Global Query Filters to prevent accidental or malicious data loss.

### 3.5 Internationalization & Accessibility
- **NFR-I18N-01 (Bilingual Architecture):** Full bidirectional localization:
  - Arabic (Cairo font, RTL layout mirroring).
  - English (Inter / System font, LTR layout).
- **NFR-ACC-01 (Touch Targets):** Minimum 48x48 dp interactive touch targets across mobile controls.
- **NFR-ACC-02 (Color Contrast):** Strict adherence to WCAG 2.1 AA contrast ratio (minimum 4.5:1 for normal text).

---

## 4. MVP Scope vs. Future Phases

To maintain rapid delivery velocity, strict architectural boundaries separate the initial MVP from subsequent phases:

| Feature Area | MVP Release (Phases 1-7) | Phase 8 (Media & Stickers) | Advanced (Phases 9-10) |
|---|---|---|---|
| **Authentication** | Register, Login, JWT + Refresh Token Rotation, Current User Profile | Social Login (optional) | Multi-Factor Authentication (MFA), Biometric Unlock |
| **Messaging** | 1-on-1 Real-Time Text Messages, Sent/Delivered/Read Receipts, Typing Indicators | Media Messages (Image, Video, Audio), Custom Stickers | Message Replies, Forwards, Reactions, Edit, Soft Delete |
| **Conversations** | Direct Chat Creation, User Search, Inbox list with unread badges | Media gallery inside chat info | Group Chats, Admin Roles, Pinned & Archived Chats |
| **Media & Stickers** | *Not in MVP* (Stubs/Schema ready) | Full Media Uploads, Image Cropper Sticker Studio, Sticker Packs | AI Automatic Background Removal, Animated WebP Stickers |
| **Privacy & Security** | Transport TLS, Identity Passwords, Token Hash, Secure Storage | Presigned URL media security | Full Signal Protocol End-to-End Encryption (E2EE) |
| **Push Notifications** | In-app SignalR notifications | Notification with media thumbnail | FCM / APNs background push notification payload |

---

## 5. Architectural Assumptions & External Dependencies

1. **Runtime & Frameworks:** Target `.NET 10.0` for ASP.NET Core API and .NET MAUI.
2. **Relational Database:** Microsoft SQL Server 2022+ / SQL Server LocalDB during development; automated migrations via EF Core.
3. **Target Platforms:** Android 8.0+ (API 26+) and iOS 15.0+.
4. **Third-Party Libraries (Mobile):** `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`. UraniumUI evaluated for specialized input elements if necessary.
5. **Third-Party Libraries (Backend):** `FluentValidation.AspNetCore`, `Serilog.AspNetCore`, `Swashbuckle.AspNetCore` / `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Authentication.JwtBearer`.
