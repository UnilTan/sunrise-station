using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// Represents signal data that can be transmitted through the logical network.
/// Supports empty signals, boolean states, numbers, and strings.
/// </summary>
[Serializable, NetSerializable]
public sealed class LogicSignalData
{
    /// <summary>
    /// Whether this signal is empty (no signal present)
    /// </summary>
    public bool IsEmpty { get; set; } = true;

    /// <summary>
    /// String representation of the signal data
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Numeric value if the signal can be parsed as a number
    /// </summary>
    public float? NumericValue { get; set; }

    /// <summary>
    /// The boolean state for compatibility with existing logic gates
    /// </summary>
    public SignalState State { get; set; } = SignalState.Low;

    public LogicSignalData()
    {
    }

    /// <summary>
    /// Creates an empty signal
    /// </summary>
    public static LogicSignalData Empty()
    {
        return new LogicSignalData
        {
            IsEmpty = true,
            StringValue = null,
            NumericValue = null,
            State = SignalState.Low
        };
    }

    /// <summary>
    /// Creates a boolean signal
    /// </summary>
    public static LogicSignalData Boolean(bool value)
    {
        return new LogicSignalData
        {
            IsEmpty = false,
            StringValue = value ? "1" : "0",
            NumericValue = value ? 1.0f : 0.0f,
            State = value ? SignalState.High : SignalState.Low
        };
    }

    /// <summary>
    /// Creates a string signal
    /// </summary>
    public static LogicSignalData String(string value)
    {
        var numericValue = TryParseFloat(value);
        
        return new LogicSignalData
        {
            IsEmpty = false,
            StringValue = value,
            NumericValue = numericValue,
            State = IsTruthy(value) ? SignalState.High : SignalState.Low
        };
    }

    /// <summary>
    /// Creates a numeric signal
    /// </summary>
    public static LogicSignalData Numeric(float value)
    {
        return new LogicSignalData
        {
            IsEmpty = false,
            StringValue = value.ToString("F7").TrimEnd('0').TrimEnd('.'),
            NumericValue = value,
            State = value != 0.0f ? SignalState.High : SignalState.Low
        };
    }

    /// <summary>
    /// Creates a color signal in RGBA format
    /// </summary>
    public static LogicSignalData Color(byte r, byte g, byte b, byte a = 255)
    {
        var colorString = $"{r},{g},{b},{a}";
        return String(colorString);
    }

    /// <summary>
    /// Gets the boolean value of this signal following the rules from the issue description
    /// </summary>
    public bool GetBooleanValue()
    {
        if (IsEmpty)
            return false;

        if (StringValue == "0" || string.IsNullOrEmpty(StringValue))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the numeric value, returning 0 for non-numeric strings
    /// </summary>
    public float GetNumericValue()
    {
        return NumericValue ?? 0.0f;
    }

    /// <summary>
    /// Gets the string value, returning empty string for empty signals
    /// </summary>
    public string GetStringValue()
    {
        return IsEmpty ? "" : (StringValue ?? "");
    }

    /// <summary>
    /// Checks if two signals are equal according to the logic described in the issue
    /// </summary>
    public bool Equals(LogicSignalData? other)
    {
        if (other == null)
            return false;

        // If both are empty, they are equal
        if (IsEmpty && other.IsEmpty)
            return true;

        // If one is empty and the other isn't, they're not equal
        if (IsEmpty != other.IsEmpty)
            return false;

        // Compare string values
        return GetStringValue() == other.GetStringValue();
    }

    /// <summary>
    /// Creates a NetworkPayload from this signal data
    /// </summary>
    public NetworkPayload ToNetworkPayload()
    {
        var payload = new NetworkPayload();
        
        payload[DeviceNetworkConstants.LogicState] = State;
        payload[DeviceNetworkConstants.LogicEmpty] = IsEmpty;
        
        if (!IsEmpty)
        {
            if (StringValue != null)
                payload[DeviceNetworkConstants.LogicStringData] = StringValue;
            if (NumericValue.HasValue)
                payload[DeviceNetworkConstants.LogicNumericData] = NumericValue.Value;
        }

        return payload;
    }

    /// <summary>
    /// Creates LogicSignalData from a NetworkPayload
    /// </summary>
    public static LogicSignalData FromNetworkPayload(NetworkPayload? payload)
    {
        if (payload == null)
            return Empty();

        // Check if signal is empty
        if (payload.TryGetValue(DeviceNetworkConstants.LogicEmpty, out bool isEmpty) && isEmpty)
            return Empty();

        // Try to get string data first
        if (payload.TryGetValue(DeviceNetworkConstants.LogicStringData, out string? stringData))
            return String(stringData);

        // Try to get numeric data
        if (payload.TryGetValue(DeviceNetworkConstants.LogicNumericData, out float numericData))
            return Numeric(numericData);

        // Fallback to boolean logic state for compatibility
        if (payload.TryGetValue(DeviceNetworkConstants.LogicState, out SignalState state))
        {
            return state switch
            {
                SignalState.High => Boolean(true),
                SignalState.Momentary => Boolean(true),
                _ => Boolean(false)
            };
        }

        return Empty();
    }

    private static float? TryParseFloat(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // Handle trailing dot
        if (value.EndsWith("."))
            value = value[..^1];

        // Handle leading dot
        if (value.StartsWith("."))
            value = "0" + value;

        if (float.TryParse(value, out float result))
            return result;

        return null;
    }

    private static bool IsTruthy(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // "0" is false, everything else is true according to the issue
        return value != "0";
    }

    public override string ToString()
    {
        if (IsEmpty)
            return "Пусто";

        return GetStringValue();
    }
}