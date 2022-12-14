using MonsterTradingCardGame.Api;
using MonsterTradingCardGame.Data.User;

namespace MonsterTradingCardGame.Server {

    /// <summary
    ///     Singleton for handling sessions.
    /// </summary>
    public class SessionHandler {

        public static readonly Response UNAUTHORIZED_RESPONSE = new(HttpCode.UNAUTHORIZED_401, "Not logged in");

        private static readonly Logger<SessionHandler> _logger = new();
        private static SessionHandler? _instance;

        // /////////////////////////////////////////////////////////////////////
        // Properties
        // /////////////////////////////////////////////////////////////////////

        /// <summary>
        ///     Singleton instance.
        ///     This is how you want to access the SessionHandler.
        /// </summary>
        public static SessionHandler Instance {
            get {
                _instance ??= new SessionHandler();
                return _instance;
            }
        }

        /// <summary>
        ///     A list of all active sessions
        ///     The key is the bearer token
        ///     The value is the token object
        /// </summary>
        private readonly Dictionary<string, Token> sessions;

        // /////////////////////////////////////////////////////////////////////
        // Init
        // /////////////////////////////////////////////////////////////////////

        private SessionHandler() {
            sessions = new Dictionary<string, Token>();
        }

        // /////////////////////////////////////////////////////////////////////
        // Methods
        // /////////////////////////////////////////////////////////////////////

        /// <summary>
        ///     Creates a new session for a <see cref="User"/>.
        /// </summary>
        /// <param name="userId">The id of the user</param>
        /// <param name="username">The username of the user</param>
        /// <param name="userRole">The role of the user</param>
        /// <returns>The token linked to the session</returns>
        public Token CreateSession(Guid userId, string username, UserRole userRole) {
            Token token = new(userId, username, userRole);
            sessions.Add(token.Bearer, token);
            _logger.Info($"Created session for user {username}");
            return token;
        }

        /// <summary>
        ///     Gets the token for a bearer.
        /// </summary>
        /// <param name="bearer">The bearer</param>
        /// <returns>The token, if it exists and has not expired, null otherwise.</returns>
        public Token? GetSession(string bearer) {
            bearer = bearer.Replace("Bearer ", "");

            if (sessions.ContainsKey(bearer)) {
                Token token = sessions[bearer];
                if (token.ExpiryDate > DateTime.Now) {
                    return token;
                }

                sessions.Remove(bearer);
                _logger.Info($"Session for user {token.Username} has expired");
            }

            return null;
        }
    }
}
