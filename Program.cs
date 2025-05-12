using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace ManagementPersoane
{
    // Modelul de date pentru persoană
    public class Persoana
    {
        public int Id { get; set; }
        public string Nume { get; set; }
        public string Email { get; set; }
        public DateTime DataNasterii { get; set; }
        public string NumarTelefon { get; set; }
        public string Sex { get; set; }

        public int Varsta => CalculeazaVarsta();

        private int CalculeazaVarsta()
        {
            var astazi = DateTime.Today;
            var varsta = astazi.Year - DataNasterii.Year;
            if (DataNasterii.Date > astazi.AddYears(-varsta))
                varsta--;
            return varsta;
        }
    }

    // Validator pentru datele introduse
    public class Validator
    {
        public static bool ValidareEmail(string email)
        {
            // Regex simplu pentru validare email
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool ValidareNumarTelefon(string numarTelefon)
        {
            // Format: +40123456789 sau 0712345678
            string pattern = @"^(\+4|0)\d{9,10}$";
            return Regex.IsMatch(numarTelefon, pattern);
        }

        public static bool ValidareNume(string nume)
        {
            // Nume fără caractere speciale, doar litere, spații și -
            string pattern = @"^[a-zA-ZăîâșțĂÎÂȘȚ\s-]+$";
            return Regex.IsMatch(nume, pattern);
        }

        public static bool ValidareSex(string sex)
        {
            return sex.ToLower() == "m" || sex.ToLower() == "f";
        }
    }

    // Manager pentru baza de date
    public class BazaDeDate
    {
        private readonly string connectionString;
        private readonly string dbPath;

        public BazaDeDate(string dbFileName = "persoane.db")
        {
            dbPath = dbFileName;
            connectionString = $"Data Source={dbPath};Version=3;";
            InitializeazaBazaDeDate();
        }

        private void InitializeazaBazaDeDate()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS Persoane (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nume TEXT NOT NULL,
                        Email TEXT NOT NULL UNIQUE,
                        DataNasterii TEXT NOT NULL,
                        NumarTelefon TEXT NOT NULL,
                        Sex TEXT NOT NULL
                    )";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AdaugaPersoana(Persoana persoana)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = @"
                    INSERT INTO Persoane (Nume, Email, DataNasterii, NumarTelefon, Sex)
                    VALUES (@Nume, @Email, @DataNasterii, @NumarTelefon, @Sex)";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Nume", persoana.Nume);
                    command.Parameters.AddWithValue("@Email", persoana.Email);
                    command.Parameters.AddWithValue("@DataNasterii", persoana.DataNasterii.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@NumarTelefon", persoana.NumarTelefon);
                    command.Parameters.AddWithValue("@Sex", persoana.Sex);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Persoana> ObtineToatePersoanele()
        {
            var persoane = new List<Persoana>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Persoane";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            persoane.Add(new Persoana
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nume = reader["Nume"].ToString(),
                                Email = reader["Email"].ToString(),
                                DataNasterii = DateTime.Parse(reader["DataNasterii"].ToString()),
                                NumarTelefon = reader["NumarTelefon"].ToString(),
                                Sex = reader["Sex"].ToString()
                            });
                        }
                    }
                }
            }

            return persoane;
        }

        public void StergePersoana(int id)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM Persoane WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ActualizeazaPersoana(Persoana persoana)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = @"
                    UPDATE Persoane 
                    SET Nume = @Nume, 
                        Email = @Email, 
                        DataNasterii = @DataNasterii, 
                        NumarTelefon = @NumarTelefon, 
                        Sex = @Sex
                    WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", persoana.Id);
                    command.Parameters.AddWithValue("@Nume", persoana.Nume);
                    command.Parameters.AddWithValue("@Email", persoana.Email);
                    command.Parameters.AddWithValue("@DataNasterii", persoana.DataNasterii.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@NumarTelefon", persoana.NumarTelefon);
                    command.Parameters.AddWithValue("@Sex", persoana.Sex);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    // Manager pentru export date
    public class ExportManager
    {
        public static void ExportToJson(List<Persoana> persoane, string filePath = "persoane.json")
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonString = JsonSerializer.Serialize(persoane, options);
            File.WriteAllText(filePath, jsonString);
            Console.WriteLine($"Date exportate cu succes în {filePath}");
        }

        public static void GenerareRaport(List<Persoana> persoane, string filePath = "raport.txt")
        {
            int nrTotal = persoane.Count;
            int nrBarbati = persoane.Count(p => p.Sex.ToLower() == "m");
            int nrFemei = persoane.Count(p => p.Sex.ToLower() == "f");
            double varstaMedia = persoane.Count > 0 ? persoane.Average(p => p.Varsta) : 0;

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("RAPORT STATISTICI BAZĂ DE DATE PERSOANE");
                writer.WriteLine($"Data generării: {DateTime.Now}");
                writer.WriteLine(new string('-', 40));
                writer.WriteLine($"Număr total persoane: {nrTotal}");
                writer.WriteLine($"Bărbați: {nrBarbati} ({(nrTotal > 0 ? (double)nrBarbati / nrTotal * 100 : 0):F2}%)");
                writer.WriteLine($"Femei: {nrFemei} ({(nrTotal > 0 ? (double)nrFemei / nrTotal * 100 : 0):F2}%)");
                writer.WriteLine($"Vârsta medie: {varstaMedia:F2} ani");
                writer.WriteLine(new string('-', 40));
                
                writer.WriteLine("\nLISTA PERSOANE:");
                foreach (var persoana in persoane.OrderBy(p => p.Nume))
                {
                    writer.WriteLine($"{persoana.Nume}, {persoana.Email}, {persoana.Varsta} ani, {(persoana.Sex.ToLower() == "m" ? "Bărbat" : "Femeie")}");
                }
            }

            Console.WriteLine($"Raport generat cu succes în {filePath}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Aplicație Gestionare Persoane");
            Console.WriteLine("-----------------------------");

            var db = new BazaDeDate();
            
            // Adăugăm câteva persoane de test dacă nu există date
            var persoane = db.ObtineToatePersoanele();
            if (!persoane.Any())
            {
                AdaugaDateTest(db);
                persoane = db.ObtineToatePersoanele();
            }

            // Export date pentru GitHub Pages
            ExportManager.ExportToJson(persoane);
            ExportManager.GenerareRaport(persoane);

            Console.WriteLine($"Total persoane în baza de date: {persoane.Count}");
            Console.WriteLine("Date exportate cu succes pentru GitHub Pages.");
        }

        static void AdaugaDateTest(BazaDeDate db)
        {
            var persoane = new List<Persoana>
            {
                new Persoana
                {
                    Nume = "Ion Popescu",
                    Email = "ion.popescu@example.com",
                    DataNasterii = new DateTime(1985, 5, 15),
                    NumarTelefon = "+40712345678",
                    Sex = "M"
                },
                new Persoana
                {
                    Nume = "Maria Ionescu",
                    Email = "maria.ionescu@example.com",
                    DataNasterii = new DateTime(1990, 8, 23),
                    NumarTelefon = "+40723456789",
                    Sex = "F"
                },
                new Persoana
                {
                    Nume = "Andrei Vasilescu",
                    Email = "andrei.vasilescu@example.com",
                    DataNasterii = new DateTime(1978, 11, 7),
                    NumarTelefon = "+40734567890",
                    Sex = "M"
                },
                new Persoana
                {
                    Nume = "Elena Dumitrescu",
                    Email = "elena.dumitrescu@example.com",
                    DataNasterii = new DateTime(1995, 2, 10),
                    NumarTelefon = "+40745678901",
                    Sex = "F"
                },
                new Persoana
                {
                    Nume = "Mihai Popa",
                    Email = "mihai.popa@example.com",
                    DataNasterii = new DateTime(1982, 7, 30),
                    NumarTelefon = "+40756789012",
                    Sex = "M"
                }
            };

            foreach (var persoana in persoane)
            {
                db.AdaugaPersoana(persoana);
                Console.WriteLine($"Adăugat: {persoana.Nume}");
            }
        }
    }
}
