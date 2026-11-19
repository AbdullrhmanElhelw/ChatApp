using Microsoft.AspNetCore.SignalR.Client;
using ChatApp.Mobile.Features.Authentication.Services;
using ChatApp.Mobile.Features.Chat.DTOs;

namespace ChatApp.Mobile.Features.Chat.Services;

public class ChatHubService : IChatHubService, IAsyncDisposable
{
    private readonly ISecureStorageService _secureStorage;
    private HubConnection? _hubConnection;
    private bool _isDisposed;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public event Action<MessageClientDto>? MessageReceived;
    public event Action<MessageClientDto>? MessageConfirmationReceived;
    public event Action<MessageStatusUpdateClientDto>? MessageStatusChanged;
    public event Action<TypingIndicatorClientDto>? UserTyping;
    public event Action<Guid, bool, DateTime?>? UserPresenceChanged;
    public event Action<bool>? ConnectionStateChanged;

    public ChatHubService(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        if (_hubConnection == null)
        {
            var hubUrl = $"{ApiConfig.BaseUrl}/hubs/chat";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () => await _secureStorage.GetAccessTokenAsync();
                })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            RegisterHandlers();
        }

        try
        {
            await _hubConnection.StartAsync();
            ConnectionStateChanged?.Invoke(true);
        }
        catch
        {
            ConnectionStateChanged?.Invoke(false);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            ConnectionStateChanged?.Invoke(false);
        }
    }

    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinConversation", conversationId);
        }
    }

    public async Task LeaveConversationAsync(Guid conversationId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveConversation", conversationId);
        }
    }

    public async Task SendMessageAsync(SendMessageRequestClientDto request)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMessage", request);
        }
    }

    public async Task MarkAsDeliveredAsync(Guid messageId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("MarkAsDelivered", messageId);
        }
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid? messageId = null)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("MarkAsRead", conversationId, messageId);
        }
    }

    public async Task SendTypingIndicatorAsync(Guid conversationId, bool isTyping)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendTypingIndicator", conversationId, isTyping);
        }
    }

    private void RegisterHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<MessageClientDto>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });

        _hubConnection.On<MessageClientDto>("ReceiveMessageConfirmation", message =>
        {
            MessageConfirmationReceived?.Invoke(message);
        });

        _hubConnection.On<MessageStatusUpdateClientDto>("MessageStatusChanged", update =>
        {
            MessageStatusChanged?.Invoke(update);
        });

        _hubConnection.On<TypingIndicatorClientDto>("UserTyping", indicator =>
        {
            UserTyping?.Invoke(indicator);
        });

        _hubConnection.On<Guid, bool, DateTime?>("UserPresenceChanged", (userId, isOnline, lastSeen) =>
        {
            UserPresenceChanged?.Invoke(userId, isOnline, lastSeen);
        });

        _hubConnection.Reconnecting += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(true);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += _ =>
        {
            ConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed && _hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _isDisposed = true;
        }
    }
}
