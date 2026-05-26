using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace StudentConsultationLog
{
   
    class Consultation
    {
        public int RecordId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string Adviser { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    static class FileRepository
    {
        private static readonly string DataFile = Path.Combine("Data", "consultations.txt");

        public static int GenerateId()
        {
            var list = GetAllRecords();
            return list.Count == 0 ? 1 : list.Max(r => r.RecordId) + 1;
        }

        public static string GenerateChecksum(string data)
        {
            if (data == null) return string.Empty;
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static void AddRecord(Consultation c)
        {
            var list = GetAllRecords();
            list.Add(c);
            SaveAll(list);
        }

        public static List<Consultation> GetAllRecords()
        {
            try
            {
                if (!File.Exists(DataFile)) return new List<Consultation>();
                var json = File.ReadAllText(DataFile);
                if (string.IsNullOrWhiteSpace(json)) return new List<Consultation>();
                try
                {
                    return JsonSerializer.Deserialize<List<Consultation>>(json) ?? new List<Consultation>();
                }
                catch
                {
                    // backup corrupted file
                    try
                    {
                        var bak = DataFile + ".corrupt." + DateTime.Now.ToString("yyyyMMddHHmmss");
                        File.Copy(DataFile, bak);
                        File.WriteAllText(DataFile, "");
                    }
                    catch { }
                    return new List<Consultation>();
                }
            }
            catch
            {
                return new List<Consultation>();
            }
        }

        public static void SaveAll(List<Consultation> list)
        {
            try
            {
                var json = JsonSerializer.Serialize(list);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                AuditLogger.Log("ERROR", "Failed to save data: " + ex.Message);
                throw;
            }
        }
    }

    static class Validator
    {
        public static bool IsValidInput(string s) => !string.IsNullOrWhiteSpace(s);
    }

    static class AuditLogger
    {
        private static readonly string AuditFile = Path.Combine("Data", "auditlog.txt");
        public static void Log(string action, string message)
        {
            var line = $"{DateTime.Now:O} [{action}] {message}";
            try { File.AppendAllText(AuditFile, line + Environment.NewLine); } catch { }
        }
    }

    static class ReportGenerator
    {
        public static void GenerateReport(List<Consultation> list)
        {
            var path = Path.Combine("Data", "report.txt");
            using var sw = new StreamWriter(path, false);
            sw.WriteLine("Consultation Report");
            sw.WriteLine("Generated: " + DateTime.Now);
            sw.WriteLine();
            foreach (var c in list.Where(x => x.IsActive))
            {
                sw.WriteLine($"ID: {c.RecordId}, Student: {c.StudentName}, Adviser: {c.Adviser}, Concern: {c.Concern}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            InitializeStorage();

            while (true)
            {
                Console.Clear();

                Console.WriteLine("===== STUDENT CONSULTATION LOG =====");
                Console.WriteLine();
                Console.WriteLine("[1] Add Consultation");
                Console.WriteLine("[2] View Consultations");
                Console.WriteLine("[3] Search Consultation");
                Console.WriteLine("[4] Update Consultation");
                Console.WriteLine("[5] Delete Consultation");
                Console.WriteLine("[6] Hard Delete Consultation (permanent)");
                Console.WriteLine("[7] Generate Report");
                Console.WriteLine("[8] Exit");
                Console.WriteLine();
                Console.Write("Select: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddConsultation();
                        break;

                    case "2":
                        ViewConsultations();
                        break;

                    case "3":
                        SearchConsultation();
                        break;

                    case "4":
                        UpdateConsultation();
                        break;

                    case "5":
                        DeleteConsultation();
                        break;

                    case "6":
                        HardDeleteConsultation();
                        break;

                    case "7":
                        GenerateReport();
                        break;

                    case "8":
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void InitializeStorage()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }

            if (!File.Exists("Data/consultations.txt"))
            {
                File.Create("Data/consultations.txt").Close();
            }

            if (!File.Exists("Data/auditlog.txt"))
            {
                File.Create("Data/auditlog.txt").Close();
            }
        }

        static void AddConsultation()
        {
            try
            {
                Consultation c = new Consultation();

                c.RecordId = FileRepository.GenerateId();

                Console.Write("Student Name: ");
                c.StudentName = Console.ReadLine();

                Console.Write("Course: ");
                c.Course = Console.ReadLine();

                Console.Write("Concern: ");
                c.Concern = Console.ReadLine();

                Console.Write("Adviser: ");
                c.Adviser = Console.ReadLine();

                if (!Validator.IsValidInput(c.StudentName) ||
                    !Validator.IsValidInput(c.Course) ||
                    !Validator.IsValidInput(c.Concern) ||
                    !Validator.IsValidInput(c.Adviser))
                {
                    Console.WriteLine("Invalid input.");
                    Console.ReadKey();
                    return;
                }

                c.CreatedAt = DateTime.Now;
                c.UpdatedAt = DateTime.Now;
                c.IsActive = true;

                string checksumData = c.StudentName + c.Course + c.Concern + c.Adviser;
                c.Checksum = FileRepository.GenerateChecksum(checksumData);

                FileRepository.AddRecord(c);

                AuditLogger.Log("ADD", "Record ID " + c.RecordId + " added");

                Console.WriteLine("Record added successfully.");
            }
            catch (Exception ex)
            {
                AuditLogger.Log("ERROR", ex.Message);
                Console.WriteLine("Error: " + ex.Message);
            }

            Console.ReadKey();
        }

        static void ViewConsultations()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            Console.WriteLine();

            foreach (Consultation c in list)
            {
                if (c.IsActive == true)
                {
                    Console.WriteLine("ID: " + c.RecordId);
                    Console.WriteLine("Student: " + c.StudentName);
                    Console.WriteLine("Course: " + c.Course);
                    Console.WriteLine("Concern: " + c.Concern);
                    Console.WriteLine("Adviser: " + c.Adviser);
                    Console.WriteLine("Created At: " + c.CreatedAt.ToString("g"));
                    Console.WriteLine("Updated At: " + c.UpdatedAt.ToString("g"));
                    Console.WriteLine();
                }
            }

            AuditLogger.Log("VIEW", "Viewed all records");

            Console.ReadKey();
        }

        static void SearchConsultation()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            Console.Write("Enter Student Name: ");
            string keyword = Console.ReadLine();

            Console.WriteLine();

            foreach (Consultation c in list)
            {
                if (c.StudentName.ToLower().Contains(keyword.ToLower()) && c.IsActive == true)
                {
                    Console.WriteLine("ID: " + c.RecordId);
                    Console.WriteLine("Student: " + c.StudentName);
                    Console.WriteLine("Course: " + c.Course);
                    Console.WriteLine("Concern: " + c.Concern);
                    Console.WriteLine("Adviser: " + c.Adviser);
                    Console.WriteLine();
                }
            }

            AuditLogger.Log("SEARCH", "Searched for " + keyword);

            Console.ReadKey();
        }

        static void UpdateConsultation()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            Console.Write("Enter Record ID: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            bool found = false;

            foreach (Consultation c in list)
            {
                if (c.RecordId == id && c.IsActive == true)
                {
                    found = true;

                    Console.Write("New Concern: ");
                    c.Concern = Console.ReadLine();

                    Console.Write("New Adviser: ");
                    c.Adviser = Console.ReadLine();

                    c.UpdatedAt = DateTime.Now;

                    string checksumData = c.StudentName + c.Course + c.Concern + c.Adviser;
                    c.Checksum = FileRepository.GenerateChecksum(checksumData);

                    break;
                }
            }

            if (found)
            {
                FileRepository.SaveAll(list);

                AuditLogger.Log("UPDATE", "Record ID " + id + " updated");

                Console.WriteLine("Record updated.");
            }
            else
            {
                Console.WriteLine("Record not found.");
            }

            Console.ReadKey();
        }

        static void DeleteConsultation()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            Console.Write("Enter Record ID: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            bool found = false;

            foreach (Consultation c in list)
            {
                if (c.RecordId == id)
                {
                    found = true;
                    c.IsActive = false;
                    break;
                }
            }

            if (found)
            {
                FileRepository.SaveAll(list);

                AuditLogger.Log("DELETE", "Record ID " + id + " soft deleted");

                Console.WriteLine("Record deleted.");
            }
            else
            {
                Console.WriteLine("Record not found.");
            }

            Console.ReadKey();
        }

        static void GenerateReport()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            try
            {
                ReportGenerator.GenerateReport(list);
                AuditLogger.Log("REPORT", $"Generated consultation report ({list.Count(r=>r.IsActive)} active records)");
                Console.WriteLine("Report generated: Data/report.txt");
            }
            catch (Exception ex)
            {
                AuditLogger.Log("ERROR", "Report generation failed: " + ex.Message);
                Console.WriteLine("Error generating report: " + ex.Message);
            }

            Console.ReadKey();
        }

        static void HardDeleteConsultation()
        {
            List<Consultation> list = FileRepository.GetAllRecords();

            Console.Write("Enter Record ID to permanently delete: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            var target = list.FirstOrDefault(x => x.RecordId == id);
            if (target == null)
            {
                Console.WriteLine("Record not found.");
                Console.ReadKey();
                return;
            }

            list.Remove(target);
            FileRepository.SaveAll(list);
            AuditLogger.Log("HARD_DELETE", "Record ID " + id + " permanently removed");
            Console.WriteLine("Record permanently deleted.");
            Console.ReadKey();
        }
    }
}