// Helper: convert degrees to 16-point compass abbreviation
function getCurrentForecast() {
    const currentIcon = document.getElementById('currentIcon');
    const currentTemp = document.getElementById('currentTemp');
    const feelsLike = document.getElementById('feelsLike');
    const windInfo = document.getElementById('windInfo');
    const humidity = document.getElementById('humidity');
    const dewPoint = document.getElementById('dewPoint');
    const forecastContainer = document.getElementById('forecastContainer');

    // Show simple loading placeholders (optional)
    if (currentTemp) currentTemp.textContent = '-- °F';
    if (feelsLike) feelsLike.textContent = '';
    if (windInfo) windInfo.textContent = '';
    if (humidity) humidity.textContent = '--';
    if (dewPoint) dewPoint.textContent = '--';



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
                currentIcon.src = iconUrl;
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

            // Feels like logic: replicate server logic
            if (tempVal !== undefined && tempVal !== null && heatIndex !== undefined && heatIndex !== null) {
                if ((Number(tempVal) + 5) < Number(heatIndex)) {
                    if (feelsLike) feelsLike.innerHTML = `<div class="text-red">Feels Like: ${formatNumber(heatIndex, 1)} °F</div>`;
                }
            }
            if (tempVal !== undefined && tempVal !== null && windChill !== undefined && windChill !== null) {
                if ((Number(tempVal) - 5) > Number(windChill)) {
                    if (feelsLike) feelsLike.innerHTML = `<div>Feels Like: ${formatNumber(windChill, 1)} °F</div>`;
                }
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
                        humidity.textContent = `${Number(asPercent).toFixed(1)}%`;
                    } else {
                        humidity.textContent = String(humidityVal);
                    }
                } else {
                    humidity.textContent = '--';
                }
            }

            if (dewPoint) {
                dewPoint.textContent = (dewPointVal !== undefined && dewPointVal !== null) ? `${formatNumber(dewPointVal, 1)} °F` : '--';
            }

            // Render forecast periods into forecastContainer
            if (forecastContainer) {
                // Remove existing forecast items whose id starts with "f" (preserve other content)
                const existingForecastItems = forecastContainer.querySelectorAll('[id^="f"]');
                existingForecastItems.forEach(el => el.remove());

                if (periods && Array.isArray(periods) && periods.length > 0) {
                    periods.forEach(p => {
                        // Support both PascalCase and camelCase for period properties
                        const name = p.Name || p.name || '';
                        const shortForecast = p.ShortForecast || p.shortForecast || '';
                        const detailedForecast = p.DetailedForecast || p.detailedForecast || '';
                        const icon = p.Icon || p.icon || '';
                        const temperatureRaw = p.Temperature ?? p.temperature;
                        const temperatureUnit = p.TemperatureUnit || p.temperatureUnit || '';
                        const windSpeedText = p.WindSpeed || p.windSpeed || '';
                        const windDirectionText = p.WindDirection || p.windDirection || '';

                        const temperatureDisplay = (typeof temperatureRaw === 'number') ? formatNumber(temperatureRaw, 0) : (temperatureRaw ?? '--');

                        const col = document.createElement('div');
                        col.className = 'col-6';
                        col.id = 'forecast';
                        col.innerHTML = `
                                    <div class="card card-style" id="f${p.PeriodName || p.periodName || ''}">
                                        <div class="row g-0">
                                            <div class="col-2" style="text-align: center; padding-left: 7px;">
                                                <img src="${icon || ''}" alt="${shortForecast || ''}" style="max-width: 48px; max-height: 48px;" class="rounded mt-25"/>
                                            </div>
                                            <div class="col-10 mt-2">
                                                <div class="card-body">
                                                    <h5 class="card-title">${name}</h5>
                                                    <div class="card-text">
                                                        <div class="text-sm">${shortForecast}</div>
                                                        <div class="text-sm"><b>${temperatureDisplay}°${temperatureUnit}</b> • ${windSpeedText} ${windDirectionText}</div>
                                                        ${detailedForecast ? `<p style="margin-top:10px;" class="text-sm text-gray">${detailedForecast}</p>` : ''}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                `;
                        forecastContainer.appendChild(col);
                    });
                } else {
                    forecastContainer.innerHTML = '<div class="col-12"><div class="card mb-30"><div class="card-body">No forecast data available.</div></div></div>';
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

function getCurrentRadar() {
    $('#radar-loop').attr('src', 'https://radar.weather.gov/ridge/standard/KTLH_loop.gif');
}

$(function () {
    getCurrentForecast();
    getCurrentRadar();

    setInterval(getCurrentRadar, 2 * 60 * 1000); // Refresh radar every 2 minutes
    setInterval(getCurrentForecast, 15 * 60 * 1000); // Refresh forecast every 15 minutes
});