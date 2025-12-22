function degreesToCompass(deg) {
    if (deg === null || deg === undefined || isNaN(deg)) return '';
    let d = deg % 360;
    if (d < 0) d += 360;
    const dirs = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
    const index = Math.round(d / 22.5) % 16;
    return dirs[index];
}

function formatNumber(value, decimals) {
    if (value === null || value === undefined || isNaN(value)) return '--';
    return Number(value).toFixed(decimals);
}

function formatDateTime(value) {
    if (!value) {
        return '';
    }

    var d = new Date(value);
    if (isNaN(d.getTime())) {
        return String(value);
    }

    try {
        // Return date-only string according to user's locale, e.g. "December 31, 2025"
        return d.toLocaleDateString(undefined, { year: 'numeric', month: 'numeric', day: 'numeric' });
    } catch (err) {
        // Fallback: basic locale date string
        return d.toLocaleDateString();
    }
}
