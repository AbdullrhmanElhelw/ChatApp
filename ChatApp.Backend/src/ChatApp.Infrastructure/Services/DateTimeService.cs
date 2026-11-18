using ChatApp.Application.Common.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
