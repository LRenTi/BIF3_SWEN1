using MTCG.Data.Repository;
using System.Collections.Generic;
   
   namespace MTCG.Core.Entities
   {
       public class User
       {
           public string Username { get; private set; }
           public string Password { get; private set; }
           public int Coins { get; set; } = 20;
           public int Elo { get; set; } = 100;
           public int Wins { get; set; } = 0;
           public int Losses { get; set; } = 0;
           public int Games { get; set; } = 0;
           public bool IsAdmin { get; set; } = false;
           public User(string username, string password)
           {
               Username = username;
               Password = password;
           }
   
           /// <summary>
           /// Registriert den User in der DB (Wrap auf das UserRepository).
           /// </summary>
           public static void Register(string username, string password)
           {
               UserRepository.RegisterUser(username, password);
           }
   
           /// <summary>
           /// Authentifiziert den User (Wrap auf das UserRepository).
           /// </summary>
           public static (bool Success, User? User) Authenticate(string username, string password)
           {
               return UserRepository.AuthenticateUser(username, password);
           }
   
           /// <summary>
           /// Holt User (wenn vorhanden) per Username aus DB.
           /// </summary>
           public static User? Get(string username)
           {
               return UserRepository.GetUserByUsername(username);
           }
   
           /// <summary>
           /// Speichert diesen User zurück in DB (z.B. nach ELO-Änderung oder Profil-Aktualisierung).
           /// </summary>
           public void Save()
           {
               UserRepository.UpdateUser(this);
           }
           
       }
   }