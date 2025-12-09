

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Common.Interfaces
{
    public interface IExternalAuthorization
    {
        /// <summary>
        /// Returnes a name of the extension (=> log output)
        /// </summary>
        string ExtensionName { get; }

        /// <summary>
        /// Initializes the extension
        /// </summary>
        /// <returns>Returns true when the initialization was successfull and the extension can be used</returns>
        bool InitializeExtension(ILoggerFactory logFactory, IConfiguration configuration);

        /// <summary>
        /// Allows to override the internal authorization logic for trancations.
        /// If the method returns null, the standard logic is used.
        /// </summary>
        bool? Authorize(AuthAction action, string token, string chargePointId, int? connectorId, string transactionId, string transactionStartToken);
    }

    public enum AuthAction
    {
        /// <summary>
        /// The authorization request is initiated by an OCPP Authorize message
        /// </summary>
        Authorize,

        /// <summary>
        /// The authorization request is initiated by an OCPP StartTransaction message
        /// </summary>
        StartTransaction,

        /// <summary>
        /// The authorization request is initiated by an OCPP StopTransaction message
        /// </summary>
        StopTransaction
    }
}
