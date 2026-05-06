using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
//Réalisé par Ryan Läuppi (Ryancmoi)

namespace RepNote.Models
{
    public class WorkoutRoot
    {
        [JsonPropertyName("workouts")]
        public List<Workout> Workouts { get; set; } = new();
    }

    public class Workout
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("exercises")]
        public List<Exercise> Exercises { get; set; } = new();
    }

    public class Exercise
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("planned_sets")]
        public List<WorkoutSet> PlannedSets { get; set; } = new();

        [JsonPropertyName("actual_sets")]
        public List<WorkoutSet> ActualSets { get; set; } = new();
    }

    public class WorkoutSet
    {
        [JsonPropertyName("reps")]
        public int Reps { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }
}
