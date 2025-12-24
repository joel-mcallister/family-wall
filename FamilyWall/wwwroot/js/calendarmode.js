// Helper: convert degrees to 16-point compass abbreviation
function getCurrentForecast() {
    const currentIcon = document.getElementById('currentIcon');
    const currentTemp = document.getElementById('currentTemp');
    const windInfo = document.getElementById('windInfo');
    const humidity = document.getElementById('humidity');
    
    // Show simple loading placeholders (optional)
    if (currentTemp) currentTemp.textContent = '-- °F';
    if (windInfo) windInfo.textContent = '';
    if (humidity) humidity.textContent = '--';
    

    fetch('./?handler=CurrentForecast', { cache: 'no-store' })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.json();
        })
        .then(data => {
            const observation = data.observation || null;

            // Find periods array in several possible paths
            const periods = data.forecast?.properties?.periods
                || data.forecast?.Properties?.Periods
                || data.forecast?.properties?.periods
                || data.Forecast?.properties?.periods
                || data.properties?.periods
                || data.Properties?.Periods
                || data.periods
                || null;

            // Choose first period for current icon if available
            const firstPeriod = (Array.isArray(periods) && periods.length > 0) ? periods[0] : null;
            const iconUrl = firstPeriod?.icon || firstPeriod?.Icon || observation?.Properties?.Icon || observation?.properties?.icon || '';

            if (currentIcon && iconUrl) {
                // If the icon URL indicates fog, show the Font Awesome smog icon inside a span.
                const isFog = /fog|smog|dust|smoke|haze/i.test(iconUrl);
                const isClear = /skc|few/i.test(iconUrl);
                const isCloudy = /sct|bkn|ovc|cloudy/i.test(iconUrl);
                const isWindy = /wind/i.test(iconUrl);
                const isSnow = /snow|sleet|fzra|blizzard/i.test(iconUrl);
                const isRain = /rain|shra|drizzle/i.test(iconUrl);
                const isThunder = /tsra|thunder/i.test(iconUrl);
                const isTornado = /tornado/i.test(iconUrl);
                const isHurricane = /hurricane|tropical/i.test(iconUrl);
                const isHot = /hot/i.test(iconUrl);
                const isCold = /cold/i.test(iconUrl);

                if (isFog) {
                    currentIcon.innerHTML = '<i class="fa-solid fa-smog"></i>';
                }
                else if (isClear) {
                    currentIcon.innerHTML = `<i class="fa-regular fa-sun"></i>`;
                }
                else if (isCloudy)
                {
                    currentIcon.innerHTML = `<i class="fa-regular fa-cloud"></i>`;
                }
                else if (isWindy)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-wind"></i>`;
                }
                else if (isSnow)
                {
                    currentIcon.innerHTML = `<i class="fa-regular fa-snowflake"></i>`;
                }
                else if (isRain)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-cloud-rain"></i>`;
                }
                else if (isThunder)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-cloud-bolt"></i>`;
                }
                else if (isTornado)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-tornado"></i>`;
                }
                else if (isHurricane)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-hurricane"></i>`;
                }
                else if (isHot)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-temperature-high"></i>`;
                }
                else if (isCold)
                {
                    currentIcon.innerHTML = `<i class="fa-solid fa-temperature-low"></i>`;
                }
                else {
                    currentIcon.innerHTML = ``;
                }
            }

            // Observation values (may be absent)
            const obsProps = observation?.properties || observation?.Properties || null;
            const tempVal = obsProps?.temperature?.value;
            const heatIndex = obsProps?.heatIndex?.value;
            const windChill = obsProps?.windChill?.value;
            const windSpeed = obsProps?.windSpeed?.value;
            const windDirDegrees = obsProps?.windDirection?.value;
            const humidityVal = obsProps?.relativeHumidity?.value;
            const dewPointVal = obsProps?.dewpoint?.value;

            if (currentTemp) {
                currentTemp.textContent = (tempVal !== undefined && tempVal !== null) ? `${formatNumber(tempVal, 1)} °F` : '-- °F';
            }

            if (windInfo) {
                // windDirDegrees from observation may be numeric degrees or a string; attempt to parse number
                let dirLabel = '';
                if (windDirDegrees !== undefined && windDirDegrees !== null) {
                    const parsed = Number(windDirDegrees);
                    dirLabel = !isNaN(parsed) ? degreesToCompass(parsed) : String(windDirDegrees);
                }
                const speedText = (windSpeed !== undefined && windSpeed !== null) ? `${formatNumber(windSpeed, 1)} mph` : '--';
                windInfo.textContent = `${speedText} ${dirLabel}`.trim();
            }

            if (humidity) {
                if (humidityVal !== undefined && humidityVal !== null) {
                    const asNumber = Number(humidityVal);
                    if (!isNaN(asNumber)) {
                        const asPercent = (asNumber > 1 ? asNumber : asNumber * 100);
                        humidity.textContent = `Humidity: ${Number(asPercent).toFixed(1)}%`;
                    } else {
                        humidity.textContent = String(humidityVal);
                    }
                } else {
                    humidity.textContent = '--';
                }
            }

        })
        .catch(err => {
            console.error('Failed to load current forecast:', err);
            if (forecastContainer) {
                forecastContainer.innerHTML = '<div class="col-12"><div class="card mb-30"><div class="card-body">Unable to load forecast.</div></div></div>';
            }
        });
}


$(function () {
    getCurrentForecast();
    setInterval(getCurrentForecast, 15 * 60 * 1000); // Refresh forecast every 15 minutes
});