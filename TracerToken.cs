using System;

namespace Shared
{
    public sealed class TracerToken
    {
        public Guid Id { get; set; }

        public string HubId { get; set; }

        public long GenerateTime { get; set; }

        public TracerToken() { }

        public TracerToken(TracerToken copyFrom)
        {
            Id = copyFrom.Id;
            HubId = copyFrom.HubId;
            GenerateTime = copyFrom.GenerateTime;
        }
    }
}
