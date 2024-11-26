using System.Security;
using System.Security.Authentication;
using BCrypt.Net;

namespace MTCG;
/// <summary>This class represents a user.</summary>
public sealed class User
{
    /// <summary>Currently holds the system users.</summary>
    /// <remarks>Is to be removed by database implementation later.</remarks>
    private static Dictionary<string, User> _Users = new();

    /// <summary>Creates a new instance of this class.</summary>
    private User()
    { }

    /// <summary>Gets the user name.</summary>
    public string UserName
    {
        get; private set;
    } = string.Empty;

    /// <summary>Gets or sets the user's full name.</summary>
    public string FullName
    {
        get; set;
    } = string.Empty;

    /// <summary>Gets or sets the user's e-mail address.</summary>
    public string EMail
    {
        get; set;
    } = string.Empty;

    /// <summary>Gets the password hash.</summary>
    private string PasswordHash { get; set; } = string.Empty;

    /// <summary>Verfügbare Münzen des Benutzers.</summary>
    public int Coins { get; private set; } = 20;

    /// <summary>ELO-Wertung des Benutzers.</summary>
    public int Elo { get; private set; } = 100;

    /// <summary>Anzahl der gespielten Spiele.</summary>
    public int GamesPlayed { get; private set; } = 0;

    /// <summary>Sammlung aller Karten des Benutzers.</summary>
    public List<Card> Stack { get; private set; } = new();

    /// <summary>Aktives Deck für Kämpfe (max. 4 Karten).</summary>
    public List<Card> Deck { get; private set; } = new();


    /// <summary>Saves changes to the user object.</summary>
    /// <param name="token">Token of the session trying to modify the object.</param>
    /// <exception cref="SecurityException">Thrown in case of an unauthorized attempt to modify data.</exception>
    /// <exception cref="AuthenticationException">Thrown when the token is invalid.</exception>
    public void Save(string token)
    {
        (bool Success, User? User) auth = Token.Authenticate(token);
        if (auth.Success)
        {
            if (auth.User!.UserName != UserName)
            {
                throw new SecurityException("Trying to change other user's data.");
            }
            // Save data.
        }
        else { new AuthenticationException("Not authenticated."); }
    }

    /// <summary>Creates a user.</summary>
    /// <param name="userName">User name.</param>
    /// <param name="password">Password.</param>
    /// <param name="fullName">Full name.</param>
    /// <param name="eMail">E-mail addresss.</param>
    /// <exception cref="UserException">Thrown when the user name already exists.</exception>
    public static void Create(string userName, string password, string fullName = "", string eMail = "")
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty or whitespace");
        }

        if (_Users.ContainsKey(userName))
        {
            throw new UserException("User name already exists.");
        }

        User user = new()
        {
            UserName = userName,
            PasswordHash = HashPassword(password),
            FullName = fullName,
            EMail = eMail
        };
        

        _Users.Add(user.UserName, user);
    }

    /// <summary>Performs a user logon.</summary>
    /// <param name="userName">User name.</param>
    /// <param name="password">Password.</param>
    /// <returns>Returns a tuple of success flag and token.
    ///          If successful, the success flag is TRUE and the token contains a token string,
    ///          otherwise success flag is FALSE and token is empty.</returns>
    public static (bool Success, string Token) Logon(string userName, string password)
    {

        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, string.Empty);
        }

        if (_Users.TryGetValue(userName, out User? user))
        {

            if (VerifyPassword(password, user.PasswordHash))
            {
                return (true, Token._CreateTokenFor(user));
            }
        }

        return (false, string.Empty);
    }

    /// <summary>
    /// Hashes the password using BCrypt with a work factor of 12.  
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password.</returns>

    private static string HashPassword(string password)
    {
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        return hash;
    }
    
    /// <summary>
    /// Verifies if the password matches the hash.  
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    private static bool VerifyPassword(string password, string hash)
    {
        bool result = BCrypt.Net.BCrypt.Verify(password, hash);
        return result;
    }

    /// <summary>Aktualisiert die ELO-Wertung nach einem Kampf.</summary>
    public void UpdateElo(bool hasWon)
    {
        Elo += hasWon ? 3 : -5;
        GamesPlayed++;
    }

    /// <summary>Fügt Karten zum Stack hinzu.</summary>
    public void AddCardsToStack(IEnumerable<Card> cards)
    {
        Stack.AddRange(cards);
    }

    /// <summary>Setzt das aktive Deck.</summary>
    public void SetDeck(List<Card> selectedCards)
    {
        if (selectedCards.Count != 4)
            throw new ArgumentException("Deck muss genau 4 Karten enthalten.");

        Deck = selectedCards;
    }
}