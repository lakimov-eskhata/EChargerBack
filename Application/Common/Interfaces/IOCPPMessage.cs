

namespace Application.Common.Interfaces
{
    public interface IOCPPMessage
    {
        /// <summary>
        /// Message type
        /// </summary>
        string MessageType { get; set; }

        /// <summary>
        /// Message ID
        /// </summary>
        string UniqueId { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        string Action { get; set; }

        /// <summary>
        /// JSON-Payload
        /// </summary>
        string JsonPayload { get; set; }
    }
}
