using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using RepNote.Models;
//Réalisé par Ryan Läuppi (Ryancmoi)

namespace RepNote.Services
{
    public class WorkoutService
    {
        private readonly string _filePath;

        public WorkoutService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "db.json");
        }

        public async Task<WorkoutRoot> LoadWorkoutsAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new WorkoutRoot();

                string json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<WorkoutRoot>(json) ?? new WorkoutRoot();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement : {ex.Message}");
                return new WorkoutRoot();
            }
        }

        public async Task SaveWorkoutsAsync(WorkoutRoot root)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(root, options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde : {ex.Message}");
            }
        }
    }
}
