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
    public class Persoana
    {
        public int Id { get; set; }
        public string Nume { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DataNasterii { get; set; }
        public string NumarTelefon { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;

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

    public class Validator
    {
        public static bool ValidareEmail(string? email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool ValidareNumarTelefon(string? numarTelefon)
        {
            if (string.IsNullOrEmpty(numarTelefon)) return false;
            string pattern = @"^(\+4|0)\d{9,10}$";
            return Regex.IsMatch(numarTelefon, pattern);
        }

        public static bool ValidareNume(string? nume)
        {
            if (string.IsNullOrEmpty(nume)) return false;
            string pattern = @"^[a-zA-ZăîâșțĂÎÂȘȚ\s-]+$";
            return Regex.IsMatch(nume, pattern);
        }

        public static bool ValidareSex(string? sex)
        {
            return !string.IsNullOrEmpty(sex) && (sex.ToLower() == "m" || sex.ToLower() == "f");
        }
    }

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
            try
            {
                if (!File.Exists(dbPath))
                {
                    SQLiteConnection.CreateFile(dbPath);
                    Console.WriteLine("Baza de date creată: persoane.db");
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
                        Console.WriteLine("Tabel Persoane creat sau existent");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la inițializarea bazei de date: {ex.Message}");
            }
        }

        public void AdaugaPersoana(Persoana persoana)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string sql = @"
                        INSERT OR IGNORE INTO Persoane (Nume, Email, DataNasterii, NumarTelefon, Sex)
                        VALUES (@Nume, @Email, @DataNasterii, @NumarTelefon, @Sex)";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Nume", persoana.Nume);
                        command.Parameters.AddWithValue("@Email", persoana.Email);
                        command.Parameters.AddWithValue("@DataNasterii", persoana.DataNasterii.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@NumarTelefon", persoana.NumarTelefon);
                        command.Parameters.AddWithValue("@Sex", persoana.Sex);
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Persoană adăugată: {persoana.Nume}, rânduri afectate: {rowsAffected}");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"Eroare la adăugarea persoanei: {ex.Message}");
            }
        }

        public List<Persoana> ObtineToatePersoanele()
        {
            var persoane = new List<Persoana>();

            try
            {
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
                                string nume = reader["Nume"]?.ToString() ?? string.Empty;
                                string email = reader["Email"]?.ToString() ?? string.Empty;
                                string numarTelefon = reader["NumarTelefon"]?.ToString() ?? string.Empty;
                                string sex = reader["Sex"]?.ToString() ?? string.Empty;
                                string dataNasteriiStr = reader["DataNasterii"]?.ToString() ?? string.Empty;

                                DateTime dataNasterii = DateTime.TryParse(dataNasteriiStr, out var parsedDate)
                                    ? parsedDate
                                    : DateTime.MinValue;

                                persoane.Add(new Persoana
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nume = nume,
                                    Email = email,
                                    DataNasterii = dataNasterii,
                                    NumarTelefon = numarTelefon,
                                    Sex = sex
                                });
                            }
                        }
                    }
                }
                Console.WriteLine($"Număr persoane obținute: {persoane.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la obținerea persoanelor: {ex.Message}");
            }

            return persoane;
        }

        public void StergePersoana(int id)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la ștergerea persoanei: {ex.Message}");
            }
        }

        public void ActualizeazaPersoana(Persoana persoana)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la actualizarea persoanei: {ex.Message}");
            }
        }
    }

    public class ExportManager
    {
        public static void ExportToJson(List<Persoana> persoane, string filePath = "persoane.json")
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(persoane, options);
                File.WriteAllText(filePath, jsonString);
                Console.WriteLine($"Date exportate cu succes în {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la exportul JSON: {ex.Message}");
            }
        }

        public static void GenerareRaport(List<Persoana> persoane, string filePath = "raport.txt")
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la generarea raportului: {ex.Message}");
            }
        }

        public static void GenerateIndexHtml(List<Persoana> persoane, string filePath = "index.html")
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html lang=\"en\">");
                    writer.WriteLine("<head>");
                    writer.WriteLine("    <meta charset=\"UTF-8\">");
                    writer.WriteLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                    writer.WriteLine("    <title>Lista Persoane</title>");
                    writer.WriteLine("    <style>");
                    writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
                    writer.WriteLine("        table { border-collapse: collapse; width: 100%; }");
                    writer.WriteLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    writer.WriteLine("        th { background-color: #f2f2f2; }");
                    writer.WriteLine("        tr:nth-child(even) { background-color: #f9f9f9; }");
                    writer.WriteLine("    </style>");
                    writer.WriteLine("</head>");
                    writer.WriteLine("<body>");
                    writer.WriteLine("    <h1>Lista Persoane</h1>");
                    writer.WriteLine("    <table>");
                    writer.WriteLine("        <tr>");
                    writer.WriteLine("            <th>Nume</th>");
                    writer.WriteLine("            <th>Email</th>");
                    writer.WriteLine("            <th>Vârsta</th>");
                    writer.WriteLine("            <th>Sex</th>");
                    writer.WriteLine("            <th>Număr Telefon</th>");
                    writer.WriteLine("        </tr>");

                    foreach (var persoana in persoane.OrderBy(p => p.Nume))
                    {
                        writer.WriteLine("        <tr>");
                        writer.WriteLine($"            <td>{persoana.Nume}</td>");
                        writer.WriteLine($"            <td>{persoana.Email}</td>");
                        writer.WriteLine($"            <td>{persoana.Varsta}</td>");
                        writer.WriteLine($"            <td>{(persoana.Sex.ToLower() == "m" ? "Bărbat" : "Femeie")}</td>");
                        writer.WriteLine($"            <td>{persoana.NumarTelefon}</td>");
                        writer.WriteLine("        </tr>");
                    }

                    writer.WriteLine("    </table>");
                    writer.WriteLine("    <p><a href=\"persoane.json\">Descarcă JSON</a></p>");
                    writer.WriteLine("    <p><a href=\"raport.txt\">Vezi Raport</a></p>");
                    writer.WriteLine("</body>");
                    writer.WriteLine("</html>");
                }

                Console.WriteLine($"index.html generat cu succes în {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la generarea index.html: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine("Aplicație Gestionare Persoane");
                Console.WriteLine("-----------------------------");

                var db = new BazaDeDate();
                var persoane = db.ObtineToatePersoanele();
                if (!persoane.Any())
                {
                    Console.WriteLine("Baza de date este goală, adăugăm date de test...");
                    AdaugaDateTest(db);
                    persoane = db.ObtineToatePersoanele();
                }

                Console.WriteLine($"Număr persoane: {persoane.Count}");
                ExportManager.ExportToJson(persoane);
                ExportManager.GenerareRaport(persoane);
                // Comentăm GenerateIndexHtml deoarece folosim index.html furnizat
                // ExportManager.GenerateIndexHtml(persoane);

                Console.WriteLine($"Total persoane în baza de date: {persoane.Count}");
                Console.WriteLine("Date exportate cu succes pentru GitHub Pages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare în execuție: {ex.Message}");
                throw;
            }
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
