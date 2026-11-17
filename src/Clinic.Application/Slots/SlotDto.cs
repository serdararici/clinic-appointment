using System.Text.Json.Serialization;
using Clinic.Domain.Scheduling;

namespace Clinic.Application.Slots;

/// <summary>One bookable slot: UTC start/end and its status (serialized as "Available", "Booked" or "Past").</summary>
public record SlotDto(
    DateTime StartUtc,
    DateTime EndUtc,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SlotStatus Status);
