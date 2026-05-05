window.getUserTimeZoneIANA = function () {
    // Returns the IANA time zone name, e.g., "Europe/Paris"
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
};

window.getUserTimeZoneOffset = function () {
    // getTimezoneOffset() returns minutes *behind* UTC (so UTC+2 returns -120)
    const offsetMinutes = new Date().getTimezoneOffset();
    // Convert to hours and minutes in ±HH:MM format
    const sign = offsetMinutes <= 0 ? "+" : "-";
    const absMinutes = Math.abs(offsetMinutes);
    const hours = String(Math.floor(absMinutes / 60)).padStart(2, "0");
    const minutes = String(absMinutes % 60).padStart(2, "0");
    return `${sign}${hours}:${minutes}`;
};

window.getUserTimeZoneOffsetMinutes = function () {
    // Raw offset in minutes (negative means ahead of UTC)
    return new Date().getTimezoneOffset() * -1; // Flip sign so positive = ahead of UTC
};